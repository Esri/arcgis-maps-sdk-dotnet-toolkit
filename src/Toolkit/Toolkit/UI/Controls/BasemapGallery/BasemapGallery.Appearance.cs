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
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "PART_InnerListView", Type = typeof(ListView))]
    public partial class BasemapGallery
    {
        private ListView? _listView;
        private ItemsPanelTemplate? _listTemplate;
        private ItemsPanelTemplate? _gridTemplate;

        // Track currently applied style to avoid unnecessary re-styling of list view
        private BasemapGalleryViewStyle _currentlyAppliedStyle = BasemapGalleryViewStyle.Automatic;

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            ListView = GetTemplateChild("PART_InnerListView") as ListView;

            SetNewStyle(ActualWidth);
        }

        public Style? ListItemContainerStyle
        {
            get => GetValue(ListItemContainerStyleProperty) as Style;
            set => SetValue(ListItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style used for the container used to display items when showing basemaps in a grid.
        /// </summary>
        public Style? GridItemContainerStyle
        {
            get => GetValue(GridItemContainerStyleProperty) as Style;
            set => SetValue(GridItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to show basemaps in a list.
        /// </summary>
        public DataTemplate? ListItemTemplate
        {
            get => GetValue(ListItemTemplateProperty) as DataTemplate;
            set => SetValue(ListItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to show basemaps in a grid.
        /// </summary>
        public DataTemplate? GridItemTemplate
        {
            get => GetValue(GridItemTemplateProperty) as DataTemplate;
            set => SetValue(GridItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the view style for the gallery.
        /// </summary>
        public BasemapGalleryViewStyle GalleryViewStyle
        {
            get => (BasemapGalleryViewStyle)GetValue(GalleryViewStyleProperty);
            set => SetValue(GalleryViewStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the width threshold to use for deciding between grid and list views when <see cref="GalleryViewStyle"/> is <see cref="BasemapGalleryViewStyle.Automatic"/>.
        /// </summary>
        public double ViewStyleWidthThreshold
        {
            get => (double)GetValue(ViewStyleWidthThresholdProperty);
            set => SetValue(ViewStyleWidthThresholdProperty, value);
        }

        private static void OnViewLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                gallery.SetNewStyle(gallery.ActualWidth);
            }
        }

        private static void OnStyleOrTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                gallery.UpdateListViewForStyle(gallery.GalleryViewStyle, true);
            }
        }

        /// <summary>
        /// Identifies the <see cref="ListItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ListItemContainerStyleProperty =
            DependencyProperty.Register(nameof(ListItemContainerStyle), typeof(Style), typeof(BasemapGallery), new PropertyMetadata(null, OnStyleOrTemplateChanged));

        /// <summary>
        /// Identifies the <see cref="GridItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridItemContainerStyleProperty =
            DependencyProperty.Register(nameof(GridItemContainerStyle), typeof(Style), typeof(BasemapGallery), new PropertyMetadata(null, OnStyleOrTemplateChanged));

        /// <summary>
        /// Identifies the <see cref="ListItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ListItemTemplateProperty =
            DependencyProperty.Register(nameof(ListItemTemplate), typeof(DataTemplate), typeof(BasemapGallery), new PropertyMetadata(null, OnStyleOrTemplateChanged));

        /// <summary>
        /// Identifies the <see cref="GridItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridItemTemplateProperty =
            DependencyProperty.Register(nameof(GridItemTemplate), typeof(DataTemplate), typeof(BasemapGallery), new PropertyMetadata(null, OnStyleOrTemplateChanged));

        /// <summary>
        /// Identifies the <see cref="GalleryViewStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GalleryViewStyleProperty =
            DependencyProperty.Register(nameof(GalleryViewStyle), typeof(BasemapGalleryViewStyle), typeof(BasemapGallery), new PropertyMetadata(BasemapGalleryViewStyle.Automatic, OnViewLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ViewStyleWidthThreshold"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewStyleWidthThresholdProperty =
            DependencyProperty.Register(nameof(ViewStyleWidthThreshold), typeof(double), typeof(BasemapGallery), new PropertyMetadata(440.0, OnViewLayoutPropertyChanged));

        private void BasemapGallerySizeChanged(object sender, SizeChangedEventArgs e) => SetNewStyle(e.NewSize.Width);

        private void SetNewStyle(double currentSize)
        {
            if (ListView == null)
            {
                return;
            }

            if (GalleryViewStyle == BasemapGalleryViewStyle.Automatic)
            {
                UpdateListViewForStyle(currentSize >= ViewStyleWidthThreshold ? BasemapGalleryViewStyle.Grid : BasemapGalleryViewStyle.List);
            }
            else
            {
                UpdateListViewForStyle(GalleryViewStyle);
            }
        }

        private void UpdateListViewForStyle(BasemapGalleryViewStyle style, bool forceReset = false)
        {
            if (_currentlyAppliedStyle == style && !forceReset)
            {
                return;
            }

            if (ListView == null)
            {
                return;
            }

            if (style == BasemapGalleryViewStyle.List)
            {
                ListView.ItemContainerStyle = ListItemContainerStyle;
                ListView.ItemTemplate = ListItemTemplate;
                ListView.ItemsPanel = _listTemplate ??= GetItemsPanelTemplate(typeof(StackPanel));
            }
            else if (style == BasemapGalleryViewStyle.Grid)
            {
                ListView.ItemContainerStyle = GridItemContainerStyle;
                ListView.ItemTemplate = GridItemTemplate;
                ListView.ItemsPanel = _gridTemplate ??= GetItemsPanelTemplate(typeof(WrapPanel));
            }

            _currentlyAppliedStyle = style;
        }

#if NETFX_CORE
        // Shim makes cross-platform code easier
        private class WrapPanel
        {
        }
#endif

        private static ItemsPanelTemplate? GetItemsPanelTemplate(Type panelType)
        {
#if !NETFX_CORE
            return new ItemsPanelTemplate(new FrameworkElementFactory(panelType));
#else
            if (panelType == typeof(StackPanel))
            {
                string xaml = "<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>"
                    + "<ItemsStackPanel />"
                    + "</ItemsPanelTemplate>";
                return XamlReader.Load(xaml) as ItemsPanelTemplate;
            }
            else
            {
                string xaml = "<ItemsPanelTemplate   xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>"
                                   + "<ItemsWrapGrid Orientation=\"Horizontal\" />"
                    + "</ItemsPanelTemplate>";
                return XamlReader.Load(xaml) as ItemsPanelTemplate;
            }
#endif
        }
    }
}
#endif
