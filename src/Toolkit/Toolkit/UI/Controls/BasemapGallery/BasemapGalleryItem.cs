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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Toolkit.Internal;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Encompasses an element in a basemap gallery.
    /// </summary>
#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    public partial class BasemapGalleryItem : INotifyPropertyChanged, IEquatable<BasemapGalleryItem>
    {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        private byte[]? _thumbnailData;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
        private RuntimeImage? _thumbnailOverride;
        private string? _tooltipOverride;
        private string? _nameOverride;
        private bool _isLoading;
        private bool _isLoadingThumbnail;
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
            await bmgi.LoadAsync().ConfigureAwait(false);
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
                    await Basemap.LoadAsync();
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tooltip)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Is3D)));
                _ = LoadImage();
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

        private async Task LoadImage()
        {
            IsLoadingThumbnail = true;
            try
            {
                await (Thumbnail?.LoadAsync() ?? Task.CompletedTask);

                if (Thumbnail?.LoadStatus == LoadStatus.Loaded)
                {
                    using var stream = await Thumbnail.GetEncodedBufferAsync();
                    var buffer = new byte[stream.Length];
#if NET8_0_OR_GREATER
                    await stream.ReadExactlyAsync(buffer);
#else
                    await stream.ReadAsync(buffer, 0, (int)stream.Length);
#endif
                    ThumbnailData = buffer;
#if WINDOWS_XAML
                    ThumbnailBitmap = await Thumbnail.ToImageSourceAsync();
#endif
                }
                _hasLoaded = true;
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                IsLoadingThumbnail = false;
            }
        }

        internal async void NotifySpatialReferenceChanged(GeoModel? gm)
        {
            try
            {
                // Scenes return a spatial reference of 4326 even when the basemap is 3857
                _lastNotifiedSpatialReference = gm is Scene scene && scene.SceneViewTilingScheme == SceneViewTilingScheme.WebMercator
                    ? SpatialReferences.WebMercator
                    : gm?.SpatialReference;

                // Load the SR.
                if (Basemap is Basemap inputBasemap && SpatialReference == null)
                {
                    var clone = inputBasemap.Clone();
                    var map = new Mapping.Map(clone);
                    await map.LoadAsync();
                    if (map.LoadStatus == LoadStatus.Loaded)
                    {
                        SpatialReference = map.SpatialReference;
                    }
                }

                if (_lastNotifiedSpatialReference == null || _lastNotifiedSpatialReference == SpatialReference || SpatialReference == null)
                {
                    IsValid = true;
                }
                else
                {
                    IsValid = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ArcGIS Toolkit - Error loading basemap item:{ex}");
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

        /// <summary>
        /// Gets thumbnail as a byte array.
        /// </summary>
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        public byte[]? ThumbnailData
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
        {
            get => _thumbnailData;
            private set
            {
                if (value != _thumbnailData)
                {
                    _thumbnailData = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThumbnailData)));
                }
            }
        }

#if WINDOWS_XAML
        private ImageSource? _thumbnailBitmap;

        /// <summary>
        /// Gets the thumbnail as an ImageSource for convenient use from UWP.
        /// </summary>
        public ImageSource? ThumbnailBitmap
        {
            get => _thumbnailBitmap;
            private set
            {
                if (value != _thumbnailBitmap)
                {
                    _thumbnailBitmap = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThumbnailBitmap)));
                }
            }
        }
