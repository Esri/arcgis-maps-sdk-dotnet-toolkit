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
    internal class UtilityNetworkTraceToolController : IDisposable
    {
        private readonly IUtilityNetworkTraceTool _traceTool;
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

        internal ObservableCollection<StartingPointModel> StartingPoints => _startingPoints;

        private readonly Symbol _defaultStartingLocationSymbol
            = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.LimeGreen, 20d);

        private readonly Symbol _defaultResultPointSymbol
            = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Blue, 20d);

        private readonly Symbol _defaultResultLineSymbol
            = new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, Color.Blue, 5d);

        private readonly Symbol _defaultResultFillSymbol
            = new SimpleFillSymbol(SimpleFillSymbolStyle.ForwardDiagonal, Color.Blue,
                new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 2d));

        private readonly GraphicsOverlay _startingPointGraphicsOverlay = new GraphicsOverlay();
        private readonly GraphicsOverlay _resultGraphicsOverlay = new GraphicsOverlay();

        private readonly SynchronizationContext _synchronizationContext;

        private CancellationTokenSource? _getTraceTypesCts;
        private CancellationTokenSource? _identifyLayersCts;
        private CancellationTokenSource? _traceCts;
        private CancellationTokenSource? _getFeaturesForElementsCts;

        public UtilityNetworkTraceToolController(IUtilityNetworkTraceTool traceTool)
        {
            _traceTool = traceTool;
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();

            _utilityNetworks.CollectionChanged += (s, e) =>
            {
                if (_utilityNetworks.Count == 1)
                {
                    SelectedUtilityNetwork = _utilityNetworks[0];
                }

                _traceTool.UpdateUtilityNetworksVisibility(isVisible: _utilityNetworks.Count > 1);
                if (_utilityNetworks.Count > 1)
                {
                    _traceTool.SetStatus($"Select a utility network");
                }
            };

            _traceTypes.CollectionChanged += (s, e) =>
            {
                if (_traceTypes.Count == 1)
                {
                    SelectedTraceType = _traceTypes[0];
                }

                _traceTool.UpdateTraceTypesVisibility(isVisible: _traceTypes.Count > 1);
                if (_traceTypes.Count > 1)
                {
                    _traceTool.SetStatus($"Select a trace type");
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
                    _traceTool.EnableTrace(traceParameters?.StartingLocations.Count >= minimum);
                }

                _traceTool.UpdateStartingPointsVisibility(isVisible: _startingPoints.Count > 0);
                if (_startingPoints.Any())
                {
                    _traceTool.SetStatus($"{_startingPoints.Count} starting points");
                }
            };

            _functionResults.CollectionChanged += (s, e) =>
            {
                _traceTool.UpdateFunctionResultsVisibility(isVisible: _functionResults.Count > 0);
            };
        }

        private void RunOnUIThread(Action action)
        {
            _synchronizationContext?.Post((o) => action?.Invoke(), null);
        }

        private UtilityNetwork? _selectedUtilityNetwork;

        private UtilityNetwork? SelectedUtilityNetwork
        {
            get => _selectedUtilityNetwork;
            set
            {
                if (_selectedUtilityNetwork != value)
                {
                    Reset(clearTraceTypes: true);
                    _selectedUtilityNetwork = value;
                    _ignoreEventsFlag = true;
                    _traceTool.SelectUtilityNetwork(_selectedUtilityNetwork);
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

        private UtilityNamedTraceConfiguration? SelectedTraceType
        {
            get => _selectedTraceType;
            set
            {
                if (_selectedTraceType != value)
                {
                    _selectedTraceType = value;
                    if (!_ignoreEventsFlag)
                    {
                        _traceTool.SelectTraceType(_selectedTraceType);
                    }

                    if (_selectedTraceType is UtilityNamedTraceConfiguration traceType)
                    {
                        TraceParameters = new UtilityTraceParameters(traceType,
                            _startingPoints.Select(p => p.StartingPoint));
                        var minimum = traceType.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ? 2 : 1;
                        _traceTool.EnableTrace(TraceParameters?.StartingLocations.Count >= minimum);
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

        private StartingPointModel? SelectedStartingPoint
        {
            get => _selectedStartingPoint;
            set
            {
                if (_selectedStartingPoint != value)
                {
                    _selectedStartingPoint = value;
                    if (!_ignoreEventsFlag)
                    {
                        _traceTool.SelectStartingPoint(_selectedStartingPoint);
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
            _traceTool.GeoView?.DismissCallout();

            if (SelectedStartingPoint?.StartingPoint is UtilityElement startingPoint
             && _startingPointGraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes["GlobalId"] is Guid guid
             && guid.Equals(startingPoint.GlobalId)) is Graphic graphic)
            {
                _startingPointGraphicsOverlay.ClearSelection();
                graphic.IsSelected = true;
                if (_traceTool.GeoView is GeoView geoView && graphic.Geometry is MapPoint location)
                {
                    var calloutDefinition = new CalloutDefinition(startingPoint.NetworkSource.Name, startingPoint.AssetGroup.Name);
                    geoView.ShowCalloutAt(location, calloutDefinition);
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
                    _traceTool.SetStatus(GetStatus());
                }
            }
        }

        private void OnUtilityNetworksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _traceTool.SetIsBusy(true);
            _traceTool.SetStatus("Loading utility networks...");

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
                    _traceTool.SetIsBusy(true);
                    _traceTool.SetStatus("Loading utility networks...");
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
                    _traceTool.SetStatus($"Loading utility networks failed ({ex.GetType().Name}): {ex.Message}");
                    _traceTool.SetIsBusy(false);
                }
            });
        }

        private void OnGeoModelPropertyChanged(object? sender, object? e)
        {
            GeoModel? geoModel = _traceTool.GeoView is MapView mapView ? mapView.Map :
                (_traceTool.GeoView is SceneView sceneView ? sceneView.Scene : null);

            if (geoModel != null)
            {
                Reset(true);
                _traceTool.SetIsBusy(true);
                _traceTool.SetStatus("Loading utility networks...");

                if (geoModel.LoadStatus == LoadStatus.Loaded)
                {
                    OnGeoModelLoaded(geoModel, EventArgs.Empty);
                }
                else
                {
                    var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(geoModel)
                    {
                        OnEventAction = (instance, source, eventArgs) => OnGeoModelLoaded(source, eventArgs),
                        OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent,
                    };
                    geoModel.Loaded += listener.OnEvent;
                }
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        private void OnGeoViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeoModel))
            {
                OnGeoModelPropertyChanged(sender, e);
            }
        }

        internal void HandleGeoViewChanged(GeoView? oldGeoView, GeoView? newGeoView)
        {
            Reset(true);

            _traceTool.SetIsBusy(true);
            _traceTool.SetStatus("Loading utility networks...");

            var graphicsOverlays = new[] { _startingPointGraphicsOverlay, _resultGraphicsOverlay };

            if (oldGeoView != null)
            {
                oldGeoView.GeoViewTapped -= OnGeoViewTapped;
#if XAMARIN_FORMS
                if (oldGeoView is INotifyPropertyChanged oldNpc)
                {
                    oldNpc.PropertyChanged -= OnGeoViewPropertyChanged;
                }
#elif NETFX_CORE
                if (oldGeoView is MapView)
                {
                    oldGeoView.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
                }
                else if (oldGeoView is SceneView)
                {
                    oldGeoView.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
                }
#else
                if (oldGeoView is MapView)
                {
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(oldGeoView, OnGeoModelPropertyChanged);
                }
                else if (oldGeoView is SceneView)
                {
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(oldGeoView, OnGeoModelPropertyChanged);
                }
#endif
                if (oldGeoView.GraphicsOverlays != null)
                {
                    foreach (var graphicsOverlay in graphicsOverlays)
                    {
                        if (oldGeoView.GraphicsOverlays.Contains(graphicsOverlay))
                        {
                            oldGeoView.GraphicsOverlays.Remove(graphicsOverlay);
                        }
                    }
                }
            }

            if (newGeoView != null)
            {
                newGeoView.GeoViewTapped += OnGeoViewTapped;

#if XAMARIN
                if (newGeoView is INotifyPropertyChanged newNpc)
                {
                    newNpc.PropertyChanged += OnGeoViewPropertyChanged;
                }

#elif NETFX_CORE
                if (newGeoView is MapView)
                {
                    _propertyChangedCallbackToken = newGeoView.RegisterPropertyChangedCallback(MapView.MapProperty, OnGeoModelPropertyChanged);
                }
                else if (newGeoView is SceneView)
                {
                    _propertyChangedCallbackToken = newGeoView.RegisterPropertyChangedCallback(SceneView.SceneProperty, OnGeoModelPropertyChanged);
                }
#else
                if (newGeoView is MapView)
                {
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(newGeoView, OnGeoModelPropertyChanged);
                }
                else if (newGeoView is SceneView)
                {
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(newGeoView, OnGeoModelPropertyChanged);
                }
#endif
                if (newGeoView.GraphicsOverlays != null)
                {
                    foreach (var graphicsOverlay in graphicsOverlays)
                    {
                        if (!newGeoView.GraphicsOverlays.Contains(graphicsOverlay))
                        {
                            newGeoView.GraphicsOverlays.Add(graphicsOverlay);
                        }
                    }
                }

                OnGeoModelPropertyChanged(null, null);
            }
        }

        private void AddStartingPoint(ArcGISFeature feature, MapPoint? location = null)
        {
            Geometry.Geometry? geometry = feature.Geometry;
            UtilityElement? element = null;
            try
            {
                element = SelectedUtilityNetwork?.CreateElement(feature);
            }
            catch (Exception ex)
            {
                _traceTool.SetStatus($"CreateElement failed ({ex.GetType().Name}): {ex.Message}");
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

                if (_traceTool.StartingPoints == null)
                {
                    _traceTool.StartingPoints = new ObservableCollection<ArcGISFeature>();
                    if (_traceTool.StartingPoints is INotifyCollectionChanged incc)
                    {
                        var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                        {
                            OnEventAction = (instance, source, eventArgs) => OnStartingPointsCollectionChanged(source, eventArgs),
                            OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                        };
                        incc.CollectionChanged += listener.OnEvent;
                    }
                }

                if (!_traceTool.StartingPoints.Any(f => f.Attributes["GlobalId"] is Guid guid
                   && guid.Equals(element.GlobalId)))
                {
                    _traceTool.StartingPoints.Add(feature);
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
                graphic.Symbol = _traceTool.StartingPointSymbol ?? _defaultStartingLocationSymbol;
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
                if (_traceTool.StartingPoints?.FirstOrDefault(f => f.Attributes["GlobalId"] is Guid guid
                   && guid.Equals(globalId)) is ArcGISFeature featureToDelete)
                {
                    _traceTool.StartingPoints.Remove(featureToDelete);
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
            if (_traceTool.GeoView is GeoView geoView
                && _startingPointGraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes["GlobalId"] is Guid guid
                && guid.Equals(startingPoint.GlobalId)) is Graphic graphic
                && graphic.Geometry is Geometry.Geometry geometry)
            {
                geoView.SetViewpoint(new Viewpoint(geometry));
            }
        }

        private ArcGISFeature? GetFeature(IdentifyLayerResult layerResult)
        {
            foreach (var geoElement in layerResult.GeoElements)
            {
                if (geoElement is ArcGISFeature feature)
                {
                    return feature;
                }
            }

            return GetFeature(layerResult.SublayerResults);
        }

        private ArcGISFeature? GetFeature(IEnumerable<IdentifyLayerResult> layerResults)
        {
            foreach (var layerResult in layerResults)
            {
                if (GetFeature(layerResult) is ArcGISFeature element)
                {
                    return element;
                }
            }

            return null;
        }

        private async void OnGeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            if (e.Handled || !_traceTool.IsAddingStartingPoints || SelectedUtilityNetwork == null)
            {
                return;
            }

            try
            {
                _traceTool.SetStatus("Identifying a starting point...");
                _traceTool.SetIsBusy(true);

                if (sender is GeoView geoView)
                {
                    if (_identifyLayersCts != null)
                    {
                        _identifyLayersCts.Cancel();
                    }

                    _identifyLayersCts = new CancellationTokenSource();
                    var identifyResults = await geoView.IdentifyLayersAsync(e.Position, 10d, false, _identifyLayersCts.Token);

                    foreach (var identifyResult in identifyResults)
                    {
                        if (GetFeature(identifyResult) is ArcGISFeature feature)
                        {
                            AddStartingPoint(feature, e.Location);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Do nothing when canceled
            }
            catch (Exception ex)
            {
                _traceTool.SetStatus($"Identifying a starting point failed ({ex.GetType().Name}): {ex.Message}");
            }
            finally
            {
                _traceTool.SetStatus(GetStatus());
                _traceTool.SetIsBusy(false);
            }
        }

        private async Task LoadTraceTypesAsync()
        {
            try
            {
                _traceTool.SetIsBusy(true);
                _traceTool.SetStatus("Loading trace types...");

                if (_traceTool.GeoView is MapView mapView && mapView.Map is Map map
                    && SelectedUtilityNetwork is UtilityNetwork utilityNetwork)
                {
                    if (_getTraceTypesCts != null)
                    {
                        _getTraceTypesCts.Cancel();
                    }

                    _getTraceTypesCts = new CancellationTokenSource();
                    var traceTypes = await map.GetNamedTraceConfigurationsFromUtilityNetworkAsync(utilityNetwork, _getTraceTypesCts.Token);
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
                _traceTool.SetStatus($"Loading trace types failed ({ex.GetType().Name}):{ex.Message}");
            }
            finally
            {
                _traceTool.SetStatus(GetStatus());
                _traceTool.SetIsBusy(false);
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
                _traceTool.SetStatus("Running a trace type...");
                _traceTool.SetIsBusy(true);

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
                        _traceTool.SetStatus($"Trace warnings: {string.Join("\n", traceResult.Warnings)}");
                    }

                    if (traceResult is UtilityElementTraceResult elementTraceResult)
                    {
                        _traceTool.SetStatus($"{elementTraceResult.Elements.Count} elements found.");

                        if (_getFeaturesForElementsCts != null)
                        {
                            _getFeaturesForElementsCts.Cancel();
                        }

                        _getFeaturesForElementsCts = new CancellationTokenSource();
                        var features = await SelectedUtilityNetwork.GetFeaturesForElementsAsync(elementTraceResult.Elements, _getFeaturesForElementsCts.Token);

                        _traceTool.SetStatus($"Selecting {features.Count()} features.");

                        bool getElementExtent = _traceTool.AutoZoomToTraceResults && !traceResults.Any(r => r is UtilityGeometryTraceResult);

                        if (_traceTool.GeoView is MapView mapView && mapView.Map is Map map && map.OperationalLayers != null)
                        {
                            int selected = 0;
                            foreach (var featureLayer in GetFeatureLayer(map.OperationalLayers))
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

                            _traceTool.SetStatus($"{features.Count()} features selected.");
                        }
                    }
                    else if (traceResult is UtilityGeometryTraceResult geometryTraceResult)
                    {
                        if (geometryTraceResult.Multipoint is Multipoint multipoint)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(multipoint, _traceTool.ResultPointSymbol ?? _defaultResultPointSymbol));
                        }

                        if (geometryTraceResult.Polyline is Polyline polyline)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(polyline, _traceTool.ResultLineSymbol ?? _defaultResultLineSymbol));
                        }

                        if (geometryTraceResult.Polygon is Polygon polygon)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(polygon, _traceTool.ResultFillSymbol ?? _defaultResultFillSymbol));
                        }

                        _traceTool.SetStatus($"'{_resultGraphicsOverlay.Graphics.Count}' aggregated geometries found.");
                    }
                    else if (traceResult is UtilityFunctionTraceResult functionTraceResult)
                    {
                        foreach (var functionOutput in functionTraceResult.FunctionOutputs)
                        {
                            _functionResults.Add(functionOutput);
                        }

                        _traceTool.SetStatus($"'{_functionResults.Count}' function result(s) found.");
                    }
                }

                var resultExtent = elementExtent ?? _resultGraphicsOverlay.Extent;
                if (_traceTool.AutoZoomToTraceResults && _traceTool.GeoView is GeoView geoView && resultExtent?.IsEmpty == false &&
                    GeometryEngine.Buffer(resultExtent, 15) is Geometry.Geometry bufferedGeometry)
                {
                    _ = geoView.SetViewpointAsync(new Viewpoint(bufferedGeometry));
                }
            }
            catch (TaskCanceledException)
            {
                // Do nothing when canceled
            }
            catch (Exception ex)
            {
                _traceTool.SetStatus($"Running a trace type failed ({ex.GetType().Name}): {ex.Message}");
                error = ex;
            }
            finally
            {
                _traceTool.SetStatus(GetStatus());
                _traceTool.SetIsBusy(false);
            }

            if (TraceParameters is UtilityTraceParameters traceParameters)
            {
                _traceTool.NotifyUtilityNetworkTraceCompleted(traceParameters, traceResults, error);
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
                _traceTool.SetIsBusy(true);
                _startingPoints.Clear();

                if (_traceTool.StartingPoints is INotifyCollectionChanged incc)
                {
                    var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                    {
                        OnEventAction = (instance, source, eventArgs) => OnStartingPointsCollectionChanged(source, eventArgs),
                        OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                    };
                    incc.CollectionChanged += listener.OnEvent;
                }

                if (_traceTool.StartingPoints != null)
                {
                    foreach (var feature in _traceTool.StartingPoints)
                    {
                        AddStartingPoint(feature);
                    }
                }
            }
            catch (Exception ex)
            {
                _traceTool.SetStatus($"Loading starting points failed ({ex.GetType().Name}): {ex.Message}");
            }
            finally
            {
                _traceTool.SetIsBusy(false);
            }
        }

        internal void HandleAddStartingPointToggled(bool isAddingStartingPoints)
        {
            _traceTool.IsAddingStartingPoints = isAddingStartingPoints;
        }

        internal void HandleStartingPointSymbolChanged()
        {
            foreach (var graphic in _startingPointGraphicsOverlay.Graphics)
            {
                graphic.Symbol = _traceTool.StartingPointSymbol;
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
                    return _traceTool.ResultPointSymbol;
                case GeometryType.Polyline:
                    return _traceTool.ResultLineSymbol;
                case GeometryType.Polygon:
                    return _traceTool.ResultFillSymbol;
            }

            return null;
        }

        private string GetStatus()
        {
            var status = new StringBuilder();
            if (!(_traceTool.GeoView is MapView mapView) ||
                (mapView.Map is Map map && map.UtilityNetworks.Count == 0))
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
                if (_traceTool.IsAddingStartingPoints)
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

            _traceTool.StartingPoints?.Clear();
            _startingPoints.Clear();
            _startingPointGraphicsOverlay.Graphics.Clear();

            _getTraceTypesCts?.Cancel();
            _identifyLayersCts?.Cancel();
            _traceCts?.Cancel();
            _getFeaturesForElementsCts?.Cancel();

            _traceTool.SetIsBusy(false);
            _traceTool.SetStatus(GetStatus());

            ClearResults();
        }

        private void ClearResults()
        {
            _resultGraphicsOverlay.Graphics.Clear();
            _functionResults.Clear();

            if (_traceTool.GeoView is MapView mapView && mapView.Map is Map map)
            {
                foreach (var layer in map.OperationalLayers)
                {
                    if (layer is FeatureLayer featureLayer)
                    {
                        featureLayer.ClearSelection();
                    }
                }
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            try
            {
                _getTraceTypesCts?.Dispose();
                _identifyLayersCts?.Dispose();
                _traceCts?.Dispose();
                _getFeaturesForElementsCts?.Dispose();
            }
            catch
            {
            }
        }
    }
}
#endif