﻿// /*******************************************************************************
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
#if WINDOWS || XAMARIN_FORMS
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;

#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using ImageSource = Xamarin.Forms.ImageSource;
using RuntimeImage = Esri.ArcGISRuntime.UI.RuntimeImage;
#elif NETFX_CORE
using Windows.UI.Xaml.Media;
using ImageSource = Windows.UI.Xaml.Media.ImageSource;
using Esri.ArcGISRuntime.UI;
#elif NETFRAMEWORK || NETCOREAPP
using System.Windows.Media;
using ImageSource = System.Windows.Media.ImageSource;
using Esri.ArcGISRuntime.UI;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Encompasses an element in a basemap gallery.
    /// </summary>
    public class BasemapGalleryItem : INotifyPropertyChanged, IEquatable<BasemapGalleryItem>
    {
        private RuntimeImage? _thumbnailOverride;
        private ImageSource? _thumbnailImageSource;
        private string? _tooltipOverride;
        private string? _nameOverride;
        private bool _isLoading;
        private bool _isValid = true;
        private bool _hasLoaded;
        private SpatialReference? _spatialReference;
        private SpatialReference? _lastNotifiedSpatialReference;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGalleryItem"/> class.
        /// </summary>
        /// <param name="basemap">Basemap for this gallery item. Must be not null and loaded.</param>
        internal BasemapGalleryItem(Basemap basemap)
        {
            Basemap = basemap;
        }

        /// <summary>
        /// Creates an item to represent the given basemap.
        /// </summary>
        public static async Task<BasemapGalleryItem> CreateAsync(Basemap basemap)
        {
            var bmgi = new BasemapGalleryItem(basemap);
            await bmgi.LoadAsync();
            return bmgi;
        }

        /// <summary>
        /// Loads the basemap and other properties.
        /// </summary>
        internal async Task LoadAsync()
        {
            if (_hasLoaded)
            {
                return;
            }

            IsLoading = true;
            try
            {
                if (Basemap != null && Basemap.LoadStatus != LoadStatus.Loaded)
                {
                    await Basemap.RetryLoadAsync();
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tooltip)));
                await LoadImage();

                // Load the SR.
                if (Basemap != null)
                {
                    var clone = Basemap.Clone();
                    var map = new Map(clone);
                    await map.LoadAsync();
                    if (map.LoadStatus == LoadStatus.Loaded)
                    {
                        SpatialReference = map.SpatialReference;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                IsLoading = false;
                _hasLoaded = true;
            }
        }

        private async Task LoadImage()
        {
            IsLoading = true;
            try
            {
                if (Thumbnail != null && Thumbnail.LoadStatus != LoadStatus.Loaded)
                {
                    await Thumbnail.RetryLoadAsync();
                }

                if (Thumbnail != null)
                {
                    ThumbnailImageSource = await Thumbnail.ToImageSourceAsync();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                IsLoading = false;
            }
        }

        internal void NotifySpatialReferenceChanged(GeoModel? gm)
        {
            // Scenes return a spatial reference of 4326 even when the basemap is 3857
            _lastNotifiedSpatialReference = gm is Scene scene && scene.SceneViewTilingScheme == SceneViewTilingScheme.WebMercator
                ? SpatialReferences.WebMercator
                : gm?.SpatialReference;

            if (_lastNotifiedSpatialReference == null || _lastNotifiedSpatialReference == SpatialReference)
            {
                IsValid = true;
            }
            else
            {
                IsValid = false;
            }
        }

        /// <summary>
        /// Gets the spatial reference, if known, of the underlying basemap.
        /// </summary>
        public SpatialReference? SpatialReference
        {
            get => _spatialReference;
            private set
            {
                if (value != _spatialReference)
                {
                    _spatialReference = value;
                    if (_lastNotifiedSpatialReference != null && _spatialReference != null && _spatialReference != _lastNotifiedSpatialReference)
                    {
                        IsValid = false;
                    }
                    else
                    {
                        IsValid = true;
                    }

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpatialReference)));
                }
            }
        }

        /// <inheritdoc />
        public bool Equals(BasemapGalleryItem? other)
        {
            if (other == null)
            {
                return false;
            }

            return EqualsBasemap(other.Basemap);
        }

        /// <summary>
        /// Returns true if the other basemap is equal to this item's underlying basemap.
        /// </summary>
        public bool EqualsBasemap(Basemap? other)
        {
            if (other == null)
            {
                return false;
            }

            return other == Basemap || other.Name == Basemap?.Name
                                    || (other.Item?.ItemId != null && other.Item?.ItemId == Basemap?.Item?.ItemId)
                                    || other.Uri == Basemap?.Uri;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is BasemapGalleryItem other)
            {
                return Equals(other);
            }

            return base.Equals(obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Basemap?.Name?.GetHashCode() ?? -1;
        }

        /// <summary>
        /// Gets a value indicating whether this basemap is a valid selection.
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            private set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
                }
            }
        }

        #if XAMARIN_FORMS
        private bool _isSelected;

        /// <summary>
        /// Gets or sets a value indicating whether to show item as selected. Selection property used to assist manual list selection implementation for Xamarin.Forms.
        /// </summary>
        /// <remarks>
        /// Native listview selection can't be used because of many outstanding Xamarin.Forms,
        /// specifically affecting UWP: e.g. https://github.com/xamarin/Xamarin.Forms/issues/11405, https://github.com/xamarin/Xamarin.Forms/issues/12491.
        /// </remarks>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
        #endif

        /// <summary>
        /// Gets the basemap associated with this basemap item.
        /// </summary>
        public Basemap Basemap { get; private set; }

        /// <summary>
        /// Gets or sets the thumbnail to display for this basemap item.
        /// </summary>
        public RuntimeImage? Thumbnail
        {
            get
            {
                if (_thumbnailOverride != null)
                {
                    return _thumbnailOverride;
                }

                return Basemap.Item?.Thumbnail;
            }

            set
            {
                if (_thumbnailOverride != value)
                {
                    _thumbnailOverride = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail)));
                    _ = LoadImage();
                }
            }
        }

        /// <summary>
        /// Gets the thumbnail in a format that is easily displayable in a view.
        /// </summary>
        public ImageSource? ThumbnailImageSource
        {
            get => _thumbnailImageSource;
            private set
            {
                _thumbnailImageSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThumbnailImageSource)));
            }
        }

        /// <summary>
        /// Gets or sets the tooltip to display for this basemap item.
        /// </summary>
        public string Tooltip
        {
            get
            {
                if (_tooltipOverride != null)
                {
                    return _tooltipOverride;
                }

                return Basemap.Item?.Description.ToPlainText() ?? Name;
            }

            set
            {
                if (_tooltipOverride != value)
                {
                    _tooltipOverride = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tooltip)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the name to display for this basemap item.
        /// </summary>
        public string Name
        {
            get
            {
                return _nameOverride ?? Basemap.Name;
            }

            set
            {
                if (_nameOverride != value)
                {
                    _nameOverride = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this gallery item is actively loading the basemap or image.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
                }
            }
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
#endif