#endif

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
        internal bool EqualsBasemap(Basemap? other)
        {
            if (other == null)
            {
                return false;
            }

            return other == Basemap || (other.Item?.ItemId != null && other.Item?.ItemId == Basemap?.Item?.ItemId)
                                    || (other.Uri != null && other.Uri == Basemap?.Uri)
                                    || AreBasemapsEqualByLayers(Basemap, other);
        }

        private static bool AreBasemapsEqualByLayers(Basemap? basemap1, Basemap? basemap2)
        {
            if (basemap1 == null || basemap2 == null) return false;

            return LayersEqual(basemap1.BaseLayers, basemap2.BaseLayers)
                && LayersEqual(basemap1.ReferenceLayers, basemap2.ReferenceLayers);
        }

        private static bool LayersEqual(LayerCollection layers1, LayerCollection layers2)
        {
            return layers1.Count == layers2.Count
                && layers1.Zip(layers2, LayerEquals).All(equal => equal);
        }

        private static bool LayerEquals(Layer layer1, Layer layer2)
        {
            // This method can be extended to handle more specific layer comparisons if needed
            if (layer1.GetType() != layer2.GetType()) return false;

            return layer1 switch
            {
                AnnotationLayer annotationLayer => annotationLayer.Source == ((AnnotationLayer)layer2).Source,
                ArcGISMapImageLayer arcGISMapImageLayer => arcGISMapImageLayer.Source == ((ArcGISMapImageLayer)layer2).Source,
                ArcGISSceneLayer sceneLayer => sceneLayer.Source == ((ArcGISSceneLayer)layer2).Source,
                ArcGISTiledLayer tiledLayer => tiledLayer.Source == ((ArcGISTiledLayer)layer2).Source,
                ArcGISVectorTiledLayer vectorTiledLayer => vectorTiledLayer.Source == ((ArcGISVectorTiledLayer)layer2).Source,
#pragma warning disable CS0618 // Type or member is obsolete
                BingMapsLayer imageServiceLayer => imageServiceLayer.Portal?.Uri == ((BingMapsLayer)layer2).Portal?.Uri,
#pragma warning restore CS0618 // Type or member is obsolete
                DimensionLayer dimensionLayer => dimensionLayer.Source == ((DimensionLayer)layer2).Source,
                FeatureCollectionLayer featureCollectionLayer => featureCollectionLayer.FeatureCollection == ((FeatureCollectionLayer)layer2).FeatureCollection,
                GroupLayer groupLayer => groupLayer.Layers.Count == ((GroupLayer)layer2).Layers.Count && LayersEqual(groupLayer.Layers, ((GroupLayer)layer2).Layers),
                IntegratedMeshLayer integratedMeshLayer => integratedMeshLayer.Source == ((IntegratedMeshLayer)layer2).Source,
                KmlLayer kmlLayer => kmlLayer.Dataset?.Source == ((KmlLayer)layer2).Dataset?.Source,
                Ogc3DTilesLayer ogc3DTilesLayer => ogc3DTilesLayer.Source == ((Ogc3DTilesLayer)layer2).Source,
                OpenStreetMapLayer openStreetMapLayer => true, // OpenStreetMap layers are considered equal if types match
                PointCloudLayer pointCloudLayer => pointCloudLayer.Source == ((PointCloudLayer)layer2).Source,
                WebTiledLayer webTiledLayer => webTiledLayer.TemplateUri == ((WebTiledLayer)layer2).TemplateUri,
                ServiceImageTiledLayer serviceImageTiledLayer => serviceImageTiledLayer.TileInfo == ((ServiceImageTiledLayer)layer2).TileInfo && serviceImageTiledLayer.FullExtent == ((ServiceImageTiledLayer)layer2).FullExtent,
                SubtypeFeatureLayer subtypeFeatureLayer => subtypeFeatureLayer.FeatureTable == ((SubtypeFeatureLayer)layer2).FeatureTable,
                WmsLayer wmsLayer => wmsLayer.Source == ((WmsLayer)layer2).Source,
                WmtsLayer wmtsLayer => wmtsLayer.Source == ((WmtsLayer)layer2).Source,
                _ => false,
            };
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
        /// Gets or sets the tooltip to display for this basemap item.
        /// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type from Markdig
        [AllowNull]
#pragma warning restore CS0436 // Type conflicts with imported type from Markdig
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
                return _nameOverride ?? (string.IsNullOrEmpty(Basemap.Name) ? (Basemap.Item?.Title ?? Basemap.Item?.Name ?? string.Empty) : Basemap.Name);
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
        /// Gets or sets a value indicating whether this gallery item is actively loading the basemap.
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

        /// <summary>
        /// Gets or sets a value indicating whether this gallery item is actively loading the <see cref="Thumbnail"/>.
        /// </summary>
        public bool IsLoadingThumbnail
        {
            get => _isLoadingThumbnail;
            set
            {
                if (_isLoadingThumbnail != value)
                {
                    _isLoadingThumbnail = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoadingThumbnail)));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this basemap is a 3D basemap.
        /// </summary>
        public bool Is3D => this.Basemap?.BaseLayers?.OfType<ArcGISSceneLayer>()?.Any() ?? false;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
