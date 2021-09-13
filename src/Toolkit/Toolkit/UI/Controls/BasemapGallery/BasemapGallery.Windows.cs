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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
    public partial class BasemapGallery : Control, IBasemapGallery
    {
        private readonly BasemapGalleryController _controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGallery"/> class.
        /// </summary>
        public BasemapGallery()
        {
            _controller = new BasemapGalleryController(this);
            DefaultStyleKey = typeof(BasemapGallery);
            SizeChanged += BasemapGallerySizeChanged;
            AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>();
            _ = _controller.LoadFromDefaultPortal();
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
                        _controller.HandleListViewChanged();
                        _listView.SelectionChanged += ListViewSelectionChanged;
                    }
                }
            }
        }

        private static void SelectedBasemapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                gallery._controller.HandleSelectedBasemapChanged();
            }
        }

        private static void GeoModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                if (e.OldValue is GeoModel oldModel)
                {
                    oldModel.PropertyChanged -= gallery.GeoModelPropertyChanged;
                }

                gallery._controller.HandleGeoModelChanged();

                if (e.NewValue is GeoModel newModel)
                {
                    newModel.PropertyChanged += gallery.GeoModelPropertyChanged;
                }
            }
        }

        private void GeoModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _controller.HandleGeoModelPropertyChanged(e.PropertyName ?? string.Empty);
        }

        private void ListViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ListView == null)
            {
                return;
            }

            _controller.HandleListViewSelectionChanged(ListView.SelectedItem as BasemapGalleryItem);
        }

        private static void PortalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                _ = gallery._controller.HandlePortalChanged();
            }
        }

        private static void AvailableBasemapsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BasemapGallery gallery)
            {
                if (e.OldValue is ObservableCollection<BasemapGalleryItem> oldAvailableBasemaps)
                {
                    oldAvailableBasemaps.CollectionChanged -= gallery.AvailableBasemapsCollectionChanged;
                }

                gallery._controller.HandleAvailableBasemapsChanged();

                if (e.NewValue is ObservableCollection<BasemapGalleryItem> newAvailableBasemaps)
                {
                    newAvailableBasemaps.CollectionChanged += gallery.AvailableBasemapsCollectionChanged;
                }
            }
        }

        private void AvailableBasemapsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _controller.HandleAvailableBasemapsCollectionChanged(e);
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

        #region Controller Callbacks
        void IBasemapGallery.SetListViewSource(IList<BasemapGalleryItem>? newSource)
        {
            if (ListView != null)
            {
                ListView.ItemsSource = newSource;
            }
        }

        void IBasemapGallery.SetListViewSelection(BasemapGalleryItem? item)
        {
            if (ListView != null)
            {
                ListView.SelectedItem = item;
            }
        }

        void IBasemapGallery.NotifyBasemapSelected(BasemapGalleryItem item)
        {
            BasemapSelected?.Invoke(this, item);
        }

        void IBasemapGallery.SetIsLoading(bool isLoading)
        {
            if (isLoading && _loadingScrim != null)
            {
                _loadingScrim.Visibility = Visibility.Visible;
            }
            else if (_loadingScrim != null)
            {
                _loadingScrim.Visibility = Visibility.Collapsed;
            }
        }
        #endregion Controller Callbacks
    }
}
#endif
