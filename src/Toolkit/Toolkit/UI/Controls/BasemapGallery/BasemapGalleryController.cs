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
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    internal partial class BasemapGalleryController : INotifyPropertyChanged
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private ArcGISPortal? _portal;
        private bool _ignoreEventsFlag;
        private IList<BasemapGalleryItem>? _availableBasemaps;
        private GeoModel? _geoModel;
        private BasemapGalleryItem? _selectedBasemap;
        private bool _isLoading;
        private CancellationTokenSource? _loadCancellationTokenSource;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value != _isLoading)
                {
                    _isLoading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
                }
            }
        }

        public IList<BasemapGalleryItem>? AvailableBasemaps
        {
            get => _availableBasemaps;
            set
            {
                if (value != _availableBasemaps)
                {
                    if (_availableBasemaps is INotifyCollectionChanged oldIncc)
                    {
                        oldIncc.CollectionChanged -= HandleAvailableBasemapsCollectionChanged;
                    }

                    _availableBasemaps = value;

                    if (_availableBasemaps is INotifyCollectionChanged newIncc)
                    {
                        newIncc.CollectionChanged += HandleAvailableBasemapsCollectionChanged;
                    }

                    HandleAvailableBasemapsChanged();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableBasemaps)));
                    _loadCancellationTokenSource?.Cancel();
                }
            }
        }

        public BasemapGalleryItem? SelectedBasemap
        {
            get => _selectedBasemap;
            set
            {
                if (_selectedBasemap != value)
                {
                    _selectedBasemap = value;
                    HandleSelectedBasemapChanged();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBasemap)));
                }
            }
        }

        public GeoModel? GeoModel
        {
            get => _geoModel;
            set
            {
                if (value != _geoModel)
                {
                    if (_geoModel is INotifyPropertyChanged oldGeoModel)
                    {
                        oldGeoModel.PropertyChanged -= HandleGeoModelPropertyChanged;
                    }

                    _geoModel = value;

                    if (_geoModel is INotifyPropertyChanged newGeoModel)
                    {
                        newGeoModel.PropertyChanged += HandleGeoModelPropertyChanged;
                    }

                    HandleGeoModelChanged();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoModel)));
                }
            }
        }

        public ArcGISPortal? Portal
        {
            get => _portal;
            set
            {
                if (value != _portal)
                {
                    _portal = value;
                    _ = HandlePortalChanged();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Portal)));
                }
            }
        }

        private void HandleAvailableBasemapsChanged()
        {
            _ignoreEventsFlag = true;

            try
            {
                // Update validity
                AvailableBasemaps?.ToList()?.ForEach(bmgi => bmgi.NotifySpatialReferenceChanged(GeoModel));

                // Update selection.
                UpdateSelectionForGeoModelBasemap();
            }
            finally
            {
                _ignoreEventsFlag = false;
            }
        }

        private void HandleAvailableBasemapsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    e.NewItems?.OfType<BasemapGalleryItem>().ToList().ForEach(bmgi => bmgi.NotifySpatialReferenceChanged(GeoModel));
                    UpdateSelectionForGeoModelBasemap();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UpdateSelectionForGeoModelBasemap();
                    break;
            }
        }

        private void HandleGeoModelChanged()
        {
            AvailableBasemaps?.ToList().ForEach(item => item.NotifySpatialReferenceChanged(GeoModel));
            UpdateSelectionForGeoModelBasemap();
        }

        private void HandleGeoModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeoModel.Basemap) && !_ignoreEventsFlag)
            {
                UpdateSelectionForGeoModelBasemap();
            }
            else if (e.PropertyName == nameof(GeoModel.SpatialReference))
            {
                AvailableBasemaps?.ToList().ForEach(item => item.NotifySpatialReferenceChanged(GeoModel));
            }
        }

        private async Task HandlePortalChanged()
        {
            IsLoading = true;
            try
            {
                if (Portal is ArcGISPortal portal)
                {
                    if (await PopulateBasemapsForPortal(portal) is List<BasemapGalleryItem> portalItems)
                    {
                        AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>(portalItems);
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void HandleSelectedBasemapChanged()
        {
            try
            {
                // Stop listening to list events
                _ignoreEventsFlag = true;

                if (GeoModel != null && (!SelectedBasemap?.EqualsBasemap(GeoModel.Basemap) ?? true))
                {
                    GeoModel.Basemap = SelectedBasemap?.Basemap?.Clone();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                // restore events
                _ignoreEventsFlag = false;
            }
        }

        public async Task LoadFromDefaultPortal()
        {
            IsLoading = true;
            _loadCancellationTokenSource = new CancellationTokenSource();
            try
            {
                AvailableBasemaps = await PopulateFromDefaultList(_loadCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateSelectionForGeoModelBasemap()
        {
            if (GeoModel?.Basemap is Basemap inputBasemap)
            {
                if (BasemapIsActuallyNotABasemap(inputBasemap))
                {
                    SelectedBasemap = null;
                }
                else if (AvailableBasemaps?.FirstOrDefault(bmgi => bmgi.EqualsBasemap(inputBasemap)) is BasemapGalleryItem selectedItem)
                {
                    SelectedBasemap = selectedItem;
                }
                else
                {
                    SelectedBasemap = null;
                }
            }
            else
            {
                SelectedBasemap = null;
            }
        }

        /// <summary>
        /// Maps and scenes start with an empty basemap that should not be shown in the UI.
        /// </summary>
        private static bool BasemapIsActuallyNotABasemap(Basemap input)
        {
            if (!input.BaseLayers.Any() && !input.ReferenceLayers.Any())
            {
                return true;
            }

            return false;
        }

        private static async Task<IList<BasemapGalleryItem>?> PopulateBasemapsForPortal(ArcGISPortal? portal)
        {
            if (portal == null)
            {
                return null;
            }

            var basemaps = portal.PortalInfo?.UseVectorBasemaps ?? false
                                        ? await portal.GetVectorBasemapsAsync()
                                        : await portal.GetBasemapsAsync();

            var basemapGalleryItems = basemaps.Select(basemap => new BasemapGalleryItem(basemap, BasemapGalleryItemType._2D)).ToList();

            if (portal.PortalInfo?.Use3DBasemaps ?? false)
            {
                var basemaps3D = await portal.Get3DBasemapsAsync();
                basemapGalleryItems = basemaps3D
                                        .Select(basemap => new BasemapGalleryItem(basemap, BasemapGalleryItemType._3D))
                                        .Concat(basemapGalleryItems)
                                        .ToList();
            }

            Parallel.ForEach(basemapGalleryItems, async item =>
            {
                await item.LoadAsync();
            });

            return basemapGalleryItems;
        }

        private static async Task<IList<BasemapGalleryItem>> PopulateFromDefaultList(CancellationToken cancellationToken = default)
        {
            ArcGISPortal defaultPortal = await ArcGISPortal.CreateAsync(cancellationToken);

            var results = await defaultPortal.GetDeveloperBasemapsAsync(cancellationToken);

            var basemapGalleryItems = results.Select(basemap => new BasemapGalleryItem(basemap, BasemapGalleryItemType._2D)).ToList();

            if (defaultPortal.PortalInfo?.Use3DBasemaps ?? false)
            {
                var basemaps3D = await defaultPortal.Get3DBasemapsAsync(cancellationToken);
                basemapGalleryItems = basemaps3D
                                        .Select(basemap => new BasemapGalleryItem(basemap, BasemapGalleryItemType._3D))
                                        .Concat(basemapGalleryItems)
                                        .ToList();
            }

            Parallel.ForEach(basemapGalleryItems, async item =>
            {
                await item.LoadAsync();
            });

            return new ObservableCollection<BasemapGalleryItem>(basemapGalleryItems);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
