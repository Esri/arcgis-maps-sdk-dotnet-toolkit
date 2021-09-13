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
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    internal class BasemapGalleryController
    {
        private IBasemapGallery _gallery;
        private bool _ignoreEventsFlag;

        public BasemapGalleryController(IBasemapGallery gallery)
        {
            _gallery = gallery;
        }

        public void HandleAvailableBasemapsChanged()
        {
            _ignoreEventsFlag = true;

            try
            {
                // Update validity
                _gallery.AvailableBasemaps?.ToList()?.ForEach(bmgi => bmgi.NotifySpatialReferenceChanged(_gallery.GeoModel));

                // Show new items in UI
                _gallery.SetListViewSource(_gallery.AvailableBasemaps);

                // Update selection.
                _ = UpdateSelectionForGeoModelBasemap();
            }
            finally
            {
                _ignoreEventsFlag = false;
            }
        }

        public void HandleAvailableBasemapsCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    e.NewItems?.OfType<BasemapGalleryItem>().ToList().ForEach(bmgi => bmgi.NotifySpatialReferenceChanged(_gallery.GeoModel));
                    _ = UpdateSelectionForGeoModelBasemap();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _ = UpdateSelectionForGeoModelBasemap();
                    break;
            }
        }

        public void HandleGeoModelChanged()
        {
            _gallery.AvailableBasemaps?.ToList().ForEach(item => item.NotifySpatialReferenceChanged(_gallery.GeoModel));
            _ = UpdateSelectionForGeoModelBasemap();
        }

        public void HandleGeoModelPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(GeoModel.Basemap) && !_ignoreEventsFlag)
            {
                _ = UpdateSelectionForGeoModelBasemap();
            }
            else if (propertyName == nameof(GeoModel.SpatialReference))
            {
                _gallery.AvailableBasemaps?.ToList().ForEach(item => item.NotifySpatialReferenceChanged(_gallery.GeoModel));
            }
        }

        public void HandleListViewChanged()
        {
            _gallery.SetListViewSource(_gallery.AvailableBasemaps);
            _gallery.SetListViewSelection(_gallery.SelectedBasemap);
        }

        public void HandleListViewSelectionChanged(BasemapGalleryItem? newSelection)
        {
            if (_ignoreEventsFlag)
            {
                return;
            }

            try
            {
                _ignoreEventsFlag = true;
                _gallery.SelectedBasemap = newSelection;
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                _ignoreEventsFlag = false;
            }
        }

        public async Task HandlePortalChanged()
        {
            _gallery.SetIsLoading(true);
            try
            {
                if (_gallery.Portal is ArcGISPortal portal)
                {
                    if (await PopulateBasemapsForPortal(portal) is List<BasemapGalleryItem> portalItems)
                    {
                        _gallery.AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>(portalItems);
                    }
                }
            }
            finally
            {
                _gallery.SetIsLoading(false);
            }
        }

        public void HandleSelectedBasemapChanged()
        {
            try
            {
                // Stop listening to list events
                _ignoreEventsFlag = true;

                _gallery.SetListViewSelection(_gallery.SelectedBasemap);

                if (_gallery.GeoModel != null && (!_gallery.SelectedBasemap?.EqualsBasemap(_gallery.GeoModel.Basemap) ?? true))
                {
                    _gallery.GeoModel.Basemap = _gallery.SelectedBasemap?.Basemap?.Clone();
                }

                if (_gallery.SelectedBasemap is BasemapGalleryItem selectedItem)
                {
                    _gallery.NotifyBasemapSelected(selectedItem);
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
            _gallery.SetIsLoading(true);
            try
            {
                var portalItems = await PopulateFromDefaultList();
                if (portalItems != null)
                {
                    _gallery.AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>(portalItems);
                }
            }
            finally
            {
                _gallery.SetIsLoading(false);
            }
        }

        public async Task UpdateSelectionForGeoModelBasemap()
        {
            if (_gallery.GeoModel?.Basemap is Basemap inputBasemap)
            {
                if (await BasemapIsActuallyNotABasemap(inputBasemap))
                {
                    _gallery.SelectedBasemap = null;
                }
                else if (_gallery.AvailableBasemaps?.FirstOrDefault(bmgi => bmgi.EqualsBasemap(inputBasemap)) is BasemapGalleryItem selectedItem)
                {
                    _gallery.SelectedBasemap = selectedItem;
                }
                else
                {
                    _gallery.SelectedBasemap = null;
                }
            }
            else
            {
                _gallery.SelectedBasemap = null;
            }
        }

        /// <summary>
        /// Maps and scenes start with an empty basemap that should not be shown in the UI.
        /// </summary>
        private static async Task<bool> BasemapIsActuallyNotABasemap(Basemap input)
        {
            await input.LoadAsync();
            if (!input.BaseLayers.Any() && !input.ReferenceLayers.Any())
            {
                return true;
            }

            return false;
        }

        private static async Task<List<BasemapGalleryItem>?> PopulateBasemapsForPortal(ArcGISPortal? portal)
        {
            if (portal == null)
            {
                return null;
            }

            Task<IEnumerable<Basemap>> getBasemapsTask;
            if (portal.PortalInfo?.UseVectorBasemaps ?? false)
            {
                getBasemapsTask = portal.GetVectorBasemapsAsync();
            }
            else
            {
                getBasemapsTask = portal.GetBasemapsAsync();
            }

            var basemaps = await getBasemapsTask;

            List<BasemapGalleryItem> listOfBasemaps = new List<BasemapGalleryItem>();

            foreach (var item in basemaps)
            {
                listOfBasemaps.Add(new BasemapGalleryItem(item));
            }

            #if !WINDOWS_UWP && !NETCOREAPP && !NETCOREAPP3_1
            await Task.WhenAll(listOfBasemaps.Select(gi => gi.LoadAsync()));
            #else
            foreach (var item in listOfBasemaps)
            {
                try
                {
                    await item.LoadAsync();
                }
                catch (Exception)
                {
                }
            }
            #endif

            return listOfBasemaps;
        }

        private static async Task<List<BasemapGalleryItem>> PopulateFromDefaultList()
        {
            ArcGISPortal defaultPortal = await ArcGISPortal.CreateAsync();

            var results = await defaultPortal.GetDeveloperBasemapsAsync();

            List<BasemapGalleryItem> listOfBasemaps = new List<BasemapGalleryItem>();

            foreach (var basemap in results)
            {
                listOfBasemaps.Add(new BasemapGalleryItem(basemap));
            }

#if !WINDOWS_UWP && !NETCOREAPP
            await Task.WhenAll(listOfBasemaps.Select(gi => gi.LoadAsync()));
#else
            foreach (var item in listOfBasemaps)
            {
                try
                {
                    await item.LoadAsync();
                }
                catch (Exception)
                {
                }
            }
#endif
            return listOfBasemaps;
        }
    }
}
