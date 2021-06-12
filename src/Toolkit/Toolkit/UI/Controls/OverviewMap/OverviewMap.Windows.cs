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

#if !XAMARIN
using System;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OverviewMap
{
    /// <summary>
    /// Defines a small "overview" (or "inset") map displaying the current extent of the attached <see cref="GeoView"/>.
    /// </summary>
    public class OverviewMap : MapView
    {
        private GeoView _geoview;
        private GraphicsOverlay _extentOverlay;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewMap"/> class.
        /// </summary>
        public OverviewMap()
        {
            IsAttributionTextVisible = false;
            Map = new Map(BasemapStyle.ArcGISTopographic);
            _extentOverlay = new GraphicsOverlay();
            _extentOverlay.Renderer = new SimpleRenderer(ExtentSymbol);
            _extentOverlay.Renderer.RotationExpression = "[Heading]";
            _extentOverlay.RenderingMode = GraphicsRenderingMode.Dynamic;
            GraphicsOverlays.Add(_extentOverlay);

            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

            ViewpointChanged += OnViewpointChanged;
            NavigationCompleted += OnNavigationCompleted;
        }

        /// <summary>
        /// Gets or sets the amount to scale the overview map's viewpoint compared to the attached <see cref="GeoView"/>.
        /// </summary>
        /// <remarks>
        /// The default is 25.
        /// </remarks>
        public double ScaleFactor
        {
            get { return (double)GetValue(ScaleFactorProperty); }
            set { SetValue(ScaleFactorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the symbol used to draw the <see cref="GeoView"/>'s extent.
        /// </summary>
        /// <remarks>
        /// The default is an empty fill symbol with a 1 point red outline.
        /// </remarks>
        public FillSymbol ExtentSymbol
        {
            get { return (FillSymbol)GetValue(ExtentSymbolProperty); }
            set { SetValue(ExtentSymbolProperty, value); }
        }

        /// <summary>
        /// Gets or sets the geoview whose extent is to be displayed.
        /// </summary>
        /// <remarks>
        /// Note that by default interaction with <see cref="OverviewMap"/> will navigate the attached GeoView.
        /// </remarks>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ScaleFactor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor), typeof(double), typeof(OverviewMap), new PropertyMetadata(25.0));

        /// <summary>
        /// Identifies the <see cref="ExtentSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtentSymbolProperty =
            DependencyProperty.Register(nameof(ExtentSymbol), typeof(FillSymbol), typeof(OverviewMap), new PropertyMetadata(
                new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent,
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 1)), OnExtentSymbolPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(OverviewMap), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((OverviewMap)d).SetGeoView(e.NewValue as GeoView);

        private static void OnExtentSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((OverviewMap)d).UpdateGraphic();

        private void SetGeoView(GeoView newView)
        {
            if (newView == _geoview)
            {
                return;
            }

            if (_geoview != null)
            {
                _geoview.ViewpointChanged -= OnViewpointChanged;
                _geoview.NavigationCompleted -= OnNavigationCompleted;
            }

            _geoview = newView;
            if (_geoview != null)
            {
                // TODO = make weak event handler
                _geoview.ViewpointChanged += OnViewpointChanged;
                _geoview.NavigationCompleted += OnNavigationCompleted;
                OnViewpointChanged(_geoview, null);
            }
        }

        private void UpdateGraphic()
        {
            _extentOverlay.Graphics.Clear();
            if (_geoview?.GetCurrentViewpoint(ViewpointType.BoundingGeometry) is Viewpoint extent)
            {
                var projectedExtent = GeometryEngine.Project(extent.TargetGeometry, SpatialReference);
                var graphic = new Graphic(projectedExtent);
                graphic.Attributes["Heading"] = extent.Rotation;
                _extentOverlay.Graphics.Add(graphic);
            }
        }

        private void OnViewpointChanged(object sender, EventArgs e)
        {
            GeoView sendingView = (GeoView)sender;
            GeoView receivingView = sendingView == this ? _geoview : this;

            if (receivingView == null || sendingView == null)
            {
                return;
            }

            // Check for IsNavigating is intended to ignore OnViewpointChanged calls triggered by the call to SetViewpoint.
            if (sendingView.IsNavigating)
            {
                ApplyViewpoint(sendingView, receivingView);
                var existingViewpoint = sendingView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
                var scaleMultiplier = sendingView == this ? 1.0 / ScaleFactor : ScaleFactor;
                var newViewpoint = new Viewpoint((MapPoint)existingViewpoint.TargetGeometry, existingViewpoint.TargetScale * scaleMultiplier);
                receivingView.SetViewpoint(newViewpoint);
                UpdateGraphic();
            }
        }

        private void OnNavigationCompleted(object sender, EventArgs e)
        {
            GeoView sendingView = (GeoView)sender;
            GeoView receivingView = sendingView == this ? _geoview : this;
            if (receivingView == null || sendingView == null)
            {
                return;
            }

            ApplyViewpoint(sendingView, receivingView);
        }

        private void ApplyViewpoint(GeoView sendingView, GeoView receivingView)
        {
            var existingViewpoint = sendingView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            var scaleMultiplier = sendingView == this ? 1.0 / ScaleFactor : ScaleFactor;
            Viewpoint newViewpoint = new Viewpoint((MapPoint)existingViewpoint.TargetGeometry, existingViewpoint.TargetScale * scaleMultiplier);
            receivingView.SetViewpoint(newViewpoint);
            UpdateGraphic();
        }
    }
}

#endif
