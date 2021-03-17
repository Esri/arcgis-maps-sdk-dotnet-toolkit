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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Mapping;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class LayerLegend
    {
        private void Initialize() => DefaultStyleKey = typeof(LayerLegend);

        /// <inheritdoc/>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            Refresh();
        }

        private void Refresh()
        {
            var ctrl = GetTemplateChild("ItemsList") as ItemsControl;

            if (ctrl == null)
            {
                return;
            }

            if (LayerContent == null)
            {
                ctrl.ItemsSource = null;
                return;
            }

            if (LayerContent is ILoadable)
            {
                if ((LayerContent as ILoadable).LoadStatus != LoadStatus.Loaded)
                {
                    (LayerContent as ILoadable).Loaded += Layer_Loaded;
                    (LayerContent as ILoadable).LoadAsync();
                    return;
                }
            }

            var items = new ObservableCollection<LegendInfo>();
            ctrl.ItemsSource = items;
            LoadRecursive(items, LayerContent, IncludeSublayers);
        }

        /// <summary>
        /// Gets or sets the layer to display the legend for.
        /// </summary>
        private ILayerContent LayerContentImpl
        {
            get { return (ILayerContent)GetValue(LayerProperty); }
            set { SetValue(LayerProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LayerContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerProperty =
            DependencyProperty.Register(nameof(LayerContent), typeof(ILayerContent), typeof(LayerLegend), new PropertyMetadata(null, (d, e) => (d as LayerLegend)?.Refresh()));

        /// <summary>
        /// Gets or sets a value indicating whether the entire <see cref="ILayerContent"/> tree hierarchy should be rendered.
        /// </summary>
        private bool IncludeSublayersImpl
        {
            get { return (bool)GetValue(IncludeSublayersProperty); }
            set { SetValue(IncludeSublayersProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IncludeSublayers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IncludeSublayersProperty =
            DependencyProperty.Register(nameof(IncludeSublayers), typeof(bool), typeof(LayerLegend), new PropertyMetadata(true, (d, e) => (d as LayerLegend)?.Refresh()));

        private void Layer_Loaded(object sender, EventArgs e)
        {
            (sender as ILoadable).Loaded -= Layer_Loaded;
#if NETFX_CORE
            var ignore_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Refresh);
#else
            var ignore = Dispatcher.InvokeAsync(Refresh);
#endif
        }

        /// <summary>
        /// Gets or sets the ItemsTemplate.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(LayerLegend), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the Items Panel Template.
        /// </summary>
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemsPanel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register(nameof(ItemsPanel), typeof(ItemsPanelTemplate), typeof(LayerLegend), new PropertyMetadata(null));
    }
}
#endif