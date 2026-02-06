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

#if WPF || WINDOWS_XAML
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Displays a collection of images representing basemaps from ArcGIS Online, a user-defined Portal, or a user-defined collection.
    /// </summary>
    /// <remarks>
    /// If connected to a GeoView, changing the basemap selection will change the connected Map or Scene's basemap.
    /// Only basemaps whose spatial reference matches the map or scene's spatial reference can be selected for display.
    /// </remarks>
    public partial class BasemapGallery : Control
    {
        private readonly BasemapGalleryController _controller;
        private bool isAvailableBasemapCollectionInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGallery"/> class.
        /// </summary>
        public BasemapGallery()
        {
            _controller = new BasemapGalleryController();
            DefaultStyleKey = typeof(BasemapGallery);
            SizeChanged += BasemapGallerySizeChanged;
            AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>();
            isAvailableBasemapCollectionInitialized = true;
            _controller.PropertyChanged += HandleControllerPropertyChanged;
            Loaded += BasemapGallery_Loaded;
        }

        private async void BasemapGallery_Loaded(object? sender, RoutedEventArgs e)
        {
            // Unsubscribe from the Loaded event to ensure this only runs once.
            Loaded -= BasemapGallery_Loaded;

            try
            {
                if ((AvailableBasemaps is null || !AvailableBasemaps.Any()) && isAvailableBasemapCollectionInitialized)
                {
                    await _controller.UpdateBasemaps();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to load basemaps: {ex.Message}", "ArcGIS Maps SDK Toolkit");
            }
        }

        private void HandleControllerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BasemapGalleryController.AvailableBasemaps):
                    AvailableBasemaps = _controller.AvailableBasemaps;
                    break;
                case nameof(BasemapGalleryController.IsLoading):
                    _loadingScrim?.SetValue(FrameworkElement.VisibilityProperty, _controller.IsLoading ? Visibility.Visible : Visibility.Collapsed);
                    break;
                case nameof(BasemapGalleryController.SelectedBasemap):
                    ListView?.SetValue(ListView.SelectedItemProperty, _controller.SelectedBasemap);
                    SelectedBasemap = _controller.SelectedBasemap;
                    if (_controller.SelectedBasemap != null)
                    {
                        BasemapSelected?.Invoke(this, _controller.SelectedBasemap);
                    }

                    break;
            }
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
                        _listView.SelectionChanged -= ListViewSelectionChanged;
                    }

                    _listView = value;

                    if (_listView != null)
                    {
                        _listView.SelectionChanged += ListViewSelectionChanged;
                    }
                }
            }
        }

        private static void SelectedBasemapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                gallery._controller.SelectedBasemap = e.NewValue as BasemapGalleryItem;
            }
        }

        private static void GeoModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                gallery._controller.GeoModel = e.NewValue as GeoModel;
            }
        }

        private void ListViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ListView == null)
            {
                return;
            }

            SelectedBasemap = ListView.SelectedItem as BasemapGalleryItem;
        }

        private static void PortalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                gallery._controller.Portal = gallery.Portal;
            }
        }

        private static void AvailableBasemapsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                gallery.isAvailableBasemapCollectionInitialized = false;
                if (e.NewValue != gallery._controller.AvailableBasemaps)
                {
                    gallery._controller.AvailableBasemaps = e.NewValue as IList<BasemapGalleryItem>;
                }
            }
        }

        #region Convenience Properties

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
        /// Gets or sets the gallery of basemaps to show.
        /// </summary>
        /// <remarks>
        /// When <see cref="Portal"/> is set, this collection will be overwritten.
        /// </remarks>
        public IList<BasemapGalleryItem>? AvailableBasemaps
        {
            get => GetValue(AvailableBasemapsProperty) as IList<BasemapGalleryItem>;
            set => SetValue(AvailableBasemapsProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected basemap.
        /// </summary>
        /// <remarks>
        /// This will be set to the gallery item that matches the basemap in the <see cref="GeoModel"/>.
        /// </remarks>
        public BasemapGalleryItem? SelectedBasemap
        {
            get => GetValue(SelectedBasemapProperty) as BasemapGalleryItem;
            set => SetValue(SelectedBasemapProperty, value);
        }

        #endregion Convenience Properties

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="GeoModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoModelProperty =
            DependencyProperty.Register(nameof(GeoModel), typeof(GeoModel), typeof(BasemapGallery), new PropertyMetadata(null, GeoModelChanged));

        /// <summary>
        /// Identifies the <see cref="Portal"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PortalProperty =
            DependencyProperty.Register(nameof(Portal), typeof(ArcGISPortal), typeof(BasemapGallery), new PropertyMetadata(null, PortalChanged));

        /// <summary>
        /// Identifies the <see cref="AvailableBasemaps"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AvailableBasemapsProperty =
            DependencyProperty.Register(nameof(AvailableBasemaps), typeof(IList<BasemapGalleryItem>), typeof(BasemapGallery), new PropertyMetadata(null, AvailableBasemapsChanged));

        /// <summary>
        /// Identifies the <see cref="SelectedBasemap"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedBasemapProperty =
            DependencyProperty.Register(nameof(SelectedBasemap), typeof(BasemapGalleryItem), typeof(BasemapGallery), new PropertyMetadata(null, SelectedBasemapChanged));
        #endregion Dependency Properties

        /// <summary>
        /// Event raised when a basemap is selected.
        /// </summary>
        public event EventHandler<BasemapGalleryItem>? BasemapSelected;
    }
}
#endif
