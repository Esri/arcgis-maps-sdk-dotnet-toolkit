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
#if WINDOWS || XAMARIN_FORMS
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    internal class BasemapGalleryController
    {
        private bool _ignoreEventsFlag;

        public void HandleSelectedBasemapChanged(BasemapGallery gallery)
        {
            try
            {
                // Stop listening to list events
                _ignoreEventsFlag = true;

                gallery.SetListViewSelection(gallery.SelectedBasemap);

                if (gallery.GeoModel != null && (!gallery.SelectedBasemap?.EqualsBasemap(gallery.GeoModel.Basemap) ?? true))
                {
                    gallery.GeoModel.Basemap = gallery.SelectedBasemap?.Basemap?.Clone();
                }

                if (gallery.SelectedBasemap is BasemapGalleryItem selectedItem)
                {
                    gallery.NotifyBasemapSelected(selectedItem);
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

        public void HandleGeoModelChanged(BasemapGallery gallery)
        {
            gallery.AvailableBasemaps?.ToList().ForEach(item => item.NotifySpatialReferenceChanged(gallery.GeoModel));
            _ = UpdateSelectionForGeoModelBasemap(gallery);
        }

        public async Task UpdateSelectionForGeoModelBasemap(BasemapGallery gallery)
        {
            if (gallery.GeoModel?.Basemap is Basemap inputBasemap)
            {
                if (await BasemapIsActuallyNotABasemap(inputBasemap))
                {
                    gallery.SelectedBasemap = null;
                }
                else if (gallery.AvailableBasemaps.FirstOrDefault(bmgi => bmgi.EqualsBasemap(inputBasemap)) is BasemapGalleryItem selectedItem)
                {
                    gallery.SelectedBasemap = selectedItem;
                }
                else
                {
                    gallery.SelectedBasemap = null;
                }
            }
            else
            {
                gallery.SelectedBasemap = null;
            }
        }

        public void HandleGeoModelPropertyChanged(BasemapGallery gallery, string propertyName)
        {
            if (propertyName == nameof(GeoModel.Basemap) && !_ignoreEventsFlag)
            {
                _ = UpdateSelectionForGeoModelBasemap(gallery);
            }
            else if (propertyName == nameof(GeoModel.SpatialReference))
            {
                gallery.AvailableBasemaps?.ToList().ForEach(item => item.NotifySpatialReferenceChanged(gallery.GeoModel));
            }
        }

        public void HandleListViewChanged(BasemapGallery gallery)
        {
            gallery.SetListViewSource(gallery.AvailableBasemaps);
            gallery.SetListViewSelection(gallery.SelectedBasemap);
        }

        public void HandleListViewSelectionChanged(BasemapGallery gallery, int selectedIndex)
        {
            if (_ignoreEventsFlag)
            {
                return;
            }

            try
            {
                _ignoreEventsFlag = true;
                if (selectedIndex < 0)
                {
                    gallery.SelectedBasemap = null;
                }
                else
                {
                    gallery.SelectedBasemap = gallery.AvailableBasemaps[selectedIndex];
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                _ignoreEventsFlag = false;
            }
        }

        public void HandleAvailableBasemapsChanged(BasemapGallery gallery)
        {
            _ignoreEventsFlag = true;

            try
            {
                // Update validity
                gallery.AvailableBasemaps?.ToList()?.ForEach(bmgi => bmgi.NotifySpatialReferenceChanged(gallery.GeoModel));

                // Show new items in UI
                gallery.SetListViewSource(gallery.AvailableBasemaps);

                // Update selection.
                _ = UpdateSelectionForGeoModelBasemap(gallery);
            }
            catch (Exception)
            {
            }
            finally
            {
                _ignoreEventsFlag = false;
            }
        }

        public async Task HandlePortalChanged(BasemapGallery gallery)
        {
            if (gallery.Portal is ArcGISPortal portal)
            {
                var portalItems = await PopulateBasemapsForPortal(portal);
                gallery.AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>(portalItems);
            }
        }

        public async Task LoadFromDefaultPortal(BasemapGallery gallery)
        {
            var portalItems = await PopulateFromDefaultList();
            gallery.AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>(portalItems);
        }

        public void HandleAvailableBasemapsCollectionChanged(BasemapGallery gallery, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    e.NewItems?.OfType<BasemapGalleryItem>().ToList().ForEach(bmgi => bmgi.NotifySpatialReferenceChanged(gallery.GeoModel));
                    _ = UpdateSelectionForGeoModelBasemap(gallery);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _ = UpdateSelectionForGeoModelBasemap(gallery);
                    break;
            }
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

            await Task.WhenAll(listOfBasemaps.Select(gi => gi.LoadAsync()));

            return listOfBasemaps;
        }

        static async Task<List<BasemapGalleryItem>> PopulateFromDefaultList()
        {
            ArcGISPortal defaultPortal = await ArcGISPortal.CreateAsync();

            var results = await defaultPortal.GetDeveloperBasemapsAsync();

            List<BasemapGalleryItem> listOfBasemaps = new List<BasemapGalleryItem>();

            foreach (var basemap in results)
            {
                listOfBasemaps.Add(new BasemapGalleryItem(basemap));
            }

            await Task.WhenAll(listOfBasemaps.Select(gi => gi.LoadAsync()));

            return listOfBasemaps;
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
    }
}
#endif
