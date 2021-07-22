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

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// Defines a small "overview" (or "inset") map displaying a representation of the attached <see cref="GeoView"/>'s current viewpoint.
    /// </summary>
    public class OverviewMap : TemplatedView
    {
        private OverviewMapController? _controller;
        private static readonly ControlTemplate DefaultControlTemplate;

        private MapView? _overviewMapView;

        static OverviewMap()
        {
            string template = @"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms""
                                                 xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
                                                 xmlns:esri=""clr-namespace:Esri.ArcGISRuntime.Xamarin.Forms;assembly=Esri.ArcGISRuntime.Xamarin.Forms""
                                                 xmlns:internal=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;assembly=Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
                                    <Grid>
                                        <Grid.Resources>
                                            <internal:LoadStatusToVisibilityConverter x:Key=""LoadStatusToVisibilityConverter"" />
                                        </Grid.Resources>
                                        <ActivityIndicator IsRunning=""{Binding Source={x:Reference PART_MapView}, Path=Map.LoadStatus, Converter={StaticResource LoadStatusToVisibilityConverter}, ConverterParameter='Loading'}"" />
                                        <Label Text=""Map failed to load. Did you forget an API key?"" IsVisible=""{Binding Source={x:Reference PART_MapView}, Path=Map.LoadStatus, Converter={StaticResource LoadStatusToVisibilityConverter}, ConverterParameter='FailedToLoad'}""  />
                                        <esri:MapView x:Name=""PART_MapView"" IsVisible=""{Binding Source={x:Reference PART_MapView}, Path=Map.LoadStatus, Converter={StaticResource LoadStatusToVisibilityConverter}, ConverterParameter='Loaded'}"" />
                                    </Grid>
                                </ControlTemplate>";
            DefaultControlTemplate = Extensions.LoadFromXaml(new ControlTemplate(), template);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewMap"/> class.
        /// </summary>
        public OverviewMap()
        {
            ControlTemplate = DefaultControlTemplate;
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _overviewMapView = GetTemplateChild("PART_MapView") as MapView;

            if (_overviewMapView != null)
            {
                _controller = new OverviewMapController(_overviewMapView)
                {
                    ScaleFactor = ScaleFactor,
                    PointSymbol = PointSymbol,
                    AreaSymbol = AreaSymbol,
                    AttachedView = GeoView,
                };
                _overviewMapView.IsAttributionTextVisible = false;

                if (Map == null)
                {
                    Map = new Map(BasemapStyle.ArcGISTopographic);
                }
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
        /// Identifies the <see cref="AreaSymbol"/> bindable property.
        /// </summary>
        public static readonly BindableProperty AreaSymbolProperty =
            BindableProperty.Create(nameof(AreaSymbol), typeof(Symbol), typeof(OverviewMap), new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 1)), propertyChanged: OnAreaSymbolChanged);

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(OverviewMap), null, BindingMode.OneWay, null, propertyChanged: OnGeoViewPropertyChanged);

        /// <summary>
        /// Identifies the <see cref="Map"/> bindable property.
        /// </summary>
        public static readonly BindableProperty MapProperty =
            BindableProperty.Create(nameof(Map), typeof(Map), typeof(OverviewMap), null, propertyChanged: OnMapPropertyChanged);

        /// <summary>
        /// Identifies the <see cref="PointSymbol"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PointSymbolProperty =
            BindableProperty.Create(nameof(PointSymbol), typeof(Symbol), typeof(OverviewMap), new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Red, 16), propertyChanged: OnPointSymbolChanged);

        /// <summary>
        /// Identifies the <see cref="ScaleFactor"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ScaleFactorProperty =
            BindableProperty.Create(nameof(ScaleFactor), typeof(double), typeof(OverviewMap), 25.0, propertyChanged: OnScaleFactorPropertyChanged);

        private static void OnAreaSymbolChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (((OverviewMap)sender)._controller is OverviewMapController controller)
            {
                controller.AreaSymbol = newValue as Symbol;
            }
        }

        private static void OnGeoViewPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (((OverviewMap)sender)._controller is OverviewMapController controller)
            {
                controller.AttachedView = newValue as GeoView;
            }
        }

        private static void OnMapPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            if ((OverviewMap)sender is OverviewMap sendingView && sendingView._overviewMapView is MapView overview)
            {
                overview.Map = (Map)newValue;
            }
        }

        private static void OnPointSymbolChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (((OverviewMap)sender)._controller is OverviewMapController controller)
            {
                controller.PointSymbol = newValue as Symbol;
            }
        }

        private static void OnScaleFactorPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (((OverviewMap)sender)._controller is OverviewMapController controller)
            {
                controller.ScaleFactor = (double)newValue;
            }
        }
    }
}
