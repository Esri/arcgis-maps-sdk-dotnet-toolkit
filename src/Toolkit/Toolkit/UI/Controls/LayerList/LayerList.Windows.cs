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
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "List", Type = typeof(ItemsControl))]
    public partial class LayerList
    {
        private void Initialize() => DefaultStyleKey = GetType();

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

        /// <summary>
        /// Generates layer list for a set of <see cref="Layer"/>s in a <see cref="Map"/> or <see cref="Scene"/>
        /// contained in a <see cref="GeoView"/>
        /// </summary>
        internal void Refresh()
        {
            var list = GetTemplateChild("List") as ItemsControl;
            if (list == null)
            {
                return;
            }
#if !NETFX_CORE
            ContextMenuService.AddContextMenuOpeningHandler(list, ContextMenuEventHandler);
#endif

            if ((GeoView as MapView)?.Map == null && (GeoView as SceneView)?.Scene == null)
            {
                list.ItemsSource = null;
                return;
            }

            ObservableLayerContentList layers = null;
            if (GeoView is MapView)
            {
                layers = new ObservableLayerContentList(GeoView as MapView, ShowLegendInternal)
                {
                    ReverseOrder = !ReverseLayerOrder,
                };

                Binding b = new Binding();
                b.Source = GeoView;
                b.Path = new PropertyPath(nameof(MapView.Map));
                b.Mode = BindingMode.OneWay;
                SetBinding(DocumentProperty, b);
            }
            else if (GeoView is SceneView)
            {
                layers = new ObservableLayerContentList(GeoView as SceneView, ShowLegendInternal)
                {
                    ReverseOrder = !ReverseLayerOrder,
                };

                Binding b = new Binding();
                b.Source = GeoView;
                b.Path = new PropertyPath(nameof(SceneView.Scene));
                b.Mode = BindingMode.OneWay;
                SetBinding(DocumentProperty, b);
            }

            if (layers == null)
            {
                list.ItemsSource = null;
                return;
            }

            foreach (var l in layers)
            {
                if (!(l.LayerContent is Layer))
                {
                    continue;
                }

                var layer = l.LayerContent as Layer;
                if (layer.LoadStatus == LoadStatus.Loaded)
                {
                    l.UpdateLayerViewState(GeoView.GetLayerViewState(layer));
                }
            }

            ScaleChanged();
            SetLayerContentList(layers);
            list.ItemsSource = layers;
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        private GeoView GeoViewImpl
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(LayerList), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contents = (LayerList)d;
            contents.OnViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scale of <see cref="GeoView"/> and any scale ranges on the <see cref="Layer"/>s
        /// are used to determine when legend for layer is displayed.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layer is displayed only when layer is in visible scale range;
        /// otherwise, <c>false</c>, legend for layer is displayed regardless of its scale range.
        /// </remarks>
        private bool FilterByVisibleScaleRangeImpl
        {
            get { return (bool)GetValue(FilterByVisibleScaleRangeProperty); }
            set { SetValue(FilterByVisibleScaleRangeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FilterByVisibleScaleRange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterByVisibleScaleRangeProperty =
            DependencyProperty.Register(nameof(FilterByVisibleScaleRange), typeof(bool), typeof(Legend), new PropertyMetadata(true, OnFilterByVisibleScaleRangePropertyChanged));

        private static void OnFilterByVisibleScaleRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayerList)d).UpdateLegendVisiblity();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the order of layers in the <see cref="GeoView"/>, top to bottom, is used.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layers is displayed from top to bottom order;
        /// otherwise, <c>false</c>, legend for layers is displayed from bottom to top order.
        /// </remarks>
        private bool ReverseLayerOrderImpl
        {
            get { return (bool)GetValue(ReverseLayerOrderProperty); }
            set { SetValue(ReverseLayerOrderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ReverseLayerOrder"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ReverseLayerOrderProperty =
            DependencyProperty.Register(nameof(ReverseLayerOrder), typeof(bool), typeof(LayerList), new PropertyMetadata(false, OnReverseLayerOrderPropertyChanged));

        private static void OnReverseLayerOrderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayerList)d)._layerContentList.ReverseOrder = !(bool)e.NewValue;
        }

        private static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof(object), typeof(LayerList), new PropertyMetadata(null, OnDocumentPropertyChanged));

        private static void OnDocumentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayerList)d).Refresh();
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
            DependencyProperty.Register(nameof(LayerItemTemplate), typeof(DataTemplate), typeof(LayerList), new PropertyMetadata(null));

#if !NETFX_CORE

        private void ContextMenuEventHandler(object sender, ContextMenuEventArgs e)
        {
            (sender as FrameworkElement).ContextMenu = null;
            var vm = (e.OriginalSource as FrameworkElement)?.DataContext as LayerContentViewModel;
            if (vm != null && vm.LayerContent != null)
            {
                if (LayerContentContextMenuOpening != null)
                {
                    var args = new LayerContentContextMenuEventArgs(sender, e)
                    {
                        MenuItems = new System.Collections.Generic.List<MenuItem>(),
                        LayerContent = vm.LayerContent
                    };
                    args.Menu = new ContextMenu() { ItemsSource = args.MenuItems };
                    LayerContentContextMenuOpening?.Invoke(this, args);
                    e.Handled = args.Handled;
                    if (args.MenuItems.Count > 0)
                    {
                        (sender as FrameworkElement).ContextMenu = args.Menu;
                    }
                }
            }
        }

        /// <summary>
        /// Event fired by the <see cref="LayerList"/> when right-clicking an item
        /// </summary>
        public event System.EventHandler<LayerContentContextMenuEventArgs> LayerContentContextMenuOpening;
#endif
    }
}
#endif