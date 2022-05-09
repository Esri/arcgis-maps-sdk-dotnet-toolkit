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
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
using System.Threading.Tasks;
using System.Threading;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ToggleButton = Windows.UI.Xaml.Controls.ToggleSwitch;
#else
using System.Windows;
using System.Windows.Controls;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityNetworkTraceTool"/> class.
        /// </summary>
        public UtilityNetworkTraceTool()
        {
            DefaultStyleKey = typeof(UtilityNetworkTraceTool);
            _controller = new UtilityNetworkTraceToolController();
            _controller.PropertyChanged += _controller_PropertyChanged;
        }

        private void _controller_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
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
                case nameof(_controller.Status):
                    if (_statusLabel != null)
                    {
                        _statusLabel.Text = _controller.Status;
                    }
                    break;
                case nameof(_controller.UtilityNetworks):
                    if (_utilityNetworksPicker != null)
                    {
                        _utilityNetworksPicker.ItemsSource = _controller.UtilityNetworks;
                    }
                    break;
                case nameof(_controller.SelectedUtilityNetwork):
                    if (UtilityNetworkChanged != null)
                    {
                        UtilityNetworkChanged?.Invoke(this, new UtilityNetworkChangedEventArgs(_controller.SelectedUtilityNetwork));
                    }
                    if (_utilityNetworksPicker != null)
                    {
                        _utilityNetworksPicker.SelectedItem = _controller.SelectedUtilityNetwork;
                    }
                    break;
                case nameof(_controller.SelectedTraceType):
                    if (_traceTypesPicker != null)
                    {
                        _traceTypesPicker.SelectedItem = _controller.SelectedTraceType;
                    }
                    break;
                case nameof(_controller.SelectedStartingPoint):
                    if (_startingPointsList != null)
                    {
                        _startingPointsList.SelectedItem = _controller.SelectedStartingPoint;
                    }
                    break;
                case nameof(_controller.CurrentResult):
                    if (_controller?.CurrentResult?.Results != null)
                    {
                        UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(_controller.CurrentResult.Paremters, _controller.CurrentResult.Results));
                    }

                    if (_controller?.CurrentResult?.Error != null)
                    {
                        UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(_controller.CurrentResult.Paremters, _controller.CurrentResult.Error));
                    }

                    if (_controller?.CurrentResult == null)
                    {
                        if (GeoView is MapView mapView && mapView.Map is Map map)
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
                    break;
                case nameof(_controller.StartingPoints):
                    break;
                //case nameof(_controller.ControllerStartingPoints):
                //    if (_startingPointsList != null)
                //    {
                //        _startingPointsList.ItemsSource = _controller.ControllerStartingPoints;
                //    }
                //    break;
                case nameof(_controller.EnableTrace):
                    if (_traceButton != null)
                    {
                        _traceButton.IsEnabled = _controller.EnableTrace;
                    }
                    break;
                case nameof(_controller.FunctionResults):
                    if (_functionResultsList != null)
                    {
                        _functionResultsList.ItemsSource = _controller.FunctionResults;
                    }
                    break;
                case nameof(_controller.IsBusy):
                    if (_busyIndicator != null)
                    {
                        _busyIndicator.Visibility = _controller.IsBusy ? Visibility.Visible : Visibility.Collapsed;
                        _busyIndicator.IsIndeterminate = _controller.IsBusy;
                    }
                    break;
                case nameof(_controller.IsAddingStartingPoints):
                    break;
                case nameof(_controller.TraceTypes):
                    if (_traceTypesPicker != null)
                    {
                        _traceTypesPicker.ItemsSource = _controller.TraceTypes;
                    }
                    break;
                case nameof(_controller.StartingPointSymbol):
                    break;
                case nameof(_controller.ResultPointSymbol):
                    break;
                case nameof(_controller.ResultLineSymbol):
                    break;
                case nameof(_controller.ResultFillSymbol):
                    break;
                case nameof(_controller.ShowUtilityNetworks):
                    if (_utilityNetworksPicker != null)
                    {
                        _utilityNetworksPicker.Visibility = _controller.ShowUtilityNetworks ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
                case nameof(_controller.ShowTraceTypes):
                    if (_traceTypesPicker != null)
                    {
                        _traceTypesPicker.Visibility = _controller.ShowTraceTypes ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
                case nameof(_controller.ShowStartingPoints):
                    if (_startingPointsList != null)
                    {
                        _startingPointsList.Visibility = _controller.ShowStartingPoints ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
                case nameof(_controller.ShowFunctionResults):
                    if (_functionResultsList != null)
                    {
                        _functionResultsList.Visibility = _controller.ShowFunctionResults ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;

#endif
        internal void HandleGeoViewChanged(GeoView? oldGeoView, GeoView? newGeoView)
        {
            _controller.Reset(true);

            _controller.IsBusy = true;
            _controller.Status = "Loading utility networks...";

            var graphicsOverlays = new[] { _controller._startingPointGraphicsOverlay, _controller._resultGraphicsOverlay };

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

        private async void OnGeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            if (e.Handled || !_controller.IsAddingStartingPoints || _controller.SelectedUtilityNetwork == null)
            {
                return;
            }

            try
            {
                _controller.Status = "Identifying a starting point...";
                _controller.IsBusy = true;

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
            catch (Exception ex)
            {
                _controller.Status = $"Identifying a starting point failed ({ex.GetType().Name}): {ex.Message}";
            }
            finally
            {
                _controller.Status = _controller.GetStatus();
                _controller.IsBusy = false;
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
        private void OnGeoModelPropertyChanged(object? sender, object? e)
        {
            if (GeoView is MapView mv && mv.Map is Map newMap)
            {
                _controller.ApplyNewMap(newMap);
            }
            else
            {
                _controller.Reset();
            }
        }

        private void OnGeoViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeoModel))
            {
                OnGeoModelPropertyChanged(sender, e);
            }
        }


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
        /// Gets or sets a <see cref="IList{ArcGISFeature}"/> used as starting points for a trace.
        /// </summary>
        /// <value>A <see cref="IList{ArcGISFeature}"/> used as starting points for a trace.</value>
        public IList<ArcGISFeature>? StartingPoints
        {
            get => GetValue(StartingPointsProperty) as IList<ArcGISFeature>;
            set => SetValue(StartingPointsProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this control is adding starting points
        /// using <see cref="GeoView.GeoViewTapped"/> event.
        /// </summary>
        /// <value>
        /// A value indicating whether this control is adding starting points using
        /// <see cref="GeoView.GeoViewTapped"/> event.
        /// </value>
        public bool IsAddingStartingPoints
        {
            get => (bool)GetValue(IsAddingStartingPointsProperty);
            set => SetValue(IsAddingStartingPointsProperty, value);
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
            get => GetValue(StartingPointSymbolProperty) as Symbol;
            set => SetValue(StartingPointSymbolProperty, value);
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
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnGeoViewChanged));

        private static void OnGeoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool.HandleGeoViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
            }
        }

        /// <summary>
        /// Identifies the <see cref="StartingPoints"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingPointsProperty =
            DependencyProperty.Register(nameof(StartingPoints), typeof(IList<ArcGISFeature>),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnStartingPointsChanged));

        private static void OnStartingPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.HandleStartingPointsChanged();
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsAddingStartingPoints"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAddingStartingPointsProperty =
            DependencyProperty.Register(nameof(IsAddingStartingPoints), typeof(bool),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(true, OnIsAddingStartingPointChanged));

        private static void OnIsAddingStartingPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.IsAddingStartingPoints = (bool)e.NewValue;
            }
        }

        /// <summary>
        /// Identifies the <see cref="AutoZoomToTraceResults"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoZoomToTraceResultsProperty =
            DependencyProperty.Register(nameof(AutoZoomToTraceResults), typeof(bool),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="StartingPointSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingPointSymbolProperty =
            DependencyProperty.Register(nameof(StartingPointSymbol), typeof(Symbol),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnStartingPointSymbolChanged));

        private static void OnStartingPointSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.HandleStartingPointSymbolChanged();
            }
        }

        /// <summary>
        /// Identifies the <see cref="ResultPointSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultPointSymbolProperty =
            DependencyProperty.Register(nameof(ResultPointSymbol), typeof(Symbol),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnResultPointSymbolChanged));

        private static void OnResultPointSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.HandleResultSymbolChanged(Geometry.GeometryType.Multipoint);
            }
        }

        /// <summary>
        /// Identifies the <see cref="ResultLineSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultLineSymbolProperty =
            DependencyProperty.Register(nameof(ResultLineSymbol), typeof(Symbol),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnResultLineSymbolChanged));

        private static void OnResultLineSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.HandleResultSymbolChanged(Geometry.GeometryType.Polyline);
            }
        }

        /// <summary>
        /// Identifies the <see cref="ResultFillSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultFillSymbolProperty =
            DependencyProperty.Register(nameof(ResultFillSymbol), typeof(Symbol),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null, OnResultFillSymbolChanged));

        private static void OnResultFillSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityNetworkTraceTool traceTool)
            {
                traceTool._controller.HandleResultSymbolChanged(Geometry.GeometryType.Polygon);
            }
        }

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
