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
        private IList<BasemapGalleryItem>? _cached2DBasemaps;
        private IList<BasemapGalleryItem>? _cached3DBasemaps;
        private Task<ObservableCollection<BasemapGalleryItem>>? _loadBasemapGalleryItemsTask;

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
                    InvalidateBasemapCache();
                    HandleAvailableBasemapsChanged();
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

                    _ = HandleGeoModelChanged();
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
            try
            {
                // Stop listening to list events
                _ignoreEventsFlag = true;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableBasemaps)));

                // Update validity
                AvailableBasemaps?.ToList()?.ForEach(bmgi => bmgi.NotifySpatialReferenceChanged(GeoModel));

                // Update selection.
                UpdateSelectionForGeoModelBasemap();
            }
            finally
            {
                // restore events
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

        private async Task HandleGeoModelChanged() => await UpdateBasemaps();

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
            InvalidateBasemapCache();
            await UpdateBasemaps();
        }

        private bool _pendingUpdateBasemaps;

        public async Task UpdateBasemaps()
        {
            _pendingUpdateBasemaps = true;
            if (IsLoading)
                return;

            IsLoading = true;
            try
            {
                while (_pendingUpdateBasemaps)
                {
                    _pendingUpdateBasemaps = false;
                    
                    _loadCancellationTokenSource?.Cancel();
                    _loadCancellationTokenSource = new CancellationTokenSource();
                    try
                    {
                        _availableBasemaps = await PopulateBasemaps(_loadCancellationTokenSource.Token);
                        HandleAvailableBasemapsChanged();
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.Message);
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

        private async Task<IList<BasemapGalleryItem>?> PopulateBasemaps(CancellationToken cancellationToken)
        {
            Portal ??= await ArcGISPortal.CreateAsync(cancellationToken);

            return await LoadBasemapGalleryItems(Portal, cancellationToken);
        }

        private async Task<ObservableCollection<BasemapGalleryItem>> LoadBasemapGalleryItems(ArcGISPortal portal, CancellationToken cancellationToken = default)
        {
            async Task<List<BasemapGalleryItem>> LoadBasemapsAsync(Func<CancellationToken, Task<IEnumerable<Basemap>>> getBasemapsFunc)
            {
                var basemaps = await getBasemapsFunc(cancellationToken);
                var basemapItems = basemaps.Select(basemap => new BasemapGalleryItem(basemap)).ToList();
                foreach (var item in basemapItems)
                {
                    _ = item.LoadAsync();
                }
                return basemapItems;
            }
            var basemapGalleryItems = new List<BasemapGalleryItem>();

            if (portal.PortalInfo?.Use3DBasemaps is true && GeoModel is Scene)
            {
                _cached3DBasemaps ??= await LoadBasemapsAsync(portal.Get3DBasemapsAsync);
                basemapGalleryItems.AddRange(_cached3DBasemaps);
            }

            _cached2DBasemaps ??= await LoadBasemapsAsync(portal.PortalInfo?.UseVectorBasemaps ?? false
                    ? portal.GetVectorBasemapsAsync
                    : portal.GetBasemapsAsync);
            basemapGalleryItems.AddRange(_cached2DBasemaps);

            return new ObservableCollection<BasemapGalleryItem>(basemapGalleryItems);
        }

        private void InvalidateBasemapCache()
        {
            // Clear caches when the portal changes
            _cached2DBasemaps = null;
            _cached3DBasemaps = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
