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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI.Controls;
#endif

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// Modifiable, observable collection of basemaps. Note, collection contents will be reset when a portal is set.
    /// </summary>
    internal class BasemapGalleryDataSource : IList<BasemapGalleryItem>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private GeoModel? _geoModel;
        private IList<BasemapGalleryItem>? _overrideList;
        private IList<BasemapGalleryItem>? _portalList;
        private BasemapGalleryItem _selectedBasemap;

        private IList<BasemapGalleryItem> ActiveBasemapList
        {
            get
            {
                if (_overrideList != null)
                {
                    return _overrideList;
                }
                else if (_portalList != null)
                {
                    return _portalList;
                }
                return new BasemapGalleryItem[] { };
            }
        }

        public void SetOverrideList(IEnumerable<BasemapGalleryItem>? basemaps)
        {
            if (_overrideList == basemaps)
            {
                return;
            }

            if (basemaps == null)
            {
                _overrideList = null;
            }
            else if (basemaps is IList<BasemapGalleryItem> listOfBasemaps)
            {
                _overrideList = listOfBasemaps;
            }
            else
            {
                _overrideList = basemaps.ToList();
            }

            // Refresh
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            // Subscribe to events if applicable
            if (basemaps is INotifyCollectionChanged iCollectionChanged)
            {
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(iCollectionChanged);
                listener.OnEventAction = (instance, source, eventArgs) => HandleOverrideListCollectionChanged(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
                iCollectionChanged.CollectionChanged += listener.OnEvent;
            }
        }

        internal void SetPortalList(IEnumerable<BasemapGalleryItem>? basemaps)
        {
            if (_portalList == basemaps)
            {
                return;
            }

            if (basemaps == null)
            {
                _portalList = null;
            }
            else if (basemaps is IList<BasemapGalleryItem> listOfBasemaps)
            {
                _portalList = listOfBasemaps;
            }
            else
            {
                _portalList = basemaps.ToList();
            }

            // Refresh
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public async Task PopulateBasemapsForPortal(ArcGISPortal? portal)
        {
            if (portal == null)
            {
                SetPortalList(null);
                return;
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

            SetPortalList(listOfBasemaps);
        }

        public async Task PopulateFromDefaultList()
        {
            ArcGISPortal defaultPortal = await ArcGISPortal.CreateAsync();

            var results = await defaultPortal.GetDeveloperBasemapsAsync();

            List<BasemapGalleryItem> listOfBasemaps = new List<BasemapGalleryItem>();

            foreach (var basemap in results)
            {
                listOfBasemaps.Add(new BasemapGalleryItem(basemap));
            }

            await Task.WhenAll(listOfBasemaps.Select(gi => gi.LoadAsync()));

            SetPortalList(listOfBasemaps);
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken;
#endif

        /// <summary>
        /// Gets or sets a reference to the connected geomodel.
        /// </summary>
        /// <remarks>
        /// Changes to the map or scene's spatial reference will change the validity of <see cref="BasemapGalleryItem"/> instances.
        /// Selection of a basemap via <see cref="SelectedBasemap"/> will change the map or scene's basemap. If the map or scene property is null, a new map or scene will be created with the selected basemap.
        /// </remarks>
        public GeoModel? GeoModel
        {
            get => _geoModel;

            set
            {
                if (_geoModel != value)
                {
                    if (_geoModel is GeoModel oldGeoModel)
                    {
                        oldGeoModel.PropertyChanged -= GeoModel_PropertyChanged;
                    }

                    _geoModel = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoModel)));

                    if (_geoModel is GeoModel newGeoModel)
                    {
                        _geoModel.PropertyChanged += GeoModel_PropertyChanged;
                    }

                    _ = UpdateBasemapSelection(_geoModel?.Basemap);

                    HandleSpatialReferenceChanged();
                }
            }
        }

        private async Task UpdateBasemapSelection(Basemap? activeBasemap)
        {
            if (activeBasemap == null)
            {
                SelectedBasemap = null;
            }

            if (await BasemapIsActuallyNotABasemap(activeBasemap))
            {
                SelectedBasemap = null;
            }

            var item = ActiveBasemapList.FirstOrDefault(item => item.Equals(new BasemapGalleryItem(activeBasemap)));
            SelectedBasemap = item;
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

        private void GeoModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeoModel.Basemap))
            {
                // TODO = does this need to be handled?
            }
            else if (e.PropertyName == nameof(GeoModel.SpatialReference))
            {
                HandleSpatialReferenceChanged();
            }
        }

        /// <summary>
        /// Gets or sets the currently selected basemap.
        /// </summary>
        /// <remarks>
        /// Setting this property will update the connected <see cref="GeoView"/>'s map or scene, or create a new map or scene if none is present.
        /// </remarks>
        public BasemapGalleryItem? SelectedBasemap
        {
            get => _selectedBasemap;

            set
            {
                if (!_selectedBasemap?.Equals(value) ?? _selectedBasemap != value)
                {
                    _selectedBasemap = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBasemap)));

                    if (_selectedBasemap?.Basemap is Basemap newBasemap && GeoModel is GeoModel activeGeoModel)
                    {
                        if (activeGeoModel != null)
                        {
                            activeGeoModel.Basemap = newBasemap.Clone();
                        }
                    }
                }
            }
        }


        private void HandleSpatialReferenceChanged()
        {
            SpatialReference? currentSR = GeoModel?.SpatialReference;

            ActiveBasemapList.ToList().ForEach(item => _ = item.NotifySpatialReferenceChanged(currentSR));
        }

        private void HandleOverrideListCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_overrideList != null)
            {
                OnCollectionChanged(e);
            }
        }

        #region IList<BasemapGalleryItem> implementation
        BasemapGalleryItem IList<BasemapGalleryItem>.this[int index] { get => ActiveBasemapList[index]; set => throw new NotImplementedException(); }

        int ICollection<BasemapGalleryItem>.Count => ActiveBasemapList.Count;

        bool ICollection<BasemapGalleryItem>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        int ICollection.Count => ActiveBasemapList.Count;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        object? IList.this[int index] { get => ActiveBasemapList[index]; set => throw new NotImplementedException(); }

        void ICollection<BasemapGalleryItem>.Add(BasemapGalleryItem item) => throw new NotImplementedException();

        void ICollection<BasemapGalleryItem>.Clear() => throw new NotImplementedException();

        bool ICollection<BasemapGalleryItem>.Contains(BasemapGalleryItem item) => ActiveBasemapList.Contains(item);

        void ICollection<BasemapGalleryItem>.CopyTo(BasemapGalleryItem[] array, int arrayIndex) => ActiveBasemapList.CopyTo(array, arrayIndex);

        IEnumerator<BasemapGalleryItem> IEnumerable<BasemapGalleryItem>.GetEnumerator() => ActiveBasemapList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ActiveBasemapList.GetEnumerator();

        int IList<BasemapGalleryItem>.IndexOf(BasemapGalleryItem item) => ActiveBasemapList.IndexOf(item);

        void IList<BasemapGalleryItem>.Insert(int index, BasemapGalleryItem item) => throw new NotImplementedException();

        bool ICollection<BasemapGalleryItem>.Remove(BasemapGalleryItem item) => throw new NotImplementedException();

        void IList<BasemapGalleryItem>.RemoveAt(int index) => throw new NotImplementedException();

        int IList.Add(object? value) => throw new NotImplementedException();

        bool IList.Contains(object? value) => ActiveBasemapList.Contains(value);

        void IList.Clear() => throw new NotImplementedException();

        int IList.IndexOf(object? value) => value is BasemapGalleryItem bm ? ActiveBasemapList.IndexOf(bm) : -1;

        void IList.Insert(int index, object? value) => throw new NotImplementedException();

        void IList.Remove(object? value) => throw new NotImplementedException();

        void IList.RemoveAt(int index) => throw new NotImplementedException();

        void ICollection.CopyTo(Array array, int index) => (ActiveBasemapList as ICollection)?.CopyTo(array, index);
        #endregion IList<BasemapGalleryItem> implementation

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
                CollectionChanged?.Invoke(this, args);
                OnPropertyChanged("Item[]");
                if (args.Action != NotifyCollectionChangedAction.Move)
                {
                    OnPropertyChanged(nameof(IList.Count));
                }
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
    }
}
