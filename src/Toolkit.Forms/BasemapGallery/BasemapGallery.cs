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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// Displays a collection of images representing basemaps from ArcGIS Online, a user-defined Portal, or a user-defined collection.
    /// </summary>
    /// <remarks>
    /// If connected to a GeoView, changing the basemap selection will change the connected Map or Scene's basemap.
    /// Only basemaps whose spatial reference matches the map or scene's spatial reference can be selected for display.
    /// </remarks>
    public partial class BasemapGallery
    {
        private CollectionView? _listView;
        private BasemapGalleryController _controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGallery"/> class.
        /// </summary>
        public BasemapGallery()
        {
            _controller = new BasemapGalleryController(this);
            ListItemTemplate = DefaultListDataTemplate;
            GridItemTemplate = DefaultGridDataTemplate;
            ControlTemplate = DefaultControlTemplate;
            _ = _controller.LoadFromDefaultPortal();
        }

        private CollectionView? ListView
        {
            get => _listView;
            set
            {
                if (value != _listView)
                {
                    _listView = value;

                    if (_listView != null)
                    {
                        _controller.HandleListViewChanged();
                        HandleTemplateChange(Width);
                    }
                }
            }
        }

        private static void SelectedBasemapChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (oldValue is BasemapGalleryItem oldItem)
            {
                oldItem.IsSelected = false;
            }

            if (sender is BasemapGallery gallery)
            {
                gallery._controller.HandleSelectedBasemapChanged();
            }

            if (newValue is BasemapGalleryItem newItem)
            {
                newItem.IsSelected = true;
            }
        }

        /// <summary>
        /// Handles property changes for the <see cref="GeoModel" /> bindable property.
        /// </summary>
        private static void GeoModelChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is BasemapGallery gallery)
            {
                if (oldValue is GeoModel oldModel)
                {
                    oldModel.PropertyChanged -= gallery.GeoModelPropertyChanged;
                }

                gallery._controller.HandleGeoModelChanged();

                if (newValue is GeoModel newModel)
                {
                    newModel.PropertyChanged += gallery.GeoModelPropertyChanged;
                }
            }
        }

        private void GeoModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _controller.HandleGeoModelPropertyChanged(e.PropertyName);
        }

        private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListView == null)
            {
                return;
            }

            if (e.CurrentSelection.Count == 0)
            {
                _controller.HandleListViewSelectionChanged(null);
            }
            else if (e.CurrentSelection.FirstOrDefault() is BasemapGalleryItem selectedItem)
            {
                if (selectedItem.IsValid)
                {
                    _controller.HandleListViewSelectionChanged(e.CurrentSelection.FirstOrDefault() as BasemapGalleryItem);
                }
            }
        }

        /// <summary>
        /// Handles property changes for the <see cref="Portal"/> bindable property.
        /// </summary>
        private static void PortalChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is BasemapGallery gallery)
            {
                _ = gallery._controller.HandlePortalChanged();
            }
        }

        private static void AvailableBasemapsChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is BasemapGallery gallery)
            {
                if (oldValue is ObservableCollection<BasemapGalleryItem> oldAvailableBasemaps)
                {
                    oldAvailableBasemaps.CollectionChanged -= gallery.AvailableBasemapsCollectionChanged;
                }

                gallery._controller.HandleAvailableBasemapsChanged();

                if (newValue is ObservableCollection<BasemapGalleryItem> newAvailableBasemaps)
                {
                    newAvailableBasemaps.CollectionChanged += gallery.AvailableBasemapsCollectionChanged;
                }
            }
        }

        private void AvailableBasemapsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _controller.HandleAvailableBasemapsCollectionChanged(e);
        }

        /// <summary>
        /// Event raised when a basemap is selected.
        /// </summary>
        public event EventHandler<BasemapGalleryItem>? BasemapSelected;

        #region Convenience Properties

        /// <summary>
        /// Gets or sets the portal used to populate the basemap list.
        /// </summary>
        public ArcGISPortal? Portal
        {
            get => (ArcGISPortal)GetValue(PortalProperty);
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
        public BasemapGalleryItem? SelectedBasemap
        {
            get => GetValue(SelectedBasemapProperty) as BasemapGalleryItem;
            set => SetValue(SelectedBasemapProperty, value);
        }

        #endregion ConvenienceProperties

        #region Bindable Properties

        /// <summary>
        /// Identifies the <see cref="Portal"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PortalProperty =
            BindableProperty.Create(nameof(Portal), typeof(ArcGISPortal), typeof(BasemapGallery), null, propertyChanged: PortalChanged);

        /// <summary>
        /// Identifies the <see cref="GeoModel"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoModelProperty =
            BindableProperty.Create(nameof(GeoModel), typeof(GeoModel), typeof(BasemapGallery), null, BindingMode.OneWay, null, propertyChanged: GeoModelChanged);

        /// <summary>
        /// Identifies the <see cref="AvailableBasemaps"/> bindable property.
        /// </summary>
        public static readonly BindableProperty AvailableBasemapsProperty =
            BindableProperty.Create(nameof(AvailableBasemaps), typeof(IList<BasemapGalleryItem>), typeof(BasemapGallery), null, BindingMode.OneWay, propertyChanged: AvailableBasemapsChanged);

        /// <summary>
        /// Identifies the <see cref="SelectedBasemap"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SelectedBasemapProperty =
            BindableProperty.Create(nameof(SelectedBasemap), typeof(BasemapGalleryItem), typeof(BasemapGallery), null, BindingMode.OneWay, propertyChanged: SelectedBasemapChanged);

        #endregion Bindable Properties

        #region Controller Callbacks

        internal void SetListViewSource(IList<BasemapGalleryItem>? newSource)
        {
            if (ListView != null)
            {
                ListView.ItemsSource = newSource;
            }
        }

        internal void SetListViewSelection(BasemapGalleryItem? item)
        {
            if (ListView != null)
            {
                ListView.SelectedItem = item;
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
        }

        internal void NotifyBasemapSelected(BasemapGalleryItem item)
        {
            item.IsSelected = true;
            BasemapSelected?.Invoke(this, item);
        }

        internal void SetIsLoading(bool isLoading)
        {
            if (isLoading && _loadingScrim != null)
            {
                _loadingScrim.IsVisible = true;
            }
            else if (_loadingScrim != null)
            {
                _loadingScrim.IsVisible = false;
            }
        }
        #endregion Controller Callbacks
    }
}
