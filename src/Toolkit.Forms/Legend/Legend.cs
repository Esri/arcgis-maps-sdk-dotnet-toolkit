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
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The Legend control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="GeoView"/>.
    /// </summary>
    public class Legend : TemplatedView
    {
        private static DataTemplate s_DefaultLayerItemTemplate;
        private static DataTemplate s_DefaultSublayerItemTemplate;
        private static DataTemplate s_DefaultLegendInfoItemTemplate;
        private static ControlTemplate s_DefaultControlTemplate;

        static Legend()
        {
            s_DefaultLayerItemTemplate = new DataTemplate(() =>
            {
                var nameLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 24 };
                nameLabel.SetBinding(Label.TextProperty, nameof(Layer.Name));
                return new ViewCell() { View = nameLabel };
            });

            s_DefaultSublayerItemTemplate = new DataTemplate(() =>
            {
                var nameLabel = new Label { FontSize = 18 };
                nameLabel.SetBinding(Label.TextProperty, nameof(ILayerContent.Name));
                return new ViewCell() { View = nameLabel };
            });

            s_DefaultLegendInfoItemTemplate = new DataTemplate(() =>
            {
                StackLayout sl = new StackLayout() { Orientation = StackOrientation.Horizontal };
                var symbol = new SymbolDisplay { WidthRequest = 40, HeightRequest = 40, VerticalOptions = LayoutOptions.Center };
                symbol.SetBinding(SymbolDisplay.SymbolProperty, nameof(LegendInfo.Symbol));
                sl.Children.Add(symbol);
                var nameLabel = new Label { FontSize = 11, VerticalOptions = LayoutOptions.Center };
                nameLabel.SetBinding(Label.TextProperty, nameof(LegendInfo.Name));
                sl.Children.Add(nameLabel);
                return new ViewCell() { View = sl };
            });

            string template = "<ControlTemplate xmlns=\"http://xamarin.com/schemas/2014/forms\" xmlns:x=\"http://schemas.microsoft.com/winfx/2009/xaml\" xmlns:esriTK=\"clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms\"><ListView x:Name=\"ListView\" HorizontalOptions=\"Fill\" VerticalOptions=\"Fill\" SelectionMode=\"None\" ><x:Arguments><ListViewCachingStrategy>RecycleElement</ListViewCachingStrategy></x:Arguments></ListView></ControlTemplate>";
            s_DefaultControlTemplate = global::Xamarin.Forms.Xaml.Extensions.LoadFromXaml<ControlTemplate>(new ControlTemplate(), template);
        }

        private readonly LegendDataSource _datasource = new LegendDataSource(null);

        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class
        /// </summary>
        public Legend()
        {
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
            LayerItemTemplate = s_DefaultLayerItemTemplate;
            SublayerItemTemplate = s_DefaultSublayerItemTemplate;
            LegendInfoItemTemplate = s_DefaultLegendInfoItemTemplate;
            ControlTemplate = s_DefaultControlTemplate;
        }

        protected override void OnApplyTemplate()
        {
            var list = GetTemplateChild("ListView") as ItemsView<Cell>;
            if (list != null)
            {
                list.ItemTemplate = new LegendItemTemplateSelector(this);
                list.ItemsSource = _datasource;
            }

            base.OnApplyTemplate();
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(Legend), null, BindingMode.OneWay, propertyChanged: OnGeoViewPropertyChanged);

        private static void OnGeoViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((Legend)bindable)._datasource.SetGeoView(newValue as GeoView);
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        /// <seealso cref="GeoViewProperty"/>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LayerItemTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LayerItemTemplateProperty =
            BindableProperty.Create(nameof(LayerItemTemplate), typeof(DataTemplate), typeof(Legend), s_DefaultLayerItemTemplate, BindingMode.OneWay, null);

        public DataTemplate LayerItemTemplate
        {
            get { return (DataTemplate)GetValue(LayerItemTemplateProperty); }
            set { SetValue(LayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SublayerItemTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SublayerItemTemplateProperty =
            BindableProperty.Create(nameof(SublayerItemTemplate), typeof(DataTemplate), typeof(Legend), s_DefaultSublayerItemTemplate, BindingMode.OneWay, null);

        public DataTemplate SublayerItemTemplate
        {
            get { return (DataTemplate)GetValue(SublayerItemTemplateProperty); }
            set { SetValue(SublayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LegendInfoItemTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LegendInfoItemTemplateProperty =
            BindableProperty.Create(nameof(LegendInfoItemTemplate), typeof(DataTemplate), typeof(Legend), s_DefaultLegendInfoItemTemplate, BindingMode.OneWay, null);

        public DataTemplate LegendInfoItemTemplate
        {
            get { return (DataTemplate)GetValue(LegendInfoItemTemplateProperty); }
            set { SetValue(LegendInfoItemTemplateProperty, value); }
        }
    }
}
