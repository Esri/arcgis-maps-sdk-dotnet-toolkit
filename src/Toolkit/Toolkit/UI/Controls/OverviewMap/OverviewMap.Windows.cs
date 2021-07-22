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
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI.Controls;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Defines a small "overview" (or "inset") map displaying a representation of the attached <see cref="GeoView"/>'s current viewpoint.
    /// </summary>
    [TemplatePart(Name = "PART_MapView", Type = typeof(MapView))]
    public class OverviewMap : Control
    {
        private OverviewMapController? _controller;
        private MapView? _overviewMapView;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewMap"/> class.
        /// </summary>
        public OverviewMap()
        {
            DefaultStyleKey = typeof(OverviewMap);
        }

        /// <inheritdoc/>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("PART_MapView") is MapView templateMapView)
            {
                _overviewMapView = templateMapView;
                _overviewMapView.IsAttributionTextVisible = false;
                _overviewMapView.Map = new Map(BasemapStyle.ArcGISTopographic);

                _controller = new OverviewMapController(_overviewMapView)
                {
                    PointSymbol = PointSymbol,
                    AreaSymbol = AreaSymbol,
                    ScaleFactor = ScaleFactor,
                    AttachedView = GeoView,
                };
            }
        }

        /// <summary>
        /// Gets or sets the symbol used to draw the <see cref="GeoView"/>'s visible area.
        /// </summary>
        /// <remarks>
        /// The default is an empty fill symbol with a 1 point red outline.
        /// </remarks>
        public Symbol? AreaSymbol
        {
            get => GetValue(AreaSymbolProperty) as Symbol;
            set => SetValue(AreaSymbolProperty, value);
        }

        /// <summary>
        /// Gets or sets the geoview whose extent is to be displayed.
        /// </summary>
        /// <remarks>
        /// Note that by default interaction with <see cref="OverviewMap"/> will navigate the attached GeoView.
        /// </remarks>
        public GeoView? GeoView
        {
            get => GetValue(GeoViewProperty) as GeoView;
            set => SetValue(GeoViewProperty, value);
        }

        /// <summary>
        /// Gets or sets the Map shown in the inset/overview map.
        /// </summary>
        /// <remarks>
        /// Defaults to a map with a basemap in style <see cref="BasemapStyle.ArcGISTopographic"/>.
        /// </remarks>
        public Map? Map
        {
            get => GetValue(MapProperty) as Map;
            set => SetValue(MapProperty, value);
        }

        /// <summary>
        /// Gets or sets the symbol used to draw the <see cref="GeoView"/>'s viewpoint when it isn't possible to show the visible area (for example, when showing a scene).
        /// </summary>
        /// <remarks>
        /// The default is a red cross.
        /// </remarks>
        public Symbol? PointSymbol
        {
            get => GetValue(PointSymbolProperty) as Symbol;
            set => SetValue(PointSymbolProperty, value);
        }

        /// <summary>
        /// Gets or sets the amount to scale the overview map's viewpoint compared to the attached <see cref="GeoView"/>.
        /// </summary>
        /// <remarks>
        /// The default is 25.
        /// </remarks>
        public double ScaleFactor
        {
            get => (double)GetValue(ScaleFactorProperty);
            set => SetValue(ScaleFactorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AreaSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreaSymbolProperty =
            DependencyProperty.Register(nameof(AreaSymbol), typeof(Symbol), typeof(OverviewMap), new PropertyMetadata(
                new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent,
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 0)),
                OnAreaSymbolPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(OverviewMap), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Map"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MapProperty =
            DependencyProperty.Register(nameof(Map), typeof(Map), typeof(OverviewMap), new PropertyMetadata(null, OnMapPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="PointSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PointSymbolProperty =
            DependencyProperty.Register(nameof(PointSymbol), typeof(Symbol), typeof(OverviewMap), new PropertyMetadata(
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Red, 16), OnPointSymbolPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ScaleFactor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor), typeof(double), typeof(OverviewMap), new PropertyMetadata(25.0, OnScaleFactorPropertyChanged));

        private static void OnAreaSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((OverviewMap)d)._controller is OverviewMapController controller)
            {
                controller.AreaSymbol = e.NewValue as Symbol;
            }
        }

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((OverviewMap)d)._controller is OverviewMapController controller)
            {
                controller.AttachedView = e.NewValue as GeoView;
            }
        }

        private static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((OverviewMap)d)._overviewMapView is MapView mv)
            {
                mv.Map = e.NewValue as Map;
            }
        }

        private static void OnPointSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((OverviewMap)d)._controller is OverviewMapController controller)
            {
                controller.PointSymbol = e.NewValue as Symbol;
            }
        }

        private static void OnScaleFactorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((OverviewMap)d)._controller is OverviewMapController controller && e.NewValue is double newScale)
            {
                controller.ScaleFactor = newScale;
            }
        }
    }
}

#endif
