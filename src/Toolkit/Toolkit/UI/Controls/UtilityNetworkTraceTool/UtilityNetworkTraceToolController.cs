// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Internal class used to manage utility network tracing.
    /// </summary>
    internal class UtilityNetworkTraceToolController : IDisposable, INotifyPropertyChanged
    {
        private readonly SynchronizationContext _synchronizationContext;
        private Map? _map;

        // Warnings
        private bool _insufficientStartingPointsWarning;
        private bool _tooManyStartingPointsWarning;
        private bool _duplicatedTraceWarning;

        // Status
        private bool _isLoadingNetwork;
        private bool _isReadyToConfigure;
        private bool _isRunningTrace;
        private bool _isAddingStartingPoints;
        private bool _enableTrace;

        // Symbology
        private System.Drawing.Color _resultColor = System.Drawing.Color.Blue;
        private Symbol? _startingPointSymbol;
        private Symbol? _resultPointSymbol;
        private Symbol? _resultLineSymbol;
        private Symbol? _resultFillSymbol;

        public GraphicsOverlay StartingPointGraphicsOverlay { get; } = new GraphicsOverlay();

        // Cancellation
        private CancellationTokenSource? _getTraceTypesCts;
#pragma warning disable SA1401 // Fields should be private
        internal CancellationTokenSource? _traceCts;
        internal CancellationTokenSource? _getFeaturesForElementsCts;
#pragma warning restore SA1401 // Fields should be private

        // Selection
        private UtilityNetwork? _selectedUtilityNetwork;
        private UtilityNamedTraceConfiguration? _selectedTraceType;
        private StartingPointModel? _selectedStartingPoint;

        // Trace configuration
        private string? _activeTraceName;

        // Options
        private bool _autoZoomToTraceResults;

        // Interaction
        private Viewpoint? _viewpoint;
        private Tuple<CalloutDefinition, MapPoint>? _requestedCallout;

        private INotifyCollectionChanged? _lastObservedNetworkCollection;

        #region Observable collections

        /// <summary>
        /// Gets the collection of results.
        /// </summary>
        public ObservableCollection<UtilityTraceOperationResult> Results { get; } = new ObservableCollection<UtilityTraceOperationResult>();

        /// <summary>
        /// Gets the collection of available utility networks.
        /// </summary>
        public ObservableCollection<UtilityNetwork> UtilityNetworks { get; } = new ObservableCollection<UtilityNetwork>();

        /// <summary>
        /// Gets the collection of available trace types for the <see cref="SelectedUtilityNetwork"/>.
        /// </summary>
        public ObservableCollection<UtilityNamedTraceConfiguration> TraceTypes { get; } = new ObservableCollection<UtilityNamedTraceConfiguration>();

        /// <summary>
        /// Gets the collection of starting points. Consumers should use <see cref="AddStartingPoint(ArcGISFeature, MapPoint?)"/> to add to this collection.
        /// </summary>
        public ObservableCollection<StartingPointModel> StartingPoints { get; } = new ObservableCollection<StartingPointModel>();

        private void UtilityNetworks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (UtilityNetworks.Count == 1)
            {
                RunOnUIThread(() =>
                {
                    SelectedUtilityNetwork = UtilityNetworks[0];
                });
            }
        }

        private void TraceTypes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (TraceTypes.Count == 1)
            {
                SelectedTraceType = TraceTypes[0];
            }
        }

        private void Results_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ApplyWarnings();
        }

        private void StartingPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    StartingPointGraphicsOverlay.Graphics.Clear();
                }

                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<StartingPointModel>().Where(m => m.SelectionGraphic != null))
                    {
                        if (item.SelectionGraphic != null)
                        {
                            StartingPointGraphicsOverlay.Graphics.Remove(item.SelectionGraphic);
                        }
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<StartingPointModel>())
                    {
                        if (item.SelectionGraphic != null && !StartingPointGraphicsOverlay.Graphics.Contains(item.SelectionGraphic))
                        {
                            StartingPointGraphicsOverlay.Graphics.Add(item.SelectionGraphic);
                        }
                    }

                    // Automatically switch to filtering view/stop adding when points have been found
                    IsAddingStartingPoints = false;
                }

                ApplyWarnings();
            });
        }

        #endregion Observable collections

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityNetworkTraceToolController"/> class.
        /// </summary>
        public UtilityNetworkTraceToolController()
        {
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();

            // Update result symbols to match default color.
            ApplyResultColor();

            // Automatically select sole items in lists.
            UtilityNetworks.CollectionChanged += UtilityNetworks_CollectionChanged;
            TraceTypes.CollectionChanged += TraceTypes_CollectionChanged;

            // Automatically update warnings
            Results.CollectionChanged += Results_CollectionChanged;

            // Automatically update warnings, keep graphics overlay in sync
            StartingPoints.CollectionChanged += StartingPoints_CollectionChanged;
        }

        #region Utility Network and Trace Type Selection

        /// <summary>
        /// Gets or sets the selected utility network. Setting this value will reset the controller then update <see cref="TraceTypes"/>.
        /// </summary>
        public UtilityNetwork? SelectedUtilityNetwork
        {
            get => _selectedUtilityNetwork;
            set
            {
                if (_selectedUtilityNetwork != value)
                {
                    Reset(clearTraceTypes: true);
                    _selectedUtilityNetwork = value;
                    OnPropertyChanged();
                    _ = LoadTraceTypesAsync();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected trace type. Setting this value will automatically update the trace name.
        /// </summary>
        public UtilityNamedTraceConfiguration? SelectedTraceType
        {
            get => _selectedTraceType;
            set
            {
                if (_selectedTraceType != value)
                {
                    _selectedTraceType = value;

                    OnPropertyChanged();
                    ApplyWarnings();
                    TraceName = GetDefaultTraceName();
                }
            }
        }

        private async Task LoadTraceTypesAsync()
        {
            try
            {
                IsLoadingNetwork = true;

                if (Map != null
                    && SelectedUtilityNetwork is UtilityNetwork utilityNetwork)
                {
                    if (_getTraceTypesCts != null)
                    {
                        _getTraceTypesCts.Cancel();
                    }

                    _getTraceTypesCts = new CancellationTokenSource();
                    var traceTypes = await Map.GetNamedTraceConfigurationsFromUtilityNetworkAsync(utilityNetwork, _getTraceTypesCts.Token);
                    foreach (var traceType in traceTypes.OrderBy(tt => tt.Name))
                    {
                        TraceTypes.Add(traceType);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Do nothing when canceled
            }
            catch (Exception)
            {
            }
            finally
            {
                IsLoadingNetwork = false;
            }
        }

        #endregion Utility Network and Trace Type Selection

        #region GeoModel Loading

        /// <summary>
        /// Gets or sets the Map from which <see cref="UtilityNetworks"/> will be populated.
        /// </summary>
        public Map? Map
        {
            get => _map;
            set
            {
                if (value != _map)
                {
                    if (_map != null)
                    {
                        _map.Loaded -= OnGeoModelLoaded;
                    }

                    _map = value;
                    OnPropertyChanged();
                    Reset(true);
                    if (_map != null)
                    {
                        IsLoadingNetwork = true;
                        IsReadyToConfigure = false;
                        if (_map.LoadStatus == LoadStatus.Loaded)
                        {
                            OnGeoModelLoaded(Map, EventArgs.Empty);
                        }
                        else
                        {
                            _map.Loaded += OnGeoModelLoaded;
                        }
                    }
                }
            }
        }

        private void OnGeoModelLoaded(object? sender, EventArgs e)
        {
            RunOnUIThread(async () =>
            {
                try
                {
                    if (Map != null)
                    {
                        Map.Loaded -= OnGeoModelLoaded;
                    }

                    IsRunningTrace = false;
                    IsLoadingNetwork = true;
                    UtilityNetworks.Clear();

                    var utilityNetworks = (sender is Map map ? map.UtilityNetworks : null) ?? throw new ArgumentException("No UtilityNetworks found.");

                    if (_lastObservedNetworkCollection != null)
                    {
                        _lastObservedNetworkCollection.CollectionChanged -= GeoModelUtilityNetworks_CollectionChanged;
                        _lastObservedNetworkCollection = null;
                    }

                    if (utilityNetworks is INotifyCollectionChanged incc)
                    {
                        incc.CollectionChanged += GeoModelUtilityNetworks_CollectionChanged;
                        _lastObservedNetworkCollection = incc;
                    }

                    foreach (var utilityNetwork in utilityNetworks)
                    {
                        if (utilityNetwork.LoadStatus == LoadStatus.Loaded)
                        {
                            UtilityNetworks.Add(utilityNetwork);
                        }
                        else
                        {
                            utilityNetwork.Loaded += OnUtilityNetworkLoaded;
                            await utilityNetwork.LoadAsync();
                        }
                    }

                    if (UtilityNetworks.Any())
                    {
                        IsReadyToConfigure = true;
                    }
                }
                catch (Exception)
                {
                    IsReadyToConfigure = false;
                }
                finally
                {
                    IsLoadingNetwork = false;
                }
            });
        }

        private void GeoModelUtilityNetworks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IsRunningTrace = false;
            IsLoadingNetwork = true;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Reset(clearUtilityNetworks: true);
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is UtilityNetwork utilityNetwork)
                    {
                        if (SelectedUtilityNetwork == utilityNetwork)
                        {
                            SelectedUtilityNetwork = null;
                        }

                        UtilityNetworks.Remove(utilityNetwork);
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is UtilityNetwork utilityNetwork)
                    {
                        if (utilityNetwork.LoadStatus == LoadStatus.Loaded)
                        {
                            UtilityNetworks.Add(utilityNetwork);
                        }
                        else
                        {
                            utilityNetwork.Loaded += OnUtilityNetworkLoaded;
                            _ = utilityNetwork.LoadAsync();
                        }
                    }
                }
            }
        }

        private void OnUtilityNetworkLoaded(object? sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (sender is UtilityNetwork utilityNetwork && utilityNetwork.LoadStatus == LoadStatus.Loaded)
                {
                    utilityNetwork.Loaded -= OnUtilityNetworkLoaded;
                    UtilityNetworks.Add(utilityNetwork);
                    IsReadyToConfigure = true;
                }
            });
        }
        #endregion GeoModel Loading

        #region Starting Points
        public void AddStartingPoint(ArcGISFeature feature, MapPoint? location = null)
        {
            Geometry.Geometry? geometry = feature.Geometry;
            UtilityElement? element = null;
            try
            {
                element = SelectedUtilityNetwork?.CreateElement(feature);
            }
            catch (Exception)
            {
            }

            if (element == null)
            {
                return;
            }

            // Skip adding duplicate starting points.
            if (StartingPointGraphicsOverlay.Graphics.Any(g => g.Attributes["GlobalId"] is Guid guid && guid.Equals(element.GlobalId)))
            {
                return;
            }

            // Apply fraction along edge based on identify location.
            if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Edge && feature.Geometry is Polyline polyline)
            {
                if (polyline.HasZ && GeometryEngine.RemoveZ(polyline) is Polyline polyline2d)
                {
                    polyline = polyline2d;
                }

                if (location?.SpatialReference is SpatialReference sr && !sr.IsEqual(polyline?.SpatialReference)
                    && polyline != null && GeometryEngine.Project(polyline, sr) is Polyline projectedPolyline)
                {
                    polyline = projectedPolyline;
                }

                geometry = polyline;

                if (polyline != null && location != null && GeometryEngine.FractionAlong(polyline, location, double.NaN) is double fractionAlongEdge
                    && !double.IsNaN(fractionAlongEdge))
                {
                    element.FractionAlongEdge = fractionAlongEdge;
                }

                if (location == null && polyline?.Parts?.FirstOrDefault()?.StartPoint is MapPoint startPoint)
                {
                    location = startPoint;
                }
            }

            // Apply terminal settings.
            else if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Junction
                && element.AssetType?.TerminalConfiguration?.Terminals.Count > 1)
            {
                element.Terminal = element.AssetType.TerminalConfiguration.Terminals[0];
            }

            var graphic = new Graphic { Geometry = geometry as MapPoint ?? location, Symbol = StartingPointSymbol ?? _defaultStartingLocationSymbol };
            graphic.Attributes["GlobalId"] = element.GlobalId;
            graphic.Attributes["NetworkSource"] = element.NetworkSource.Name;
            graphic.Attributes["AssetGroup"] = element.AssetGroup.Name;
            graphic.Attributes["AssetType"] = element.AssetType?.Name;
            graphic.Attributes["Geometry"] = geometry?.ToJson();

            StartingPoints.Add(new StartingPointModel(this, element, graphic, feature, geometry?.Extent));
        }

        /// <summary>
        /// Gets or sets the selected starting point.
        /// Selecting a starting point selects the <see cref="StartingPointModel.SelectionGraphic"/> and updates <see cref="RequestedCallout"/> and <see cref="RequestedViewpoint"/>.
        /// </summary>
        public StartingPointModel? SelectedStartingPoint
        {
            get => _selectedStartingPoint;
            set
            {
                if (_selectedStartingPoint != value)
                {
                    _selectedStartingPoint = value;
                    OnPropertyChanged();

                    // Update selection and show callout.
                    RequestedCallout = null;

                    if (SelectedStartingPoint?.StartingPoint is UtilityElement startingPoint
                     && StartingPointGraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes["GlobalId"] is Guid guid
                     && guid.Equals(startingPoint.GlobalId)) is Graphic graphic)
                    {
                        StartingPointGraphicsOverlay.ClearSelection();
                        graphic.IsSelected = true;
                        if (graphic.Geometry is MapPoint location)
                        {
                            _ = SetCallout(SelectedStartingPoint, location);
                        }
                    }
                }
            }
        }

        private async Task SetCallout(StartingPointModel selectedStartingPoint, MapPoint location)
        {
            var calloutDefinition = new CalloutDefinition(selectedStartingPoint.StartingPoint.NetworkSource.Name, selectedStartingPoint.StartingPoint.AssetGroup.Name);
            try
            {
                calloutDefinition.Icon = selectedStartingPoint.Symbol is null ? null : await selectedStartingPoint.Symbol.CreateSwatchAsync();
            }
            catch (Exception)
            {
                // Ignore
            }

            RequestedCallout = new Tuple<CalloutDefinition, MapPoint>(calloutDefinition, location);
        }

        public Tuple<CalloutDefinition, MapPoint>? RequestedCallout
        {
            get => _requestedCallout;
            set
            {
                if (value != _requestedCallout)
                {
                    _requestedCallout = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion Starting Points

        #region Tracing

        /// <summary>
        /// Runs the trace operation. Results are added to <see cref="Results"/>.
        /// </summary>
        public async Task TraceAsync()
        {
            if (SelectedTraceType == null)
            {
                return;
            }

            var traceParameters = new UtilityTraceParameters(SelectedTraceType, StartingPoints.Select(sp => sp.StartingPoint));

            UtilityTraceOperationResult resultInProgress = new UtilityTraceOperationResult(this, SelectedTraceType, traceParameters, StartingPoints.ToList()) { GraphicVisualizationColor = ResultColor };

            try
            {
                IsRunningTrace = true;

                if (SelectedUtilityNetwork == null)
                {
                    throw new InvalidOperationException("No utility network selected.");
                }

                if (SelectedTraceType == null)
                {
                    throw new InvalidOperationException("No trace type selected.");
                }

                if (traceParameters == null)
                {
                    throw new InvalidOperationException("No trace parameters created.");
                }

                if (_traceCts != null)
                {
                    _traceCts.Cancel();
                }

                _traceCts = new CancellationTokenSource();
                var traceResults = await SelectedUtilityNetwork.TraceAsync(traceParameters, _traceCts.Token);

                Envelope? elementExtent = null;

                resultInProgress.RawResults?.AddRange(traceResults);

                foreach (var traceResult in traceResults)
                {
                    if (traceResult.Warnings.Count > 0)
                    {
                        resultInProgress.Warnings.AddRange(traceResult.Warnings);
                    }

                    if (traceResult is UtilityElementTraceResult elementTraceResult)
                    {
                        resultInProgress.ElementResults.AddRange(elementTraceResult.Elements);

                        if (_getFeaturesForElementsCts != null)
                        {
                            _getFeaturesForElementsCts.Cancel();
                        }

                        _getFeaturesForElementsCts = new CancellationTokenSource();
                        var features = await SelectedUtilityNetwork.GetFeaturesForElementsAsync(elementTraceResult.Elements, _getFeaturesForElementsCts.Token);
                        resultInProgress.Features.AddRange(features);
                        resultInProgress.AreFeaturesSelected = true;
                        if (features.Any() && AutoZoomToTraceResults)
                        {
                            elementExtent = GeometryEngine.CombineExtents(features.Select(m => m.Geometry).OfType<Geometry.Geometry>().ToList());
                        }
                    }
                    else if (traceResult is UtilityGeometryTraceResult geometryTraceResult)
                    {
                        if (geometryTraceResult.Multipoint is Multipoint multipoint)
                        {
                            var graphic = new Graphic(multipoint, (ResultPointSymbol ?? _defaultResultPointSymbol).Clone());
                            resultInProgress.Graphics.Add(graphic);
                            resultInProgress.ResultOverlay.Graphics.Add(graphic);
                        }

                        if (geometryTraceResult.Polyline is Polyline polyline)
                        {
                            var graphic = new Graphic(polyline, (ResultLineSymbol ?? _defaultResultLineSymbol).Clone());
                            resultInProgress.Graphics.Add(graphic);
                            resultInProgress.ResultOverlay.Graphics.Add(graphic);
                        }

                        if (geometryTraceResult.Polygon is Polygon polygon)
                        {
                            var graphic = new Graphic(polygon, (ResultFillSymbol ?? _defaultResultFillSymbol).Clone());
                            if (graphic.Symbol is SimpleFillSymbol simpleFillSymbol && simpleFillSymbol.Outline is SimpleLineSymbol outlineSymbol)
                            {
                                // Prevent sharing outlines
                                simpleFillSymbol.Outline = (SimpleLineSymbol)outlineSymbol.Clone();
                            }

                            resultInProgress.Graphics.Add(graphic);
                            resultInProgress.ResultOverlay.Graphics.Add(graphic);
                        }
                    }
                    else if (traceResult is UtilityFunctionTraceResult functionTraceResult)
                    {
                        foreach (var functionOutput in functionTraceResult.FunctionOutputs)
                        {
                            resultInProgress.FunctionResults.Add(functionOutput);
                        }
                    }
                }

                var resultExtent = elementExtent ?? resultInProgress.ResultOverlay.Extent;
                if (AutoZoomToTraceResults && resultExtent?.IsEmpty == false &&
                    GeometryEngine.Buffer(resultExtent, 15) is Geometry.Geometry bufferedGeometry)
                {
                    RequestedViewpoint = new Viewpoint(bufferedGeometry);
                }

                resultInProgress.ElementResultsGrouped.AddRange(resultInProgress.ElementResults.GroupBy(m => m.AssetGroup).OrderByDescending(group => group.Count()).Select(grp => new Tuple<UtilityAssetGroup, int>(grp.Key, grp.Count())));
                resultInProgress.Name = TraceName?.ToString() ?? GetDefaultTraceName();
                Results.Add(resultInProgress);
            }
            catch (TaskCanceledException)
            {
                // Do nothing when canceled
            }
            catch (Exception ex)
            {
                resultInProgress.Error = ex;
                resultInProgress.Name = TraceName?.ToString() ?? GetDefaultTraceName();
                Results.Add(resultInProgress);
            }
            finally
            {
                IsRunningTrace = false;
                TraceName = GetDefaultTraceName();
            }
        }

        private IEnumerable<FeatureLayer> GetFeatureLayer(IEnumerable<Layer> layers)
        {
            foreach (var layer in layers)
            {
                if (layer is FeatureLayer featureLayer)
                {
                    yield return featureLayer;
                }

                if (layer is GroupLayer groupLayer)
                {
                    GetFeatureLayer(groupLayer.Layers);
                }
            }
        }

        public Viewpoint? RequestedViewpoint
        {
            get => _viewpoint;
            set
            {
                if (value != _viewpoint)
                {
                    _viewpoint = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion Tracing

        #region Status and Settings
        private string GetDefaultTraceName()
        {
            return $"{SelectedTraceType?.Name} {Results.Count + 1}";
        }

        public void Reset(bool clearUtilityNetworks = false, bool clearTraceTypes = false)
        {
            TooManyStartingPointsWarning = false;
            InsufficientStartingPointsWarning = false;
            DuplicatedTraceWarning = false;

            if (clearUtilityNetworks)
            {
                UtilityNetworks.Clear();
                SelectedUtilityNetwork = null;
                IsReadyToConfigure = false;
                IsLoadingNetwork = false;
            }

            if (clearTraceTypes)
            {
                TraceTypes.Clear();
                SelectedTraceType = null;
            }

            StartingPoints.Clear();
            StartingPointGraphicsOverlay.Graphics.Clear();

            _getTraceTypesCts?.Cancel();
            _traceCts?.Cancel();
            _getFeaturesForElementsCts?.Cancel();

            IsRunningTrace = false;

            foreach (var result in Results)
            {
                result.AreFeaturesSelected = false;
                result.ResultOverlay.Graphics.Clear();
            }

            Results.Clear();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically zoom to trace results.
        /// </summary>
        public bool AutoZoomToTraceResults
        {
            get => _autoZoomToTraceResults;
            set
            {
                if (value != _autoZoomToTraceResults)
                {
                    _autoZoomToTraceResults = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the name to use to label the next trace result.
        /// </summary>
        public string? TraceName
        {
            get => _activeTraceName;
            set
            {
                if (value != _activeTraceName)
                {
                    _activeTraceName = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion Status and Settings

        #region Symbology

        // Fallback symbols
        private readonly Symbol _defaultStartingLocationSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.LimeGreen, 20d);
        private readonly Symbol _defaultResultPointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 20d);
        private readonly Symbol _defaultResultLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, System.Drawing.Color.Blue, 5d);
        private readonly Symbol _defaultResultFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.ForwardDiagonal, System.Drawing.Color.Blue,
                new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Blue, 2d));

        /// <summary>
        /// Gets or sets the starting point symbol.
        /// </summary>
        public Symbol? StartingPointSymbol
        {
            get => _startingPointSymbol;
            set
            {
                if (value != _startingPointSymbol)
                {
                    _startingPointSymbol = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the result point symbol.
        /// </summary>
        public Symbol? ResultPointSymbol
        {
            get => _resultPointSymbol;
            set
            {
                if (value != _resultPointSymbol)
                {
                    _resultPointSymbol = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the result line symbol.
        /// </summary>
        public Symbol? ResultLineSymbol
        {
            get => _resultLineSymbol;
            set
            {
                if (value != _resultLineSymbol)
                {
                    _resultLineSymbol = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the result fill symbol.
        /// </summary>
        public Symbol? ResultFillSymbol
        {
            get => _resultFillSymbol;
            set
            {
                if (value != _resultFillSymbol)
                {
                    _resultFillSymbol = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the result color. Setting this value will update <see cref="ResultFillSymbol"/>, <see cref="ResultLineSymbol"/>, and <see cref="ResultPointSymbol"/>
        /// if they are of type <see cref="SimpleFillSymbol"/>, <see cref="SimpleLineSymbol"/>, and <see cref="SimpleMarkerSymbol"/> respectively.
        /// </summary>
        public System.Drawing.Color ResultColor
        {
            get => _resultColor;
            set
            {
                if (value != _resultColor)
                {
                    _resultColor = value;
                    OnPropertyChanged();
                    ApplyResultColor();
                }
            }
        }

        private void ApplyResultColor()
        {
            if (ResultFillSymbol is SimpleFillSymbol existingFill)
            {
                if (existingFill.Outline is SimpleLineSymbol existingOutline)
                {
                    existingOutline.Color = _resultColor;
                }

                existingFill.Color = _resultColor;
            }
            else if (ResultFillSymbol == null && _defaultResultFillSymbol.Clone() is SimpleFillSymbol defaultFill)
            {
                ResultFillSymbol = defaultFill;
                if (defaultFill.Outline?.Clone() is SimpleLineSymbol defaultOutline)
                {
                    defaultFill.Outline = defaultOutline;
                    defaultOutline.Color = _resultColor;
                }

                defaultFill.Color = _resultColor;
            }

            if (ResultPointSymbol is SimpleMarkerSymbol existingPoint)
            {
                existingPoint.Color = _resultColor;
            }
            else if (ResultPointSymbol == null && _defaultResultPointSymbol.Clone() is SimpleMarkerSymbol copiedMarker)
            {
                ResultPointSymbol = copiedMarker;
                copiedMarker.Color = _resultColor;
            }

            if (ResultLineSymbol is SimpleLineSymbol existingLine)
            {
                existingLine.Color = _resultColor;
            }
            else if (ResultLineSymbol == null)
            {
                ResultLineSymbol = _defaultResultLineSymbol.Clone();
                ((SimpleLineSymbol)ResultLineSymbol).Color = _resultColor;
            }
        }

        public void HandleStartingPointSymbolChanged()
        {
            foreach (var graphic in StartingPointGraphicsOverlay.Graphics)
            {
                graphic.Symbol = StartingPointSymbol;
            }
        }
        #endregion Symbology

        #region Status
        public bool IsAddingStartingPoints
        {
            get => _isAddingStartingPoints;
            set
            {
                if (value != _isAddingStartingPoints)
                {
                    _isAddingStartingPoints = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRunningTrace
        {
            get => _isRunningTrace;
            set
            {
                if (value != _isRunningTrace)
                {
                    _isRunningTrace = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoadingNetwork
        {
            get => _isLoadingNetwork;
            set
            {
                if (value != _isLoadingNetwork)
                {
                    _isLoadingNetwork = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsReadyToConfigure
        {
            get => _isReadyToConfigure;
            set
            {
                if (value != _isReadyToConfigure)
                {
                    _isReadyToConfigure = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableTrace
        {
            get => _enableTrace;
            set
            {
                if (value != _enableTrace)
                {
                    _enableTrace = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion Status

        #region Warnings

        public bool InsufficientStartingPointsWarning
        {
            get => _insufficientStartingPointsWarning;
            set
            {
                if (value != _insufficientStartingPointsWarning)
                {
                    _insufficientStartingPointsWarning = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TooManyStartingPointsWarning
        {
            get => _tooManyStartingPointsWarning;
            set
            {
                if (value != _tooManyStartingPointsWarning)
                {
                    _tooManyStartingPointsWarning = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DuplicatedTraceWarning
        {
            get => _duplicatedTraceWarning;
            set
            {
                if (value != _duplicatedTraceWarning)
                {
                    _duplicatedTraceWarning = value;
                    OnPropertyChanged();
                }
            }
        }

        private void ApplyWarnings()
        {
            if (SelectedTraceType == null)
            {
                return;
            }

            var minimum = SelectedTraceType.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ? 2 : 1;
            EnableTrace = StartingPoints.Count >= minimum;
            InsufficientStartingPointsWarning = StartingPoints.Count < minimum;
            TooManyStartingPointsWarning = StartingPoints.Count > minimum;
            bool hasDuplicated = false;
            foreach (var result in Results)
            {
                if (result.Configuration == SelectedTraceType && (result.StartingPoints?.SequenceEqual(StartingPoints) ?? false))
                {
                    hasDuplicated = true;
                    break;
                }
            }

            DuplicatedTraceWarning = hasDuplicated;
        }

        #endregion Warnings

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RunOnUIThread(Action action)
        {
            _synchronizationContext?.Post((o) => action?.Invoke(), null);
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            try
            {
                _getTraceTypesCts?.Dispose();
                _traceCts?.Dispose();
                _getFeaturesForElementsCts?.Dispose();
            }
            catch
            {
            }
        }
    }
}