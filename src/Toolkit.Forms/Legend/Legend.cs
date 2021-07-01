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
using Esri.ArcGISRuntime.Toolkit.UI;
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
                var nameLabel = new Label { FontSize = 18, VerticalOptions = LayoutOptions.Center };
                nameLabel.SetBinding(Label.TextProperty, $"{nameof(LegendEntry.Content)}.{nameof(Layer.Name)}");
                return new ViewCell() { View = nameLabel };
            });

            s_DefaultSublayerItemTemplate = new DataTemplate(() =>
            {
                var nameLabel = new Label { FontSize = 14, VerticalOptions = LayoutOptions.Center };
                nameLabel.SetBinding(Label.TextProperty, $"{nameof(LegendEntry.Content)}.{nameof(ILayerContent.Name)}");
                return new ViewCell() { View = nameLabel };
            });

            s_DefaultLegendInfoItemTemplate = new DataTemplate(() =>
            {
                StackLayout sl = new StackLayout() { Orientation = StackOrientation.Horizontal };
                var symbol = new SymbolDisplay { WidthRequest = 40, HeightRequest = 40, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 5, 0) };
                symbol.SetBinding(SymbolDisplay.SymbolProperty, $"{nameof(LegendEntry.Content)}.{nameof(LegendInfo.Symbol)}");
                sl.Children.Add(symbol);
                var nameLabel = new Label { FontSize = 12, VerticalOptions = LayoutOptions.Center };
                nameLabel.SetBinding(Label.TextProperty, $"{nameof(LegendEntry.Content)}.{nameof(LegendInfo.Name)}");
                sl.Children.Add(nameLabel);
                return new ViewCell() { View = sl };
            });

            string template = @"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
                                    <ListView x:Name=""ListView"" HorizontalOptions=""Fill"" VerticalOptions=""Fill"" SelectionMode=""None"" SeparatorVisibility=""None"" >
                                        <x:Arguments>
                                            <ListViewCachingStrategy>RecycleElement</ListViewCachingStrategy>
                                        </x:Arguments>
                                    </ListView>
                                </ControlTemplate>";
            s_DefaultControlTemplate = global::Xamarin.Forms.Xaml.Extensions.LoadFromXaml<ControlTemplate>(new ControlTemplate(), template);
        }

        private readonly LegendDataSource _datasource;

        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class.
        /// </summary>
        public Legend()
        {
            _datasource = new LegendDataSource(this);
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
            LayerItemTemplate = s_DefaultLayerItemTemplate;
            SublayerItemTemplate = s_DefaultSublayerItemTemplate;
            LegendInfoItemTemplate = s_DefaultLegendInfoItemTemplate;
            ControlTemplate = s_DefaultControlTemplate;
        }

        /// <inheritdoc />
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

        private static void OnGeoViewPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
        {
            ((Legend)bindable)._datasource.SetGeoView(newValue as GeoView);
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        /// <seealso cref="GeoViewProperty"/>
        public GeoView? GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FilterByVisibleScaleRange"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FilterByVisibleScaleRangeProperty =
            BindableProperty.Create(nameof(FilterByVisibleScaleRange), typeof(bool), typeof(Legend), true, BindingMode.OneWay, propertyChanged: OnFilterByVisibleScaleRangePropertyChanged);

        private static void OnFilterByVisibleScaleRangePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((Legend)bindable)._datasource.FilterByVisibleScaleRange = (bool)newValue;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scale of <see cref="GeoView"/> and any scale ranges on the <see cref="Layer"/>s
        /// are used to determine when legend for layer is displayed.
        /// </summary>
        /// <value>
        /// If <c>true</c>, legend for layer is displayed only when layer is in visible scale range;
        /// otherwise, <c>false</c>, legend for layer is displayed regardless of its scale range.
        /// </value>
        public bool FilterByVisibleScaleRange
        {
            get { return (bool)GetValue(FilterByVisibleScaleRangeProperty); }
            set { SetValue(FilterByVisibleScaleRangeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FilterHiddenLayers"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FilterHiddenLayersProperty =
            BindableProperty.Create(nameof(FilterHiddenLayers), typeof(bool), typeof(Legend), true, BindingMode.OneWay, propertyChanged: OnFilterHiddenLayersPropertyChanged);

        private static void OnFilterHiddenLayersPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((Legend)bindable)._datasource.FilterHiddenLayers = (bool)newValue;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the visibility of a <see cref="Layer"/>
        /// is used to determine when the legend for the layer is displayed.
        /// </summary>
        /// <value>
        /// If <c>true</c>, legend for the layer and sublayers is displayed only when the layer's <see cref="ILayerContent.IsVisible"/> property is true.
        /// </value>
        public bool FilterHiddenLayers
        {
            get { return (bool)GetValue(FilterHiddenLayersProperty); }
            set { SetValue(FilterHiddenLayersProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ReverseLayerOrder"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ReverseLayerOrderProperty =
            BindableProperty.Create(nameof(ReverseLayerOrder), typeof(bool), typeof(Legend), true, BindingMode.OneWay, propertyChanged: OnReverseLayerOrderPropertyChanged);

        private static void OnReverseLayerOrderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((Legend)bindable)._datasource.ReverseLayerOrder = (bool)newValue;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the order of layers in the <see cref="GeoView"/> are displayed top to bottom.
        /// </summary>
        /// <value>
        /// If <c>true</c>, legend for layers is displayed from top to bottom order;
        /// otherwise, <c>false</c>, legend for layers is displayed from bottom to top order.
        /// </value>
        public bool ReverseLayerOrder
        {
            get { return (bool)GetValue(ReverseLayerOrderProperty); }
            set { SetValue(ReverseLayerOrderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LayerItemTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LayerItemTemplateProperty =
            BindableProperty.Create(nameof(LayerItemTemplate), typeof(DataTemplate), typeof(Legend), s_DefaultLayerItemTemplate, BindingMode.OneWay, null);

        /// <summary>
        /// Gets or sets the item template for each <see cref="Layer" /> entry.
        /// </summary>
        /// <remarks>
        /// If this is set to null, the <see cref="SublayerItemTemplate"/> will be used instead.
        /// </remarks>
        /// <seealso cref="SublayerItemTemplate"/>
        /// <seealso cref="LegendInfoItemTemplate"/>
        public DataTemplate? LayerItemTemplate
        {
            get { return GetValue(LayerItemTemplateProperty) as DataTemplate; }
            set { SetValue(LayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SublayerItemTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SublayerItemTemplateProperty =
            BindableProperty.Create(nameof(SublayerItemTemplate), typeof(DataTemplate), typeof(Legend), s_DefaultSublayerItemTemplate, BindingMode.OneWay, null);

        /// <summary>
        /// Gets or sets the item template for each <see cref="ILayerContent"/> entry that is not a <see cref="Layer"/>.
        /// </summary>
        /// <seealso cref="LegendInfoItemTemplate"/>
        /// <seealso cref="LayerItemTemplate"/>
        public DataTemplate? SublayerItemTemplate
        {
            get { return GetValue(SublayerItemTemplateProperty) as DataTemplate; }
            set { SetValue(SublayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LegendInfoItemTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LegendInfoItemTemplateProperty =
            BindableProperty.Create(nameof(LegendInfoItemTemplate), typeof(DataTemplate), typeof(Legend), s_DefaultLegendInfoItemTemplate, BindingMode.OneWay, null);

        /// <summary>
        /// Gets or sets the item template for each <see cref="LegendInfo"/> content entry.
        /// </summary>
        /// <seealso cref="SublayerItemTemplate"/>
        /// <seealso cref="LayerItemTemplate"/>
        public DataTemplate? LegendInfoItemTemplate
        {
            get { return GetValue(LegendInfoItemTemplateProperty) as DataTemplate; }
            set { SetValue(LegendInfoItemTemplateProperty, value); }
        }
    }
}
