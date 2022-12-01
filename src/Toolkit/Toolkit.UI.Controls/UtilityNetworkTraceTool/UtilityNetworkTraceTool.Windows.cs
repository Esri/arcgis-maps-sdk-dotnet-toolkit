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

#if WPF || WINDOWS_XAML
using System.Collections.Specialized;
using System.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if NETFX_CORE
using ToggleButton = Windows.UI.Xaml.Controls.ToggleSwitch;
#elif WINUI
using ToggleButton = Microsoft.UI.Xaml.Controls.ToggleSwitch;
#elif WPF
using System.Windows.Controls.Primitives;
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
            _controller.Results.CollectionChanged += Results_CollectionChanged;
            _controller.TraceTypes.CollectionChanged += TraceTypes_CollectionChanged;
            _controller.StartingPoints.CollectionChanged += StartingPoints_CollectionChanged;
            _controller.UtilityNetworks.CollectionChanged += UtilityNetworks_CollectionChanged;
            ResultFillSymbol = _controller.ResultFillSymbol;
            ResultLineSymbol = _controller.ResultLineSymbol;
            ResultPointSymbol = _controller.ResultPointSymbol;
        }

        private void UtilityNetworks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _part_utilityNetworkSelectorContainer?.SetValue(VisibilityProperty, _controller.UtilityNetworks.Count > 1 ? Visibility.Visible : Visibility.Collapsed);
        }

        private void StartingPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _part_resetStartingPointsButton?.SetValue(VisibilityProperty, _controller.StartingPoints.Any() ? Visibility.Visible : Visibility.Collapsed);
        }

        private void TraceTypes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _part_noNamedTraceConfigurationsWarningContainer?.SetValue(VisibilityProperty, _controller.TraceTypes.Any() ? Visibility.Collapsed : Visibility.Visible);
            _part_startingPointsSectionContainer?.SetValue(VisibilityProperty, _controller.TraceTypes.Any() ? Visibility.Visible : Visibility.Collapsed);
            _part_traceConfigurationsContainer?.SetValue(VisibilityProperty, _controller.TraceTypes.Any() ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// Adds a starting point.
        /// </summary>
        /// <param name="feature">The feature to use as the basis for the starting point.</param>
        /// <param name="location">Optional location to use, which will be used to specify the location along the line for line features.</param>
        public void AddStartingPoint(ArcGISFeature feature, MapPoint? location)
        {
            _controller.AddStartingPoint(feature, location);
        }

        #region Command implementations

        private void HandleZoomToStartingPointCommand(object? parameter)
        {
            if (parameter is StartingPointModel selectedStartingPoint && selectedStartingPoint.ZoomToExtent is Envelope zoomEnvelope)
            {
                GeoView?.SetViewpoint(new Viewpoint(zoomEnvelope));
            }
            else if (_part_startingPointsList?.SelectedItem is StartingPointModel selectedSP && selectedSP.ZoomToExtent is Envelope selectedZoomEnvelope)
            {
                GeoView?.SetViewpoint(new Viewpoint(selectedZoomEnvelope));
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
#if !WINDOWS_XAML
                _tabControl?.SetValue(TabControl.SelectedIndexProperty, 0);
#else
                _pivotControl?.SetValue(Pivot.SelectedIndexProperty, 0);
#endif

                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<UtilityTraceOperationResult>())
                    {
                        item.AreFeaturesSelected = false;
                    }
                }
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
            }

            _part_resultsTabItem?.SetValue(VisibilityProperty, _controller.Results.Any() ? Visibility.Visible : Visibility.Collapsed);
#if !WINDOWS_XAML
            _tabControl?.SetValue(TabControl.SelectedIndexProperty, _controller.Results.Any() ? 1 : 0);
#else
            _pivotControl?.SetValue(Pivot.SelectedIndexProperty, _controller.Results.Any() ? 1 : 0);
