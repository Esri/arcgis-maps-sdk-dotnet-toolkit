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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ToggleButton = Windows.UI.Xaml.Controls.ToggleSwitch;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Represents a control that enables user to perform trace analysis with pre-configured trace types.
    /// </summary>
    public partial class UtilityNetworkTraceTool : Control
    {
        private CancellationTokenSource? _identifyLayersCts;
        private readonly UtilityNetworkTraceToolController _controller;
        private List<GraphicsOverlay> _resultOverlays = new List<GraphicsOverlay>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityNetworkTraceTool"/> class.
        /// </summary>
        public UtilityNetworkTraceTool()
        {
            DefaultStyleKey = typeof(UtilityNetworkTraceTool);
            _controller = new UtilityNetworkTraceToolController();
            _controller.AutoZoomToTraceResults = AutoZoomToTraceResults;
            _controller.PropertyChanged += Controller_PropertyChanged;
            UtilityNetworks = _controller.UtilityNetworks;
            StartingPoints = _controller.StartingPoints;
            NamedTraceConfigurations = _controller.TraceTypes;
            SelectedTraceConfiguration = _controller.SelectedTraceType;
            TraceResults = _controller.Results;

            _controller.Results.CollectionChanged += Results_CollectionChanged;

            DeleteCommand = new DelegateCommand(HandleDeleteStartingPointCommand);
            ZoomToCommand = new DelegateCommand(HandleZoomToStartingPointCommand);
            ResetStartingPointsCommand = new DelegateCommand(HandleResetStartingPointsCommand);
            SetIsAddingStartingPointsCommand = new DelegateCommand(HandleSetIsAddingStartingPointsCommand);
            RunTraceCommand = new DelegateCommand(HandleRunTraceCommand);
            DeleteResultCommand = new DelegateCommand(HandleDeleteResultCommand);
            ZoomToResultCommand = new DelegateCommand(HandleZoomToResultCommand);
            CancelOperationCommand = new DelegateCommand(HandleCancelCommand);

            ResultFillSymbol = _controller.ResultFillSymbol;
            ResultLineSymbol = _controller.ResultLineSymbol;
            ResultPointSymbol = _controller.ResultPointSymbol;
        }

        /// <summary>
        /// Adds a starting point, which will be transformed into a <see cref="StartingPointModel"/> and added to <see cref="StartingPoints"/>.
        /// </summary>
        /// <param name="feature">The feature to use as the basis for the starting point.</param>
        /// <param name="location">Optional location to use, which will be used to specify the location along the line for line features.</param>
        public void AddStartingPoint(ArcGISFeature feature, MapPoint? location)
        {
            _controller.AddStartingPoint(feature, location);
        }

        #region Command implementations

        private void HandleCancelCommand(object? parameter)
        {
            _identifyLayersCts?.Cancel();
            _controller._getFeaturesForElementsCts?.Cancel();
            _controller._traceCts?.Cancel();
        }

        private async void HandleRunTraceCommand(object? parameter)
        {
            try
            {
                await _controller.TraceAsync();
                if (_controller.Results.Any())
                {
                    #if !WINDOWS_UWP
                    _tabControl?.SetValue(TabControl.SelectedIndexProperty, 1);
                    #endif
                }
            }
            catch (Exception)
            {
                // TODO
            }
        }

        private void HandleSetIsAddingStartingPointsCommand(object? parameter)
        {
            if (parameter is bool newValue)
            {
                IsAddingStartingPoints = newValue;
            }
            else if (parameter is string stringValue)
            {
                var trimmed = stringValue.Trim().ToLower();
                if (trimmed == "true")
                {
                    IsAddingStartingPoints = true;
                }
                else if (trimmed == "false")
                {
                    IsAddingStartingPoints = false;
                }
            }
        }

        private void HandleResetStartingPointsCommand(object? parameter)
        {
            _controller.StartingPoints.Clear();
        }

        private void HandleDeleteStartingPointCommand(object? parameter)
        {
            if (SelectedStartingPoint != null)
            {
                StartingPoints?.Remove(SelectedStartingPoint);
            }
        }

        private void HandleZoomToStartingPointCommand(object? parameter)
        {
            if (parameter is StartingPointModel selectedStartingPoint && selectedStartingPoint.ZoomToExtent is Envelope zoomEnvelope)
            {
                GeoView?.SetViewpoint(new Viewpoint(zoomEnvelope));
            }
            else if (SelectedStartingPoint is StartingPointModel selectedSP && selectedSP.ZoomToExtent is Envelope selectedZoomEnvelope)
            {
                GeoView?.SetViewpoint(new Viewpoint(selectedZoomEnvelope));
            }
        }

        private void HandleDeleteResultCommand(object? parameter)
        {
            if (parameter is UtilityTraceOperationResult targetResult)
            {
                TraceResults?.Remove(targetResult);

                // Go back to the options tab automatically if this was the last trace result
                if (!(TraceResults?.Any() ?? false))
                {
                    #if !WINDOWS_UWP
                    _tabControl?.SetValue(TabControl.SelectedIndexProperty, 0);
                    #endif
                }
            }
            else if (TraceResults != null)
            {
                foreach (var item in TraceResults)
                {
                    item.AreFeaturesSelected = false;
                }

                TraceResults?.Clear();
            }
        }

        private void HandleZoomToResultCommand(object? parameter)
        {
            if (parameter is UtilityTraceOperationResult targetResult)
            {
                var graphicsExtent = targetResult.ResultOverlay.Extent;
                var featureExtent = (targetResult.Features?.Any() ?? false) ? GeometryEngine.CombineExtents(targetResult.Features.Select(m => m.Geometry).OfType<Geometry.Geometry>()) : null;
                if (targetResult.ResultOverlay.Graphics.Any() && featureExtent != null && graphicsExtent != null && !graphicsExtent.IsEmpty)
                {
                    GeoView?.SetViewpoint(new Viewpoint(GeometryEngine.CombineExtents(graphicsExtent, featureExtent)));
                }
                else if (featureExtent != null)
                {
                    GeoView?.SetViewpoint(new Viewpoint(featureExtent));
                }
                else if (targetResult.ResultOverlay.Graphics.Any() && graphicsExtent != null && !graphicsExtent.IsEmpty)
                {
                    GeoView?.SetViewpoint(new Viewpoint(graphicsExtent));
                }
            }
        }

        #endregion Command implementations

        #region Non-static event handlers

        private void Results_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset && GeoView?.GraphicsOverlays is GraphicsOverlayCollection visualizedOverlays)
            {
                foreach (var item in _resultOverlays)
                {
                    if (visualizedOverlays.Contains(item))
                    {
                        visualizedOverlays.Remove(item);
                    }
                }

                _resultOverlays = new List<GraphicsOverlay>();
                #if !WINDOWS_UWP
                _tabControl?.SetValue(TabControl.SelectedIndexProperty, 0);
                #endif
                SelectedResult = null;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<UtilityTraceOperationResult>())
                {
                    if (GeoView?.GraphicsOverlays?.Contains(item.ResultOverlay) ?? false)
                    {
                        GeoView.GraphicsOverlays.Remove(item.ResultOverlay);
                    }

                    item.AreFeaturesSelected = false;
                }

                if (SelectedResult != null && e.OldItems.Contains(SelectedResult))
                {
                    SelectedResult = _controller.Results.LastOrDefault();
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<UtilityTraceOperationResult>())
                {
                    if (!(GeoView?.GraphicsOverlays?.Contains(item.ResultOverlay) ?? true))
                    {
                        _resultOverlays.Add(item.ResultOverlay);
                        GeoView.GraphicsOverlays.Insert(0, item.ResultOverlay);
                    }

                    UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(item.Parameters, item.RawResults));
                    if (item?.Error != null)
                    {
                        UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(item.Parameters, item.Error));
                    }
                }

                if (e.NewItems.Count == 1)
                {
                    SelectedResult = e.NewItems[0] as UtilityTraceOperationResult;
                }
            }
        }

        private void Controller_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_controller.EnableTrace):
                    IsReadyToTrace = _controller.EnableTrace;
                    break;
                case nameof(_controller.RequestedViewpoint):
                    if (_controller.RequestedViewpoint != null)
                    {
                        GeoView?.SetViewpoint(_controller.RequestedViewpoint);
                    }

                    break;
                case nameof(_controller.RequestedCallout):
                    if (_controller.RequestedCallout == null)
                    {
                        GeoView?.DismissCallout();
                    }
                    else
                    {
                        GeoView?.ShowCalloutAt(_controller.RequestedCallout.Item2, _controller.RequestedCallout.Item1);
                    }

                    break;
                case nameof(_controller.SelectedUtilityNetwork):
                    SelectedUtilityNetwork = _controller.SelectedUtilityNetwork;
                    UtilityNetworkChanged?.Invoke(this, new UtilityNetworkChangedEventArgs(SelectedUtilityNetwork));
                    break;
                case nameof(_controller.SelectedTraceType):
                    SelectedTraceConfiguration = _controller.SelectedTraceType;
                    break;
                case nameof(_controller.SelectedStartingPoint):
                    SelectedStartingPoint = _controller.SelectedStartingPoint;
                    break;
                case nameof(_controller.TraceName):
                    TraceInProgressName = _controller.TraceName;
                    break;
                case nameof(_controller.IsRunningTrace):
                    IsTraceInProgress = _controller.IsRunningTrace;
                    break;
                case nameof(_controller.IsReadyToConfigure):
                    if (_ineligibleScrim != null)
                    {
                        _ineligibleScrim.Visibility = (!_controller.IsReadyToConfigure && !_controller.IsLoadingNetwork) ? Visibility.Visible : Visibility.Collapsed;
                    }

                    break;
                case nameof(_controller.IsLoadingNetwork):
                    if (_loadingScrim != null)
                    {
                        _loadingScrim.Visibility = _controller.IsLoadingNetwork ? Visibility.Visible : Visibility.Collapsed;
                    }

                    break;
                case nameof(_controller.IsAddingStartingPoints):
                    IsAddingStartingPoints = _controller.IsAddingStartingPoints;
                    break;
                case nameof(_controller.StartingPointSymbol):
                    break;
                case nameof(_controller.ResultPointSymbol):
                    ResultPointSymbol = _controller.ResultPointSymbol;
                    break;
                case nameof(_controller.ResultLineSymbol):
                    ResultLineSymbol = _controller.ResultLineSymbol;
                    break;
                case nameof(_controller.ResultFillSymbol):
                    ResultFillSymbol = _controller.ResultFillSymbol;
                    break;
                case nameof(_controller.DuplicatedTraceWarning):
                    ConfigurationDuplicatesExistingResultWarning = _controller.DuplicatedTraceWarning;
                    break;
                case nameof(_controller.InsufficientStartingPointsWarning):
                    HasInsufficientStartingPointsWarning = _controller.InsufficientStartingPointsWarning;
                    break;
                case nameof(_controller.TooManyStartingPointsWarning):
                    HasUnnecessaryStartingPointsWarning = _controller.TooManyStartingPointsWarning;
                    break;
            }
        }

        private async void OnGeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            if (e.Handled || !_controller.IsAddingStartingPoints || _controller.SelectedUtilityNetwork == null)
            {
                return;
            }

            try
            {
                IsIdentifyingStartingPoints = true;

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
                            _controller.AddStartingPoint(feature, e.Location);
                        }
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
                IsIdentifyingStartingPoints = false;
            }
        }

        private void OnGeoModelPropertyChanged(object? sender, object? e)
        {
            if (GeoView is MapView mv && mv.Map is Map newMap)
            {
                _controller.Map = newMap;
            }
            else
            {
                _controller.Map = null;
            }
        }

        private void OnGeoViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeoModel))
            {
                OnGeoModelPropertyChanged(sender, e);
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
        #endregion Non-static event handlers

        #region Static event handlers
        private static void OnResultVisualizationColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.ResultColor = (System.Drawing.Color)args.NewValue;
            }
        }

        private static void OnTraceInProgressNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UtilityNetworkTraceTool traceTool && args.NewValue is string newTraceName && traceTool._controller.TraceName != newTraceName)
            {
                traceTool._controller.TraceName = newTraceName;
            }
        }

        private static void OnResultLineSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.ResultLineSymbol = e.NewValue as Symbol;
            }
        }

        private static void OnResultFillSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.ResultFillSymbol = e.NewValue as Symbol;
            }
        }

        private static void OnSelectedUtilityNetworkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UtilityNetworkTraceTool tt)
            {
                if (tt._controller.SelectedUtilityNetwork != args.NewValue)
                {
                    tt._controller.SelectedUtilityNetwork = args.NewValue as UtilityNetwork;
                }
            }
        }

        private static void OnSelectedTraceConfigurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UtilityNetworkTraceTool tt)
            {
                if (tt._controller.SelectedTraceType != args.NewValue)
                {
                    tt._controller.SelectedTraceType = args.NewValue as UtilityNamedTraceConfiguration;
                }
            }
        }

        private static void OnAutoZoomToTraceResultsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.AutoZoomToTraceResults = (bool)args.NewValue;
            }
        }

        private static void OnStartingPointSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.HandleStartingPointSymbolChanged();
            }
        }

        private static void OnResultPointSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.ResultPointSymbol = e.NewValue as Symbol;
            }
        }

        private static void OnIsAddingStartingPointsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.IsAddingStartingPoints = (bool)args.NewValue;
            }
        }

        private static void OnSelectedStartingPointChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.SelectedStartingPoint = args.NewValue as StartingPointModel;
            }
        }

        private static void OnGeoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool.HandleGeoViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;

