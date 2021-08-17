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
using Esri.ArcGISRuntime.Mapping;
using System.Collections.Generic;
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
        private ListView? _listView;
        private readonly BasemapGalleryDataSource _controller;
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
            _controller = new BasemapGalleryDataSource();
            DataContext = this;
            SizeChanged += BasemapGallery_SizeChanged;
            _controller.PropertyChanged += _controller_PropertyChanged;
            _controller.CollectionChanged += _controller_CollectionChanged;
            _ = _controller.PopulateFromDefaultList();
        }

        private void _controller_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                if (ListView.Items.Contains(_controller.SelectedBasemap))
                {
                    ListView.SelectedItem = _controller.SelectedBasemap;
                }
            }
        }

        private void _controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_controller.SelectedBasemap))
            {
                if (_controller.SelectedBasemap != null)
                {
                    BasemapSelected?.Invoke(this, _controller.SelectedBasemap);
                }

                ListView.SelectedItem = _controller.SelectedBasemap;
            }
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            ListView = GetTemplateChild("PART_InnerListView") as ListView;

            if (ListView != null)
            {
                ListView.ItemsSource = _controller;
            }

            SetNewStyle(ActualWidth);
        }

        private ListView? ListView
        {
            get => _listView;
            set
            {
                if (value != _listView)
                {
                    if (_listView != null)
                    {
                        _listView.SelectionChanged -= ListSelectionChanged;
                    }

                    _listView = value;

                    if (_listView != null)
                    {
                        _listView.SelectionChanged += ListSelectionChanged;
                    }
                }
            }
        }

        private void ListSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is BasemapGalleryItem item)
            {
                if (item.IsValid)
                {
                    _controller.SelectedBasemap = item;
                    BasemapSelected?.Invoke(this, item);
                }
                else if (sender is ListView lv)
                {
                    lv.SelectedItem = null;
                }
            }
        }

        public event EventHandler<BasemapGalleryItem>? BasemapSelected;

        /// <summary>
        /// Gets or sets the portal to use for displaying basemaps.
        /// </summary>
        public ArcGISPortal? Portal
        {
            get => GetValue(PortalProperty) as ArcGISPortal;
            set => SetValue(PortalProperty, value);
        }

        /// <summary>
        /// Gets or sets the connected GeoModel.
        /// </summary>
        public GeoModel? GeoModel
        {
            get => GetValue(GeoModelProperty) as GeoModel;
            set => SetValue(GeoModelProperty, value);
        }

        /// <summary>
        /// Gets or sets the style used for the container used to display items when showing basemaps in a list.
        /// </summary>
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

        public IEnumerable<BasemapGalleryItem>? OverrideList
        {
            get => GetValue(OverrideListProperty) as IEnumerable<BasemapGalleryItem>;
            set => SetValue(OverrideListProperty, value);
        }

        // Using a DependencyProperty as the backing store for OverrideList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverrideListProperty =
            DependencyProperty.Register("OverrideList", typeof(IEnumerable<BasemapGalleryItem>), typeof(BasemapGallery), new PropertyMetadata(null, OnOverrideListPropertyChanged));

        private static void OnGeoModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BasemapGallery)d)._controller.GeoModel = e.NewValue as GeoModel;

        private static void OnPortalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BasemapGallery)d)._controller.PopulateBasemapsForPortal(e.NewValue as ArcGISPortal);

        private static void OnOverrideListPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BasemapGallery)d)._controller.SetOverrideList(e.NewValue as IEnumerable<BasemapGalleryItem>);

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
        /// Identifies the <see cref="GeoModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoModelProperty =
            DependencyProperty.Register(nameof(GeoModel), typeof(GeoModel), typeof(BasemapGallery), new PropertyMetadata(null, OnGeoModelPropertyChanged));

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
