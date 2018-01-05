// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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
using System.Windows;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The Legend Control that generates a list of Legend Items for a Layer
    /// </summary>
    public class LayerLegend : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class.
        /// </summary>
        public LayerLegend()
        {
            DefaultStyleKey = typeof(LayerLegend);
        }

        /// <inheritdoc/>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            UpdateLegend();
        }

        /// <summary>
        /// Gets or sets the layer to display the legend for.
        /// </summary>
        public ILayerContent LayerContent
        {
            get { return (ILayerContent)GetValue(LayerProperty); }
            set { SetValue(LayerProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LayerContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerProperty =
            DependencyProperty.Register(nameof(LayerContent), typeof(ILayerContent), typeof(LayerLegend), new PropertyMetadata(null, (d, e) => (d as LayerLegend)?.UpdateLegend()));

        private void UpdateLegend()
        {
            var ctrl = GetTemplateChild("ItemsList") as ItemsControl;
            if (ctrl == null || LayerContent == null)
            {
                if (ctrl != null)
                {
                    ctrl.ItemsSource = null;
                }

                return;
            }

            if (LayerContent is ILoadable)
            {
                if ((LayerContent as ILoadable).LoadStatus != LoadStatus.Loaded)
                {
                    (LayerContent as ILoadable).Loaded += Layer_Loaded;
                    return;
                }
            }

            var items = new ObservableCollection<LayerLegendInfo>();
            ctrl.ItemsSource = items;
            LoadRecursive(items, LayerContent, ShowEntireTreeHiarchy);
        }

        private async void LoadRecursive(IList<LayerLegendInfo> itemsList, ILayerContent content, bool recursive)
        {
            if (content == null)
            {
                return;
            }

            try
            {
                if(LayerContent.Name != content.Name)
                    itemsList.Add(new LayerLegendInfo(content));
#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
                var legendInfo = await content.GetLegendInfosAsync();
#pragma warning restore ESRI1800
                foreach (var item in legendInfo)
                {
                    itemsList.Add(new LayerLegendInfo(item));
                }
            }
            catch
            {
                return;
            }

            if (recursive)
            {
                foreach (var item in content.SublayerContents)
                {
                    LoadRecursive(itemsList, item, recursive);
                }
            }
        }

        private void Layer_Loaded(object sender, EventArgs e)
        {
            (sender as Layer).Loaded -= Layer_Loaded;
#if NETFX_CORE
            var ignore_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, UpdateLegend);
#else
            var ignore = Dispatcher.InvokeAsync(UpdateLegend);
#endif
        }

        /// <summary>
        /// Gets or sets the ItemsTemplate
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
        /// Gets or sets the Items Panel Template
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

        /// <summary>
        /// Gets or sets a value indicating whether the entire <see cref="ILayerContent"/> tree hiarchy should be rendered
        /// </summary>
        public bool ShowEntireTreeHiarchy
        {
            get { return (bool)GetValue(ShowEntireTreeHiarchyProperty); }
            set { SetValue(ShowEntireTreeHiarchyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowEntireTreeHiarchy"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowEntireTreeHiarchyProperty =
            DependencyProperty.Register(nameof(ShowEntireTreeHiarchy), typeof(bool), typeof(LayerLegend), new PropertyMetadata(true, (d, e) => (d as LayerLegend)?.UpdateLegend()));
    }
    
    internal class LayerLegendInfo
    {
        internal LayerLegendInfo(LegendInfo info)
        {
            Name = info?.Name;
            Symbol = info?.Symbol ?? null;
        }

        internal LayerLegendInfo(ILayerContent content)
        {
            Name = content?.Name;
            Symbol = null;
        }

        public string Name { get; }
        public Symbology.Symbol Symbol { get; }
    }
}
#endif