#endif
            #if WPF
            // Fix an issue on WPF where the results tab control would show multiple tabs for the first item in the results when bound to observable collection.
            if (_part_resultsItemControl != null)
            {
                _part_resultsItemControl.ItemsSource = _controller.Results.ToList();
                if (_part_resultsItemControl is Selector resultSelector && !_controller.Results.Contains(resultSelector.SelectedItem))
                {
                    resultSelector.SelectedIndex = 0;
                }
            }
            #endif

            if (e.NewItems != null && e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count == 1 && _part_resultsItemControl is Selector resultSelectionControl)
            {
                resultSelectionControl.SelectedItem = e.NewItems[0];
            }
        }

        private void Controller_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_controller.EnableTrace):
                    _part_runTraceButton?.SetValue(IsEnabledProperty, _controller.EnableTrace);
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
                    _part_utilityNetworkSelector?.SetValue(ListView.SelectedItemProperty, _controller.SelectedUtilityNetwork);
                    UtilityNetworkChanged?.Invoke(this, new UtilityNetworkChangedEventArgs(_controller.SelectedUtilityNetwork));
                    break;
                case nameof(_controller.SelectedTraceType):
                    _part_traceConfigurationsSelector?.SetValue(Selector.SelectedItemProperty, _controller.SelectedTraceType);
                    break;
                case nameof(_controller.SelectedStartingPoint):
                    _part_startingPointsList?.SetValue(ListView.SelectedItemProperty, _controller.SelectedStartingPoint);
                    break;
                case nameof(_controller.TraceName):
                    _part_resultNamedTextBox?.SetValue(TextBox.TextProperty, _controller.TraceName ?? "");
                    break;
                case nameof(_controller.IsRunningTrace):
                    _part_traceInProgressIndicator?.SetValue(VisibilityProperty, _controller.IsRunningTrace ? Visibility.Visible : Visibility.Collapsed);
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
                    _part_addRemoveStartingPointsButtonContainer?.SetValue(VisibilityProperty, _controller.IsAddingStartingPoints ? Visibility.Collapsed : Visibility.Visible);
                    _part_cancelAddStartingPointsButton?.SetValue(VisibilityProperty, _controller.IsAddingStartingPoints ? Visibility.Visible : Visibility.Collapsed);
                    break;
                case nameof(_controller.StartingPointSymbol):
                    StartingPointSymbol = _controller.StartingPointSymbol;
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
                    _part_duplicateTraceWarningContainer?.SetValue(VisibilityProperty, _controller.DuplicatedTraceWarning ? Visibility.Visible : Visibility.Collapsed);
                    break;
                case nameof(_controller.InsufficientStartingPointsWarning):
                    _part_needMoreStartingPointsWarningContainer?.SetValue(VisibilityProperty, _controller.InsufficientStartingPointsWarning ? Visibility.Visible : Visibility.Collapsed);
                    break;
                case nameof(_controller.TooManyStartingPointsWarning):
                    _part_extraStartingPointsWarningContainer?.SetValue(VisibilityProperty, _controller.TooManyStartingPointsWarning ? Visibility.Visible : Visibility.Collapsed);
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
                _part_identifyInProgressIndicator?.SetValue(VisibilityProperty, Visibility.Visible);

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
                _part_identifyInProgressIndicator?.SetValue(VisibilityProperty, Visibility.Collapsed);
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

        private static void OnGeoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool.HandleGeoViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
            }
        }

#if WINDOWS_XAML
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
#if WINDOWS_XAML
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

#if WINDOWS_XAML
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
        /// Gets or sets the <see cref="GeoView"/> where starting points and trace results are displayed.
        /// </summary>
        /// <value>A <see cref="GeoView"/> where starting points and trace results are displayed.</value>
        public GeoView? GeoView
        {
            get => GetValue(GeoViewProperty) as GeoView;
            set => SetValue(GeoViewProperty, value);
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

        #endregion Convenience Properties

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnGeoViewChanged));

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
    }
}
#endif
