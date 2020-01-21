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
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class Legend : Control
    {
        private void Initialize()
        {
            DefaultStyleKey = typeof(Legend);
            ItemTemplateSelector = new LegendItemTemplateSelector(this);
        }

#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            var listView = GetTemplateChild("List") as ItemsControl;
            if (listView != null)
            {
                listView.ItemsSource = _datasource;
            }
        }

        /// <summary>
        /// Identifies the <see cref="FilterByVisibleScaleRange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterByVisibleScaleRangeProperty =
            DependencyProperty.Register(nameof(FilterByVisibleScaleRange), typeof(bool), typeof(Legend), new PropertyMetadata(true, OnFilterByVisibleScaleRangePropertyChanged));

        private static void OnFilterByVisibleScaleRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Legend)d)._datasource.FilterByVisibleScaleRange = (bool)e.NewValue;
        }

        /// <summary>
        /// Identifies the <see cref="FilterHiddenLayers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterHiddenLayersProperty =
            DependencyProperty.Register(nameof(FilterHiddenLayers), typeof(bool), typeof(Legend), new PropertyMetadata(true, OnFilterHiddenLayersPropertyChanged));

        private static void OnFilterHiddenLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Legend)d)._datasource.FilterHiddenLayers = (bool)e.NewValue;
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(Legend), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Legend)d)._datasource.SetGeoView(e.NewValue as GeoView);
        }

        /// <summary>
        /// Gets or sets the item template for each layer content entry
        /// </summary>
        public DataTemplate LayerItemTemplate
        {
            get { return (DataTemplate)GetValue(LayerItemTemplateProperty); }
            set { SetValue(LayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LayerItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerItemTemplateProperty =
            DependencyProperty.Register(nameof(LayerItemTemplate), typeof(DataTemplate), typeof(Legend), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the item template for each layer content entry
        /// </summary>
        public DataTemplate SublayerItemTemplate
        {
            get { return (DataTemplate)GetValue(SublayerItemTemplateProperty); }
            set { SetValue(SublayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SublayerItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SublayerItemTemplateProperty =
                DependencyProperty.Register(nameof(SublayerItemTemplate), typeof(DataTemplate), typeof(Legend), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the item template for each layer content entry
        /// </summary>
        public DataTemplate LegendInfoItemTemplate
        {
            get { return (DataTemplate)GetValue(LegendInfoItemTemplateProperty); }
            set { SetValue(LegendInfoItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LegendInfoItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LegendInfoItemTemplateProperty =
                DependencyProperty.Register(nameof(LegendInfoItemTemplate), typeof(DataTemplate), typeof(Legend), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the item template for each layer content entry
        /// </summary>
        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplateSelector"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateSelectorProperty =
                DependencyProperty.Register(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(Legend), new PropertyMetadata(null));
    }
}
#endif