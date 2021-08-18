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
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

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
        private CancellationTokenSource? _cancelSource;
        private BasemapGalleryController _controller = new BasemapGalleryController();

        public BasemapGallery()
        {
            ListItemTemplate = DefaultListDataTemplate;
            GridItemTemplate = DefaultGridDataTemplate;
            ControlTemplate = DefaultControlTemplate;
            _controller.LoadFromDefaultPortal(this);
        }


        private CollectionView? ListView
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
                        _controller.HandleListViewChanged(this);
                        _listView.SelectionChanged += ListViewSelectionChanged;
                        HandleTemplateChange(Width);
                    }
                }
            }
        }

        private static void SelectedBasemapChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is BasemapGallery gallery)
            {
                gallery._controller.HandleSelectedBasemapChanged(gallery);
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

                gallery._controller.HandleGeoModelChanged(gallery);

                if (newValue is GeoModel newModel)
                {
                    newModel.PropertyChanged += gallery.GeoModelPropertyChanged;
                }
            }
        }

        private void GeoModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _controller.HandleGeoModelPropertyChanged(this, e.PropertyName);
        }

        private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListView == null)
            {
                return;
            }

            if (e.CurrentSelection.Count == 0)
            {
                _controller.HandleListViewSelectionChanged(this, -1);
            }
            else
            {
                //TODO = decide if this really has to be done by index
                _controller.HandleListViewSelectionChanged(this, AvailableBasemaps.IndexOf((BasemapGalleryItem)e.CurrentSelection.First()));
            }
        }

        /// <summary>
        /// Handles property changes for the <see cref="Portal"/> bindable property.
        /// </summary>
        private static void PortalChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is BasemapGallery gallery)
            {
                _ = gallery._controller.HandlePortalChanged(gallery);
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

                gallery._controller.HandleAvailableBasemapsChanged(gallery);

                if (newValue is ObservableCollection<BasemapGalleryItem> newAvailableBasemaps)
                {
                    newAvailableBasemaps.CollectionChanged += gallery.AvailableBasemapsCollectionChanged;
                }
            }
        }

        private void AvailableBasemapsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _controller.HandleAvailableBasemapsCollectionChanged(this, e);
        }

        /// <summary>
        /// Event raised when a basemap is selected.
        /// </summary>
        public event EventHandler<BasemapGalleryItem>? BasemapSelected;

        #region Convenience Properties

        /// <summary>
        /// Gets or sets the portal used to populate the basemap list.
        /// </summary>
        public ArcGISPortal Portal
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

        internal void SetListViewSource(IList<BasemapGalleryItem> newSource)
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
                #if WINDOWS_UWP
                ListView.SelectedItems.Clear();
                ListView.SelectedItem = null;
                #endif
                ListView.SelectedItem = item;
            }
        }

        internal void NotifyBasemapSelected(BasemapGalleryItem item)
        {
            BasemapSelected?.Invoke(this, item);
        }

        #endregion Controller Callbacks
    }
}
