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
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI.Controls;
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
    /// <summary>
    /// Displays a collection of images representing basemaps from ArcGIS Online, a user-defined Portal, or a user-defined collection.
    /// </summary>
    /// <remarks>
    /// If connected to a GeoView, changing the basemap selection will change the connected Map or Scene's basemap.
    /// Only basemaps whose spatial reference matches the map or scene's spatial reference can be selected for display.
    /// </remarks>
    [TemplatePart(Name = "PART_InnerListView", Type = typeof(ListView))]
    public class BasemapGallery : Control
    {
        private ListView? _listViewFromTemplate;
        private readonly BasemapGalleryController _controller;
        private ItemsPanelTemplate? _listTemplate;
        private ItemsPanelTemplate? _gridTemplate;

        // Track currently applied style to avoid unnecessary re-styling of list view
        private BasemapGalleryViewStyle _currentlyAppliedStyle = BasemapGalleryViewStyle.Automatic;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGallery"/> class.
        /// </summary>
        public BasemapGallery()
        {
            DefaultStyleKey = typeof(BasemapGallery);
            _controller = new BasemapGalleryController();
            DataContext = this;
            SizeChanged += BasemapGallery_SizeChanged;
        }

        /// <summary>
        /// Gets the data source for the gallery.
        /// </summary>
        public BasemapGalleryController Controller { get => _controller; }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            _listViewFromTemplate = GetTemplateChild("PART_InnerListView") as ListView;
            SetNewStyle(ActualWidth);
        }

        /// <summary>
        /// Gets or sets the portal to use for displaying basemaps.
        /// </summary>
        public ArcGISPortal Portal
        {
            get => (ArcGISPortal)GetValue(PortalProperty);
            set => SetValue(PortalProperty, value);
        }

        /// <summary>
        /// Gets or sets the connected GeoView.
        /// </summary>
        public GeoView GeoView
        {
            get => (GeoView)GetValue(GeoViewProperty);
            set => SetValue(GeoViewProperty, value);
        }

        /// <summary>
        /// Gets or sets the style used for the container used to display items when showing basemaps in a list.
        /// </summary>
        public Style ListItemContainerStyle
        {
            get => (Style)GetValue(ListItemContainerStyleProperty);
            set => SetValue(ListItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style used for the container used to display items when showing basemaps in a grid.
        /// </summary>
        public Style GridItemContainerStyle
        {
            get => (Style)GetValue(GridItemContainerStyleProperty);
            set => SetValue(GridItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to show basemaps in a list.
        /// </summary>
        public DataTemplate ListItemTemplate
        {
            get => (DataTemplate)GetValue(ListItemTemplateProperty);
            set => SetValue(ListItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to show basemaps in a grid.
        /// </summary>
        public DataTemplate GridItemTemplate
        {
            get => (DataTemplate)GetValue(GridItemTemplateProperty);
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

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BasemapGallery)d)._controller.GeoView = e.NewValue as GeoView;

        private static void OnPortalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BasemapGallery)d)._controller.Portal = e.NewValue as ArcGISPortal;

        private static void OnViewLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gallery = (BasemapGallery)d;
            gallery.SetNewStyle(gallery.ActualWidth);
        }

        private static void OnStyleOrTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gallery = (BasemapGallery)d;
            gallery.UpdateListViewForStyle(gallery.GalleryViewStyle, true);
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
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(BasemapGallery), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Portal"/> dependency proeprty.
        /// </summary>
        public static readonly DependencyProperty PortalProperty =
            DependencyProperty.Register(nameof(Portal), typeof(ArcGISPortal), typeof(BasemapGallery), new PropertyMetadata(null, OnPortalPropertyChanged));

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

        private void BasemapGallery_SizeChanged(object sender, SizeChangedEventArgs e) => SetNewStyle(e.NewSize.Width);

        private void SetNewStyle(double currentSize)
        {
            if (_listViewFromTemplate == null)
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

            if (_listViewFromTemplate == null)
            {
                return;
            }

            if (style == BasemapGalleryViewStyle.List)
            {
                _listViewFromTemplate.ItemContainerStyle = ListItemContainerStyle;
                _listViewFromTemplate.ItemTemplate = ListItemTemplate;
                _listViewFromTemplate.ItemsPanel = _listTemplate ??= GetItemsPanelTemplate(typeof(StackPanel));
            }
            else if (style == BasemapGalleryViewStyle.Grid)
            {
                _listViewFromTemplate.ItemContainerStyle = GridItemContainerStyle;
                _listViewFromTemplate.ItemTemplate = GridItemTemplate;
                _listViewFromTemplate.ItemsPanel = _gridTemplate ??= GetItemsPanelTemplate(typeof(WrapPanel));
            }

            _currentlyAppliedStyle = style;
        }

#if NETFX_CORE
        // Shim makes cross-platform code easier
        private class WrapPanel
        {
        }
#endif

        private static ItemsPanelTemplate GetItemsPanelTemplate(Type panelType)
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
