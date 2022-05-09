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

#if !__IOS__ && !__ANDROID__
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    internal class UtilityNetworkTraceToolController : IDisposable, INotifyPropertyChanged
    {
        private bool _ignoreEventsFlag;

        private readonly ObservableCollection<UtilityNetwork> _utilityNetworks
            = new ObservableCollection<UtilityNetwork>();

        internal ObservableCollection<UtilityNetwork> UtilityNetworks => _utilityNetworks;

        private readonly ObservableCollection<UtilityTraceFunctionOutput> _functionResults
            = new ObservableCollection<UtilityTraceFunctionOutput>();

        internal ObservableCollection<UtilityTraceFunctionOutput> FunctionResults => _functionResults;

        private readonly ObservableCollection<UtilityNamedTraceConfiguration> _traceTypes
            = new ObservableCollection<UtilityNamedTraceConfiguration>();

        internal ObservableCollection<UtilityNamedTraceConfiguration> TraceTypes => _traceTypes;

        private readonly ObservableCollection<StartingPointModel> _startingPoints
            = new ObservableCollection<StartingPointModel>();

        public ObservableCollection<StartingPointModel> ControllerStartingPoints => _startingPoints;

        private readonly Symbol _defaultStartingLocationSymbol
            = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.LimeGreen, 20d);

        private readonly Symbol _defaultResultPointSymbol
            = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Blue, 20d);

        private readonly Symbol _defaultResultLineSymbol
            = new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, Color.Blue, 5d);

        private readonly Symbol _defaultResultFillSymbol
            = new SimpleFillSymbol(SimpleFillSymbolStyle.ForwardDiagonal, Color.Blue,
                new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 2d));

        internal readonly GraphicsOverlay _startingPointGraphicsOverlay = new GraphicsOverlay();
        internal readonly GraphicsOverlay _resultGraphicsOverlay = new GraphicsOverlay();

        private readonly SynchronizationContext _synchronizationContext;

        private CancellationTokenSource? _getTraceTypesCts;
        private CancellationTokenSource? _traceCts;
        private CancellationTokenSource? _getFeaturesForElementsCts;

        public void ApplyNewMap(Map newMap)
        {
            Map = newMap;
            if (Map != null)
            {
                Reset(true);
                IsBusy = true;
                Status = "Loading utility networks...";

                if (Map.LoadStatus == LoadStatus.Loaded)
                {
                    OnGeoModelLoaded(Map, EventArgs.Empty);
                }
                else
                {
                    var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(Map)
                    {
                        OnEventAction = (instance, source, eventArgs) => OnGeoModelLoaded(source, eventArgs),
                        OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent,
                    };
                    Map.Loaded += listener.OnEvent;
                }
            }
        }

        public UtilityNetworkTraceToolController()
        {
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();

            _utilityNetworks.CollectionChanged += (s, e) =>
            {
                if (_utilityNetworks.Count == 1)
                {
                    SelectedUtilityNetwork = _utilityNetworks[0];
                }

                ShowUtilityNetworks = _utilityNetworks.Count > 1;
                if (_utilityNetworks.Count > 1)
                {
                    Status = "Select a utility network";
                }
            };

            _traceTypes.CollectionChanged += (s, e) =>
            {
                if (_traceTypes.Count == 1)
                {
                    SelectedTraceType = _traceTypes[0];
                }

                ShowTraceTypes = _traceTypes.Count > 1;
                if (_traceTypes.Count > 1)
                {
                    Status = "Select a trace type";
                }
            };

            _startingPoints.CollectionChanged += (s, e) =>
            {
                if (SelectedTraceType is UtilityNamedTraceConfiguration traceType &&
                TraceParameters is UtilityTraceParameters traceParameters)
                {
                    traceParameters.StartingLocations.Clear();
                    if (_startingPoints.Any())
                    {
                        foreach (var startingPoint in _startingPoints.Select(p => p.StartingPoint))
                        {
                            traceParameters.StartingLocations.Add(startingPoint);
                        }
                    }

                    var minimum = traceType.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ? 2 : 1;
                    EnableTrace = TraceParameters?.StartingLocations.Count >= minimum;
                }

                ShowStartingPoints = _startingPoints.Any();
                if (_startingPoints.Any())
                {
                    Status = $"{_startingPoints.Count} starting points";
                }
            };

            _functionResults.CollectionChanged += (s, e) =>
            {
                ShowFunctionResults = _functionResults.Any();
            };
        }

        private void RunOnUIThread(Action action)
        {
            _synchronizationContext?.Post((o) => action?.Invoke(), null);
        }

        private UtilityNetwork? _selectedUtilityNetwork;

        public UtilityNetwork? SelectedUtilityNetwork
        {
            get => _selectedUtilityNetwork;
            set
            {
                if (_selectedUtilityNetwork != value)
                {
                    Reset(clearTraceTypes: true);
                    _selectedUtilityNetwork = value;
                    _ignoreEventsFlag = true;
                    OnPropertyChanged();
                    _ = LoadTraceTypesAsync();
                }
            }
        }

        internal void UpdateSelectedUtilityNetwork(UtilityNetwork? utilityNetwork)
        {
            if (!_ignoreEventsFlag)
            {
                SelectedUtilityNetwork = utilityNetwork;
            }

            _ignoreEventsFlag = false;
        }

        private UtilityNamedTraceConfiguration? _selectedTraceType;

        public UtilityNamedTraceConfiguration? SelectedTraceType
        {
            get => _selectedTraceType;
            set
            {
                if (_selectedTraceType != value)
                {
                    _selectedTraceType = value;

                    OnPropertyChanged();

                    if (_selectedTraceType is UtilityNamedTraceConfiguration traceType)
                    {
                        TraceParameters = new UtilityTraceParameters(traceType,
                            _startingPoints.Select(p => p.StartingPoint));
                        var minimum = traceType.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ? 2 : 1;
                        EnableTrace = TraceParameters?.StartingLocations.Count >= minimum;
                    }
                    else
                    {
                        TraceParameters = null;
                    }
                }
            }
        }

        internal void UpdateSelectedTraceType(UtilityNamedTraceConfiguration? traceType)
        {
            _ignoreEventsFlag = true;
            SelectedTraceType = traceType;
            _ignoreEventsFlag = false;
        }

        private StartingPointModel? _selectedStartingPoint;

        public StartingPointModel? SelectedStartingPoint
        {
            get => _selectedStartingPoint;
            set
            {
                if (_selectedStartingPoint != value)
                {
                    _selectedStartingPoint = value;
                    if (!_ignoreEventsFlag)
                    {
                        OnPropertyChanged();
                    }

                    SelectStartingPoint();
                }
            }
        }

        internal void UpdateSelectedStartingPoint(StartingPointModel? startingPoint)
        {
            _ignoreEventsFlag = true;
            SelectedStartingPoint = startingPoint;
            _ignoreEventsFlag = false;
        }

        private void SelectStartingPoint()
        {
            RequestedCallout = null;

            if (SelectedStartingPoint?.StartingPoint is UtilityElement startingPoint
             && _startingPointGraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes["GlobalId"] is Guid guid
             && guid.Equals(startingPoint.GlobalId)) is Graphic graphic)
            {
                _startingPointGraphicsOverlay.ClearSelection();
                graphic.IsSelected = true;
                if (graphic.Geometry is MapPoint location)
                {
                    var calloutDefinition = new CalloutDefinition(startingPoint.NetworkSource.Name, startingPoint.AssetGroup.Name);
                    RequestedCallout = new Tuple<CalloutDefinition, MapPoint>(calloutDefinition, location);
                }
            }
        }

        private UtilityTraceParameters? _traceParameters;

        private UtilityTraceParameters? TraceParameters
        {
            get => _traceParameters;
            set
            {
                if (_traceParameters != value)
                {
                    _traceParameters = value;
                    Status = GetStatus();
                }
            }
        }

        private void OnUtilityNetworksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IsBusy = true;
            Status = "Loading utility networks...";

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

                        _utilityNetworks.Remove(utilityNetwork);
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
                            _utilityNetworks.Add(utilityNetwork);
                        }
                        else
                        {
                            var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(utilityNetwork)
                            {
                                OnEventAction = (instance, source, eventArgs) => OnUtilityNetworkLoaded(source, eventArgs),
                                OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent,
                            };
                            utilityNetwork.Loaded += listener.OnEvent;
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
                    _utilityNetworks.Add(utilityNetwork);
                }
            });
        }

        private void OnGeoModelLoaded(object? sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                try
                {
                    IsBusy = true;
                    Status = "Loading utility networks";
                    _utilityNetworks.Clear();

                    var utilityNetworks = (sender is Map map ? map.UtilityNetworks : null) ?? throw new ArgumentException("No UtilityNetworks found.");

                    if (utilityNetworks is INotifyCollectionChanged incc)
                    {
                        var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                        {
                            OnEventAction = (instance, source, eventArgs) => OnUtilityNetworksCollectionChanged(sender, eventArgs),
                            OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                        };
                        incc.CollectionChanged += listener.OnEvent;
                    }

                    foreach (var utilityNetwork in utilityNetworks)
                    {
                        if (utilityNetwork.LoadStatus == LoadStatus.Loaded)
                        {
                            _utilityNetworks.Add(utilityNetwork);
                        }
                        else
                        {
                            var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(utilityNetwork)
                            {
                                OnEventAction = (instance, source, eventArgs) => OnUtilityNetworkLoaded(source, eventArgs),
                                OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent,
                            };
                            utilityNetwork.Loaded += listener.OnEvent;
                            _ = utilityNetwork.LoadAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Status = $"Loading utility networks failed ({ex.GetType().Name}): {ex.Message}";
                    IsBusy = false;
                }
            });
        }





        public void AddStartingPoint(ArcGISFeature feature, MapPoint? location = null)
        {
            Geometry.Geometry? geometry = feature.Geometry;
            UtilityElement? element = null;
            try
            {
                element = SelectedUtilityNetwork?.CreateElement(feature);
            }
            catch (Exception ex)
            {
                Status = $"CreateElement failed ({ex.GetType().Name}): {ex.Message}";
            }

            if (element != null)
            {
                if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Edge
                    && feature.Geometry is Polyline polyline)
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
                else if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Junction
                    && element.AssetType?.TerminalConfiguration?.Terminals.Count > 1)
                {
                    element.Terminal = element.AssetType.TerminalConfiguration.Terminals[0];
                }

                if (StartingPoints == null)
                {
                    StartingPoints = new ObservableCollection<ArcGISFeature>();
                    if (StartingPoints is INotifyCollectionChanged incc)
                    {
                        var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                        {
                            OnEventAction = (instance, source, eventArgs) => OnStartingPointsCollectionChanged(source, eventArgs),
                            OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                        };
                        incc.CollectionChanged += listener.OnEvent;
                    }
                }

                if (!StartingPoints.Any(f => f.Attributes["GlobalId"] is Guid guid
                   && guid.Equals(element.GlobalId)))
                {
                    StartingPoints.Add(feature);
                }

                AddStartingPoint(element, geometry, location);
            }
        }

        private void AddStartingPoint(UtilityElement element, Geometry.Geometry? geometry, MapPoint? location)
        {
            if (!_startingPointGraphicsOverlay.Graphics.Any(g => g.Attributes["GlobalId"] is Guid guid
              && guid.Equals(element.GlobalId)))
            {
                var graphic = new Graphic();
                graphic.Geometry = geometry as MapPoint ?? location;
                graphic.Attributes["GlobalId"] = element.GlobalId;
                graphic.Attributes["NetworkSource"] = element.NetworkSource.Name;
                graphic.Attributes["AssetGroup"] = element.AssetGroup.Name;
                graphic.Attributes["AssetType"] = element.AssetType?.Name;
                graphic.Attributes["Geometry"] = geometry?.ToJson();
                graphic.Symbol = StartingPointSymbol ?? _defaultStartingLocationSymbol;
                _startingPointGraphicsOverlay.Graphics.Add(graphic);
            }

            if (!_startingPoints.Any(p => p.StartingPoint.GlobalId.Equals(element.GlobalId)))
            {
                var model = new StartingPointModel(element,
                    new DelegateCommand((o) =>
                    {
                        if (o is UtilityElement startingPoint)
                        {
                            UpdateStartingPoint(startingPoint);
                        }
                    }),
                    new DelegateCommand((o) =>
                    {
                        if (o is UtilityElement startingPoint)
                        {
                            RemoveStartingPoint(startingPoint.GlobalId);
                        }
                    }));
                _startingPoints.Add(model);
            }
        }

        private void RemoveStartingPoint(ArcGISFeature feature)
        {
            if (feature.FeatureTable is ArcGISFeatureTable table && !string.IsNullOrEmpty(table.GlobalIdField)
                   && feature.GetAttributeValue(table.GlobalIdField) is Guid globalId)
            {
                if (StartingPoints?.FirstOrDefault(f => f.Attributes["GlobalId"] is Guid guid
                   && guid.Equals(globalId)) is ArcGISFeature featureToDelete)
                {
                    StartingPoints.Remove(featureToDelete);
                }

                RemoveStartingPoint(globalId);
            }
        }

        private void RemoveStartingPoint(Guid globalId)
        {
            if (_startingPointGraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes["GlobalId"] is Guid guid
            && guid.Equals(globalId)) is Graphic graphic)
            {
                _startingPointGraphicsOverlay.Graphics.Remove(graphic);
            }

            if (_startingPoints.FirstOrDefault(p => p.StartingPoint.GlobalId.Equals(globalId)) is StartingPointModel model)
            {
                _startingPoints.Remove(model);
            }
        }

        private void UpdateStartingPoint(UtilityElement startingPoint)
        {
            if (_startingPointGraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes["GlobalId"] is Guid guid
            && guid.Equals(startingPoint.GlobalId)) is Graphic graphic)
            {
                if (graphic.Attributes["Geometry"] is string json && Geometry.Geometry.FromJson(json) is Polyline polyline)
                {
                    if (polyline != null && startingPoint.FractionAlongEdge is double fractionAlongEdge)
                    {
                        var part = polyline.Parts.FirstOrDefault();
                        if (fractionAlongEdge == 0 && part?.StartPoint is MapPoint startPoint)
                        {
                            graphic.Geometry = startPoint;
                        }
                        else if (fractionAlongEdge == 1 && part?.StartPoint is MapPoint endPoint)
                        {
                            graphic.Geometry = endPoint;
                        }
                        else if (fractionAlongEdge > 0)
                        {
                            var length = GeometryEngine.Length(polyline);
                            graphic.Geometry = GeometryEngine.CreatePointAlong(polyline, length * fractionAlongEdge);
                        }
                    }
                }
            }
        }

        private void ZoomToStartingPoint(UtilityElement startingPoint)
        {
            if (_startingPointGraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes["GlobalId"] is Guid guid
                && guid.Equals(startingPoint.GlobalId)) is Graphic graphic
                && graphic.Geometry is Geometry.Geometry geometry)
            {
                RequestedViewpoint = new Viewpoint(geometry);
            }
        }

 



        private async Task LoadTraceTypesAsync()
        {
            try
            {
                IsBusy = true;
                Status = "Loading trace types...";

                if (Map != null
                    && SelectedUtilityNetwork is UtilityNetwork utilityNetwork)
                {
                    if (_getTraceTypesCts != null)
                    {
                        _getTraceTypesCts.Cancel();
                    }

                    _getTraceTypesCts = new CancellationTokenSource();
                    var traceTypes = await Map.GetNamedTraceConfigurationsFromUtilityNetworkAsync(utilityNetwork, _getTraceTypesCts.Token);
                    foreach (var traceType in traceTypes)
                    {
                        _traceTypes.Add(traceType);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Do nothing when canceled
            }
            catch (Exception ex)
            {
                Status = $"Loading trace types failed ({ex.GetType().Name}):{ex.Message}";
            }
            finally
            {
                Status = GetStatus();
                IsBusy = false;
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

        internal async Task TraceAsync()
        {
            Exception? error = null;
            var traceResults = Enumerable.Empty<UtilityTraceResult>();

            try
            {
                Status = "Running a trace type...";
                IsBusy = true;

                ClearResults();

                if (SelectedUtilityNetwork == null)
                {
                    throw new InvalidOperationException("No utility network selected.");
                }

                if (SelectedTraceType == null)
                {
                    throw new InvalidOperationException("No trace type selected.");
                }

                if (TraceParameters == null)
                {
                    throw new InvalidOperationException("No trace parameters created.");
                }

                if (_traceCts != null)
                {
                    _traceCts.Cancel();
                }

                _traceCts = new CancellationTokenSource();
                traceResults = await SelectedUtilityNetwork.TraceAsync(TraceParameters, _traceCts.Token);

                Envelope? elementExtent = null;

                foreach (var traceResult in traceResults)
                {
                    if (traceResult.Warnings.Count > 0)
                    {
                        Status = $"Trace warnings: {string.Join("\n", traceResult.Warnings)}";
                    }

                    if (traceResult is UtilityElementTraceResult elementTraceResult)
                    {
                        Status = $"{elementTraceResult.Elements.Count} elements found.";

                        if (_getFeaturesForElementsCts != null)
                        {
                            _getFeaturesForElementsCts.Cancel();
                        }

                        _getFeaturesForElementsCts = new CancellationTokenSource();
                        var features = await SelectedUtilityNetwork.GetFeaturesForElementsAsync(elementTraceResult.Elements, _getFeaturesForElementsCts.Token);

                        Status = $"Selecting {features.Count()} features.";

                        bool getElementExtent = AutoZoomToTraceResults && !traceResults.Any(r => r is UtilityGeometryTraceResult);

                        if (Map.OperationalLayers != null)
                        {
                            int selected = 0;
                            foreach (var featureLayer in GetFeatureLayer(Map.OperationalLayers))
                            {
                                var featuresInLayer = features.Where(f => f.FeatureTable == featureLayer.FeatureTable);

                                if (getElementExtent)
                                {
                                    foreach (var feature in featuresInLayer)
                                    {
                                        if (feature.Geometry?.Extent is Envelope extent)
                                        {
                                            if (elementExtent == null)
                                            {
                                                elementExtent = extent;
                                            }
                                            else
                                            {
                                                if (elementExtent.SpatialReference?.IsEqual(extent.SpatialReference) == false
                                                    && GeometryEngine.Project(extent, elementExtent.SpatialReference) is Envelope projectedExtent)
                                                {
                                                    extent = projectedExtent;
                                                }

                                                if (GeometryEngine.CombineExtents(elementExtent, extent) is Envelope combinedExtents)
                                                {
                                                    elementExtent = combinedExtents;
                                                }
                                            }
                                        }
                                    }
                                }

                                featureLayer.SelectFeatures(featuresInLayer);
                                var selectedFeatures = await featureLayer.GetSelectedFeaturesAsync();
                                selected += selectedFeatures.Count();
                            }

                            Status = $"{features.Count()} features selected.";
                        }
                    }
                    else if (traceResult is UtilityGeometryTraceResult geometryTraceResult)
                    {
                        if (geometryTraceResult.Multipoint is Multipoint multipoint)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(multipoint, ResultPointSymbol ?? _defaultResultPointSymbol));
                        }

                        if (geometryTraceResult.Polyline is Polyline polyline)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(polyline, ResultLineSymbol ?? _defaultResultLineSymbol));
                        }

                        if (geometryTraceResult.Polygon is Polygon polygon)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(polygon, ResultFillSymbol ?? _defaultResultFillSymbol));
                        }

                        Status = $"'{_resultGraphicsOverlay.Graphics.Count}' aggregated geometries found.";
                    }
                    else if (traceResult is UtilityFunctionTraceResult functionTraceResult)
                    {
                        foreach (var functionOutput in functionTraceResult.FunctionOutputs)
                        {
                            _functionResults.Add(functionOutput);
                        }

                        Status = $"'{_functionResults.Count}' function result(s) found.";
                    }
                }

                var resultExtent = elementExtent ?? _resultGraphicsOverlay.Extent;
                if (AutoZoomToTraceResults && resultExtent?.IsEmpty == false &&
                    GeometryEngine.Buffer(resultExtent, 15) is Geometry.Geometry bufferedGeometry)
                {
                    RequestedViewpoint = new Viewpoint(bufferedGeometry);
                }
            }
            catch (TaskCanceledException)
            {
                // Do nothing when canceled
            }
            catch (Exception ex)
            {
                Status = $"Running a trace type failed ({ex.GetType().Name}): {ex.Message}";
                error = ex;
            }
            finally
            {
                IsBusy = false;
                Status = GetStatus();
            }

            if (TraceParameters is UtilityTraceParameters traceParameters)
            {
                CurrentResult = new UtilityTraceOperationResult { Paremters = traceParameters, Results = traceResults, Error = error};
            }
        }

        private void OnStartingPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    foreach (var model in _startingPoints.ToList())
                    {
                        RemoveStartingPoint(model.StartingPoint.GlobalId);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        if (item is ArcGISFeature feature)
                        {
                            RemoveStartingPoint(feature);
                        }
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        if (item is ArcGISFeature feature)
                        {
                            AddStartingPoint(feature);
                        }
                    }
                }
            });
        }

        internal void HandleStartingPointsChanged()
        {
            try
            {
                IsBusy = true;
                _startingPoints.Clear();

                // TODO = with refactoring, we might be able to rule this out
                if (StartingPoints is INotifyCollectionChanged incc)
                {
                    var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                    {
                        OnEventAction = (instance, source, eventArgs) => OnStartingPointsCollectionChanged(source, eventArgs),
                        OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                    };
                    incc.CollectionChanged += listener.OnEvent;
                }

                if (StartingPoints != null)
                {
                    foreach (var feature in StartingPoints)
                    {
                        AddStartingPoint(feature);
                    }
                }
            }
            catch (Exception ex)
            {
                Status = $"Loading starting points failed ({ex.GetType().Name}): {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal void HandleStartingPointSymbolChanged()
        {
            foreach (var graphic in _startingPointGraphicsOverlay.Graphics)
            {
                graphic.Symbol = StartingPointSymbol;
            }
        }

        internal void HandleResultSymbolChanged(GeometryType geometryType)
        {
            var symbol = GetSymbol(geometryType);

            foreach (var graphic in _resultGraphicsOverlay.Graphics)
            {
                if (graphic.Geometry?.GeometryType == geometryType)
                {
                    graphic.Symbol = symbol;
                }
            }
        }

        private Symbol? GetSymbol(GeometryType geometryType)
        {
            switch (geometryType)
            {
                case GeometryType.Multipoint:
                    return ResultPointSymbol;
                case GeometryType.Polyline:
                    return ResultLineSymbol;
                case GeometryType.Polygon:
                    return ResultFillSymbol;
            }

            return null;
        }

        public string GetStatus()
        {
            var status = new StringBuilder();
            //if (!(_traceTool.GeoView is MapView mapView) ||
            //    (mapView.Map is Map map && map.UtilityNetworks.Count == 0))
            if (UtilityNetworks == null || UtilityNetworks.Count == 0)
            {
                status.AppendLine("No utility networks found.");
            }
            else if (SelectedUtilityNetwork == null && _utilityNetworks.Count > 1)
            {
                status.AppendLine("Select a utility network.");
            }
            else if (SelectedUtilityNetwork != null && _traceTypes.Count == 0)
            {
                status.AppendLine("No trace types found.");
            }
            else if (SelectedTraceType == null && _traceTypes.Count > 1)
            {
                status.AppendLine("Select a trace type.");
            }
            else if (SelectedUtilityNetwork != null)
            {
                if (IsAddingStartingPoints)
                {
                    status.AppendLine("Tap a feature to identify a starting point.");
                }
                else
                {
                    status.AppendLine("Toggle 'Add Starting Points' button.");
                }

                if (SelectedTraceType is UtilityNamedTraceConfiguration traceType)
                {
                    var minimum = traceType.MinimumStartingLocations == UtilityMinimumStartingLocations.One ? 1 : 2;
                    if (_startingPoints.Count >= minimum)
                    {
                        status.AppendLine("Or click 'Trace' button.");
                    }
                }
            }

            return status.ToString();
        }

        internal void Reset(bool clearUtilityNetworks = false, bool clearTraceTypes = false)
        {
            if (clearUtilityNetworks)
            {
                _utilityNetworks.Clear();
                SelectedUtilityNetwork = null;
            }

            if (clearTraceTypes)
            {
                _traceTypes.Clear();
                SelectedTraceType = null;
            }

            StartingPoints = null;
            ControllerStartingPoints.Clear();
            _startingPointGraphicsOverlay.Graphics.Clear();

            _getTraceTypesCts?.Cancel();
            _traceCts?.Cancel();
            _getFeaturesForElementsCts?.Cancel();

            IsBusy = false;
            Status = GetStatus();

            ClearResults();
        }

        private void ClearResults()
        {
            _resultGraphicsOverlay.Graphics.Clear();
            _functionResults.Clear();
            CurrentResult = null;
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

        #region replacing interface
        public event PropertyChangedEventHandler PropertyChanged;
        
        // TODO = make INPC
        private IList<ArcGISFeature>? _tvStartingPoints;
        public IList<ArcGISFeature>? StartingPoints
        {
            get => _tvStartingPoints;
            set
            {
                if (value != _tvStartingPoints)
                {
                    _tvStartingPoints = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isAddingStartingPoints;
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

        private Tuple<CalloutDefinition, MapPoint>? _requestedCallout;
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

        private bool _autoZoomToTraceResults;
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

        private Symbol? _startingPointSymbol;
        private Symbol? _resultPointSymbol;
        private Symbol? _resultLineSymbol;
        private Symbol? _resultFillSymbol;

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

        private Map _map;

        public Map? Map
        {
            get => _map;
            set
            {
                if (value != _map)
                {
                    _map = value;
                    OnPropertyChanged();
                }
            }
        }

        private Viewpoint? _viewpoint;
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

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (value != _isBusy)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showUtilityNetworks;

        public bool ShowUtilityNetworks
        {
            get => _showUtilityNetworks;
            set
            {
                if (value != _showUtilityNetworks)
                {
                    _showUtilityNetworks = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showTraceTypes;

        public bool ShowTraceTypes
        {
            get => _showTraceTypes;
            set
            {
                if (value != _showTraceTypes)
                {
                    _showTraceTypes = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showStartingPoints;

        public bool ShowStartingPoints
        {
            get => _showStartingPoints;
            set
            {
                if (value != _showStartingPoints)
                {
                    _showStartingPoints = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showFunctionResults;

        public bool ShowFunctionResults
        {
            get => _showFunctionResults;
            set
            {
                if (value != _showFunctionResults)
                {
                    _showFunctionResults = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableTrace;

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

        private string _status;

        public string Status
        {
            get => _status;
            set
            {
                if (value != _status)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        private UtilityTraceOperationResult? _currentResult;

        public UtilityTraceOperationResult? CurrentResult
        {
            get => _currentResult;
            set
            {
                if (value != _currentResult)
                {
                    _currentResult = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class UtilityTraceOperationResult
    {
        public UtilityTraceParameters Paremters { get; set; }
        public IEnumerable<UtilityTraceResult>? Results { get; set; }
        public Exception? Error { get; set; }
        public IList<string> Warnings { get; set; }
    }
}
#endif