#endif
        internal void HandleGeoViewChanged(GeoView? oldGeoView, GeoView? newGeoView)
        {
            _controller.Reset(true);

            _controller.IsRunningTrace = true;

            var graphicsOverlays = new[] { _controller.StartingPointGraphicsOverlay };

            if (oldGeoView != null)
            {
                oldGeoView.GeoViewTapped -= OnGeoViewTapped;
#if NETFX_CORE
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

#if NETFX_CORE
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
        #endregion Static event handlers

        #region Convenience Properties

        /// <summary>
        /// Gets the command used to clear all starting points.
        /// </summary>
        public ICommand ResetStartingPointsCommand
        {
            get => (ICommand)GetValue(ResetStartingPointsCommandProperty);
            private set => SetValue(ResetStartingPointsCommandProperty, value);
        }

        /// <summary>
        /// Gets the command used to delete a starting point.
        /// </summary>
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            private set => SetValue(DeleteCommandProperty, value);
        }

        /// <summary>
        /// Gets the command used to zoom to a starting point.
        /// </summary>
        public ICommand ZoomToCommand
        {
            get => (ICommand)GetValue(ZoomToCommandProperty);
            private set => SetValue(ZoomToCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the drawing used to visualize graphics for the next trace result. Setting this value will update
        /// <see cref="ResultPointSymbol"/>, <see cref="ResultLineSymbol"/>, and <see cref="ResultFillSymbol"/> if they
        /// are of type <see cref="SimpleMarkerSymbol"/>, <see cref="SimpleLineSymbol"/>, or <see cref="SimpleFillSymbol"/>, respectively.
        /// </summary>
        public System.Drawing.Color ResultVisualizationColor
        {
            get => (System.Drawing.Color)GetValue(ResultVisualizationColorProperty);
            set => SetValue(ResultVisualizationColorProperty, value);
        }

        /// <summary>
        /// Gets the command used to cancel whatever operation is in progress.
        /// </summary>
        public ICommand CancelOperationCommand
        {
            get => (ICommand)GetValue(CancelOperationCommandProperty);
            private set => SetValue(CancelOperationCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="GeoView"/> where starting points and trace results are displayed.
        /// </summary>
        /// <value>A <see cref="GeoView"/> where starting points and trace results are displayed.</value>
        public GeoView? GeoView
        {
            get => GetValue(GeoViewProperty) as GeoView;
            set => SetValue(GeoViewProperty, value);
        }

        /// <summary>
        /// Gets a <see cref="IList{StartingPointModel}"/> used as starting points for a trace.
        /// Items in this collection may be removed, but do not add directly. Instead call <see cref="AddStartingPoint(ArcGISFeature, MapPoint?)"/> to add starting points.
        /// </summary>
        /// <value>A <see cref="IList{StartingPointModel}"/> used as starting points for a trace.</value>
        public IList<StartingPointModel>? StartingPoints
        {
            get => GetValue(StartingPointsProperty) as IList<StartingPointModel>;
            private set => SetValue(StartingPointsProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to navigate to the result area once trace is complete.
        /// </summary>
        /// <value>
        /// A value indicating whether to navigate to the result area once trace is complete.
        /// </value>
        public bool AutoZoomToTraceResults
        {
            get => (bool)GetValue(AutoZoomToTraceResultsProperty);
            set => SetValue(AutoZoomToTraceResultsProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="Symbol"/> that represents a starting point.
        /// </summary>
        /// <value>
        /// A <see cref="Symbol"/> that represents a starting point.
        /// </value>
        public Symbol? StartingPointSymbol
        {
            get => GetValue(StartingPointSymbolProperty) as Symbol;
            set => SetValue(StartingPointSymbolProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="Symbol"/> that represents an aggregated multipoint trace result.
        /// </summary>
        /// <value>
        /// A <see cref="Symbol"/> that represents an aggregated multipoint trace result.
        /// </value>
        public Symbol? ResultPointSymbol
        {
            get => GetValue(ResultPointSymbolProperty) as Symbol;
            set => SetValue(ResultPointSymbolProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="Symbol"/> that represents an aggregated polyline trace result.
        /// </summary>
        /// <value>
        /// A <see cref="Symbol"/> that represents an aggregated polyline trace result.
        /// </value>
        public Symbol? ResultLineSymbol
        {
            get => GetValue(ResultLineSymbolProperty) as Symbol;
            set => SetValue(ResultLineSymbolProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="Symbol"/> that represents an aggregated polygon trace result.
        /// </summary>
        /// <value>
        /// A <see cref="Symbol"/> that represents an aggregated polygon trace result.
        /// </value>
        public Symbol? ResultFillSymbol
        {
            get => GetValue(ResultFillSymbolProperty) as Symbol;
            set => SetValue(ResultFillSymbolProperty, value);
        }

        /// <summary>
        /// Gets the command used to delete results.
        /// </summary>
        public ICommand DeleteResultCommand
        {
            get => (ICommand)GetValue(DeleteResultCommandProperty);
            private set => SetValue(DeleteResultCommandProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the control is identifying starting points.
        /// </summary>
        public bool IsIdentifyingStartingPoints
        {
            get => (bool)GetValue(IsIdentifyingStartingPointsProperty);
            private set => SetValue(IsIdentifyingStartingPointsProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected result.
        /// </summary>
        public UtilityTraceOperationResult? SelectedResult
        {
            get => (UtilityTraceOperationResult)GetValue(SelectedResultProperty);
            set => SetValue(SelectedResultProperty, value);
        }

        /// <summary>
        /// Gets or sets the label used for the trace in progress.
        /// </summary>
        public string? TraceInProgressName
        {
            get => (string)GetValue(TraceInProgressNameProperty);
            set => SetValue(TraceInProgressNameProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether there are more than the minimum required starting points for the selected trace.
        /// </summary>
        /// <remarks>
        /// Running traces with extra starting points may be valid depending on the trace.
        /// </remarks>
        public bool HasUnnecessaryStartingPointsWarning
        {
            get => (bool)GetValue(HasUnnecessaryStartingPointsWarningProperty);
            private set => SetValue(HasUnnecessaryStartingPointsWarningProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether there are not enough starting points to run the selected trace.
        /// </summary>
        public bool HasInsufficientStartingPointsWarning
        {
            get => (bool)GetValue(HasInsufficientStartingPointsWarningProperty);
            private set => SetValue(HasInsufficientStartingPointsWarningProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the current configuration duplicates the configuration used to produce a result in <see cref="TraceResults"/>.
        /// </summary>
        public bool ConfigurationDuplicatesExistingResultWarning
        {
            get => (bool)GetValue(ConfigurationDuplicatesExistingResultWarningProperty);
            private set => SetValue(ConfigurationDuplicatesExistingResultWarningProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected trace configuration.
        /// </summary>
        public UtilityNamedTraceConfiguration? SelectedTraceConfiguration
        {
            get => (UtilityNamedTraceConfiguration)GetValue(SelectedTraceConfigurationProperty);
            set => SetValue(SelectedTraceConfigurationProperty, value);
        }

        /// <summary>
        /// Gets a list of available named trace configurations for the currently selected network.
        /// </summary>
        public IList<UtilityNamedTraceConfiguration>? NamedTraceConfigurations
        {
            get => (IList<UtilityNamedTraceConfiguration>?)GetValue(NamedTraceConfigurationsProperty);
            private set => SetValue(NamedTraceConfigurationsProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the control is being used to add starting points.
        /// </summary>
        public bool IsAddingStartingPoints
        {
            get => (bool)GetValue(IsAddingStartingPointsProperty);
            private set => SetValue(IsAddingStartingPointsProperty, value);
        }

        /// <summary>
        /// Gets a command that changes the value of <see cref="IsAddingStartingPoints"/>.
        /// </summary>
        public ICommand SetIsAddingStartingPointsCommand
        {
            get => (ICommand)GetValue(SetIsAddingStartingPointsCommandProperty);
            private set => SetValue(SetIsAddingStartingPointsCommandProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether a trace is in progress.
        /// </summary>
        public bool IsTraceInProgress
        {
            get => (bool)GetValue(IsTraceInProgressProperty);
            private set => SetValue(IsTraceInProgressProperty, value);
        }

        /// <summary>
        /// Gets the command used to run the trace.
        /// </summary>
        public ICommand RunTraceCommand
        {
            get => (ICommand)GetValue(RunTraceCommandProperty);
            private set => SetValue(RunTraceCommandProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the active trace configuration is ready to run.
        /// </summary>
        public bool IsReadyToTrace
        {
            get => (bool)GetValue(IsReadyToTraceProperty);
            private set => SetValue(IsReadyToTraceProperty, value);
        }

        /// <summary>
        /// Gets the list of trace results.
        /// </summary>
        public IList<UtilityTraceOperationResult>? TraceResults
        {
            get => (IList<UtilityTraceOperationResult>?)GetValue(TraceResultsProperty);
            private set => SetValue(TraceResultsProperty, value);
        }

        /// <summary>
        /// Gets or sets the command used to zoom to results.
        /// </summary>
        public ICommand ZoomToResultCommand
        {
            get => (ICommand)GetValue(ZoomToResultCommandProperty);
            set => SetValue(ZoomToResultCommandProperty, value);
        }

        /// <summary>
        /// Gets the available utility networks. This property is populated from the Map in the associated <see cref="GeoView"/> automatically.
        /// </summary>
        public IList<UtilityNetwork>? UtilityNetworks
        {
            get => (IList<UtilityNetwork>?)GetValue(UtilityNetworksProperty);
            private set => SetValue(UtilityNetworksProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected utility network.
        /// </summary>
        public UtilityNetwork? SelectedUtilityNetwork
        {
            get => (UtilityNetwork?)GetValue(SelectedUtilityNetworkProperty);
            set => SetValue(SelectedUtilityNetworkProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected starting point. Selecting a starting point highlights it on the map.
        /// </summary>
        public StartingPointModel? SelectedStartingPoint
        {
            get => GetValue(SelectedStartingPointProperty) as StartingPointModel;
            set => SetValue(SelectedStartingPointProperty, value);
        }

        #endregion Convenience Properties

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="ResetStartingPointsCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResetStartingPointsCommandProperty =
            DependencyProperty.Register(nameof(ResetStartingPointsCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="ZoomToCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomToCommandProperty =
            DependencyProperty.Register(nameof(ZoomToCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="ResultVisualizationColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultVisualizationColorProperty =
            DependencyProperty.Register(nameof(ResultVisualizationColor), typeof(System.Drawing.Color), typeof(UtilityNetworkTraceTool), new PropertyMetadata(System.Drawing.Color.Blue, OnResultVisualizationColorChanged));

        /// <summary>
        /// Identifies the <see cref="DeleteCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="CancelOperationCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelOperationCommandProperty =
            DependencyProperty.Register(nameof(CancelOperationCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnGeoViewChanged));

        /// <summary>
        /// Identifies the <see cref="StartingPoints"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingPointsProperty =
            DependencyProperty.Register(nameof(StartingPoints), typeof(IList<StartingPointModel>), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="AutoZoomToTraceResults"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoZoomToTraceResultsProperty =
            DependencyProperty.Register(nameof(AutoZoomToTraceResults), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(true, OnAutoZoomToTraceResultsChanged));

        /// <summary>
        /// Identifies the <see cref="StartingPointSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingPointSymbolProperty =
            DependencyProperty.Register(nameof(StartingPointSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnStartingPointSymbolChanged));

        /// <summary>
        /// Identifies the <see cref="ResultPointSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultPointSymbolProperty =
            DependencyProperty.Register(nameof(ResultPointSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnResultPointSymbolChanged));

        /// <summary>
        /// Identifies the <see cref="ResultLineSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultLineSymbolProperty =
            DependencyProperty.Register(nameof(ResultLineSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnResultLineSymbolChanged));

        /// <summary>
        /// Identifies the <see cref="ResultFillSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultFillSymbolProperty =
            DependencyProperty.Register(nameof(ResultFillSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnResultFillSymbolChanged));

        /// <summary>
        /// Identifies the <see cref="UtilityNetworks"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UtilityNetworksProperty =
            DependencyProperty.Register(nameof(UtilityNetworks), typeof(IList<UtilityNetwork>), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="SelectedUtilityNetwork"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedUtilityNetworkProperty =
            DependencyProperty.Register(nameof(SelectedUtilityNetwork), typeof(UtilityNetwork), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnSelectedUtilityNetworkChanged));

        /// <summary>
        /// Identifies the <see cref="SelectedTraceConfiguration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedTraceConfigurationProperty =
            DependencyProperty.Register(nameof(SelectedTraceConfiguration), typeof(UtilityNamedTraceConfiguration), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnSelectedTraceConfigurationChanged));

        /// <summary>
        /// Identifies the <see cref="NamedTraceConfigurations"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NamedTraceConfigurationsProperty =
            DependencyProperty.Register(nameof(NamedTraceConfigurations), typeof(IList<UtilityNamedTraceConfiguration>), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="IsAddingStartingPoints"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAddingStartingPointsProperty =
            DependencyProperty.Register(nameof(IsAddingStartingPoints), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(false, OnIsAddingStartingPointsChanged));

        /// <summary>
        /// Identifies the <see cref="SetIsAddingStartingPointsCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SetIsAddingStartingPointsCommandProperty =
            DependencyProperty.Register(nameof(SetIsAddingStartingPointsCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="IsTraceInProgress"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTraceInProgressProperty =
            DependencyProperty.Register(nameof(IsTraceInProgress), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="RunTraceCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RunTraceCommandProperty =
            DependencyProperty.Register(nameof(RunTraceCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="IsReadyToTrace"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadyToTraceProperty =
            DependencyProperty.Register(nameof(IsReadyToTrace), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="TraceResults"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceResultsProperty =
            DependencyProperty.Register(nameof(TraceResults), typeof(IList<UtilityTraceOperationResult>), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="ZoomToResultCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomToResultCommandProperty =
            DependencyProperty.Register(nameof(ZoomToResultCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="DeleteResultCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DeleteResultCommandProperty =
            DependencyProperty.Register(nameof(DeleteResultCommand), typeof(ICommand), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="IsIdentifyingStartingPoints"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsIdentifyingStartingPointsProperty =
            DependencyProperty.Register(nameof(IsIdentifyingStartingPoints), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="SelectedResult"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedResultProperty =
            DependencyProperty.Register(nameof(SelectedResult), typeof(UtilityTraceOperationResult), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Identifies the <see cref="TraceInProgressName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceInProgressNameProperty =
            DependencyProperty.Register(nameof(TraceInProgressName), typeof(string), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnTraceInProgressNameChanged));

        /// <summary>
        /// Identifies the <see cref="HasUnnecessaryStartingPointsWarning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasUnnecessaryStartingPointsWarningProperty =
            DependencyProperty.Register(nameof(HasUnnecessaryStartingPointsWarning), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="HasInsufficientStartingPointsWarning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasInsufficientStartingPointsWarningProperty =
            DependencyProperty.Register(nameof(HasInsufficientStartingPointsWarning), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="ConfigurationDuplicatesExistingResultWarning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ConfigurationDuplicatesExistingResultWarningProperty =
            DependencyProperty.Register(nameof(ConfigurationDuplicatesExistingResultWarning), typeof(bool), typeof(UtilityNetworkTraceTool), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="SelectedStartingPoint"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedStartingPointProperty =
            DependencyProperty.Register(nameof(SelectedStartingPoint), typeof(StartingPointModel), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnSelectedStartingPointChanged));

        #endregion Dependency Properties

        #region Events

        /// <summary>
        /// Event raised when a new utility network is selected.
        /// </summary>
        public event EventHandler<UtilityNetworkChangedEventArgs>? UtilityNetworkChanged;

        /// <summary>
        /// Event raised when a utility network trace is completed.
        /// </summary>
        public event EventHandler<UtilityNetworkTraceCompletedEventArgs>? UtilityNetworkTraceCompleted;

        #endregion Events

        /// <summary>
        /// Finalizes an instance of the <see cref="UtilityNetworkTraceTool"/> class.
        /// </summary>
        ~UtilityNetworkTraceTool()
        {
            if (_controller is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
#endif
