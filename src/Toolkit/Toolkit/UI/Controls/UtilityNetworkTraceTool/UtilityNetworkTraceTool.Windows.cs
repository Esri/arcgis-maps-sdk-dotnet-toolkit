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
    public partial class UtilityNetworkTraceTool : Control, IUtilityNetworkTraceTool
    {
        private readonly UtilityNetworkTraceToolController _controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityNetworkTraceTool"/> class.
        /// </summary>
        public UtilityNetworkTraceTool()
        {
            DefaultStyleKey = typeof(UtilityNetworkTraceTool);
            _controller = new UtilityNetworkTraceToolController(this);
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
                traceTool._controller.HandleGeoViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
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
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(true));

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

        #region Controller Callbacks

        void IUtilityNetworkTraceTool.SelectUtilityNetwork(UtilityNetwork? utilityNetwork)
        {
            if (_utilityNetworksPicker != null)
            {
                _utilityNetworksPicker.SelectedItem = utilityNetwork;
            }
        }

        void IUtilityNetworkTraceTool.SelectTraceType(UtilityNamedTraceConfiguration? traceType)
        {
            if (_traceTypesPicker != null)
            {
                _traceTypesPicker.SelectedItem = traceType;
            }
        }

        void IUtilityNetworkTraceTool.SelectStartingPoint(StartingPointModel? startingPoint)
        {
            if (_startingPointsList != null)
            {
                _startingPointsList.SelectedItem = startingPoint;
            }
        }

        void IUtilityNetworkTraceTool.UpdateUtilityNetworksVisibility(bool isVisible)
        {
            if (_utilityNetworksPicker != null)
            {
                _utilityNetworksPicker.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void IUtilityNetworkTraceTool.UpdateTraceTypesVisibility(bool isVisible)
        {
            if (_traceTypesPicker != null)
            {
                _traceTypesPicker.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void IUtilityNetworkTraceTool.UpdateStartingPointsVisibility(bool isVisible)
        {
            if (_startingPointsList != null)
            {
                _startingPointsList.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void IUtilityNetworkTraceTool.UpdateFunctionResultsVisibility(bool isVisible)
        {
            if (_functionResultsList != null)
            {
                _functionResultsList.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void IUtilityNetworkTraceTool.EnableTrace(bool isTraceEnabled)
        {
            if (_traceButton != null)
            {
                _traceButton.IsEnabled = isTraceEnabled;
            }
        }

        void IUtilityNetworkTraceTool.SetIsBusy(bool isBusy)
        {
            if (_busyIndicator != null)
            {
                _busyIndicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
                _busyIndicator.IsIndeterminate = isBusy;
            }
        }

        void IUtilityNetworkTraceTool.SetStatus(string status)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = status;
            }
        }

        void IUtilityNetworkTraceTool.NotifyUtilityNetworkChanged(UtilityNetwork utilityNetwork)
        {
            if (UtilityNetworkChanged != null)
            {
                UtilityNetworkChanged?.Invoke(this, new UtilityNetworkChangedEventArgs(utilityNetwork));
            }
        }

        void IUtilityNetworkTraceTool.NotifyUtilityNetworkTraceCompleted(UtilityTraceParameters parameters,
            IEnumerable<UtilityTraceResult>? results, Exception? error)
        {
            if (UtilityNetworkTraceCompleted != null)
            {
                if (results != null)
                {
                    UtilityNetworkTraceCompleted.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(parameters, results));
                }

                if (error != null)
                {
                    UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(parameters, error));
                }
            }
        }

        #endregion Controller Callbacks
    }
}
#endif
