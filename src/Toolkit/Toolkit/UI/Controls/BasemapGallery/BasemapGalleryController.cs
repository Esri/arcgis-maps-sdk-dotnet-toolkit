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
    public class BasemapGalleryController : INotifyPropertyChanged
    {
        private BasemapGalleryItem? _selectedBasemap;
        private GeoView? _geoview;
        private ArcGISPortal? _portal;
        private ArcGISPortal? _bakedInPortal;

        private readonly ObservableCollection<BasemapGalleryItem> _galleryItems = new ObservableCollection<BasemapGalleryItem>();
#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGalleryController"/> class.
        /// </summary>
        public BasemapGalleryController()
        {
            // Load from default list
            _ = ConfigureFromDefaultList();
        }

        /// <summary>
        /// Gets the collection of items to display.
        /// </summary>
        public ObservableCollection<BasemapGalleryItem> Basemaps => _galleryItems;

        /// <summary>
        /// Gets or sets the portal used to populate the list.
        /// </summary>
        /// <remarks>
        /// Setting this to a new portal will reset the contents of the list, including any custom additions.
        /// </remarks>
        public ArcGISPortal? Portal
        {
            get => _portal;
            set
            {
                if (_portal != value)
                {
                    _portal = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Portal)));
                    _ = UpdateForCurrentPortal(_portal);
                }
            }
        }

        /// <summary>
        /// Gets or sets a reference to the connected geoview.
        /// </summary>
        /// <remarks>
        /// The GeoView and any map or scene is observed for changes.
        /// Changes to the map or scene's spatial reference will change the validity of <see cref="BasemapGalleryItem"/> instances.
        /// Selection of a basemap via <see cref="SelectedBasemap"/> will change the map or scene's basemap. If the map or scene property is null, a new map or scene will be created with the selected basemap.
        /// </remarks>
        public GeoView? GeoView
        {
            get => _geoview;

            set
            {
                if (_geoview != value)
                {
                    if (_geoview is MapView oldMapView)
                    {
                        oldMapView.SpatialReferenceChanged -= Geoview_SpatialReferenceChanged;
#if NETFX_CORE && !XAMARIN_FORMS
                        oldMapView.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#elif NETCOREAPP || NETFRAMEWORK
                        DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(oldMapView, GeoViewDocumentChanged);
#endif
                    }
                    else if (_geoview is SceneView oldSceneView)
                    {
                        oldSceneView.SpatialReferenceChanged -= Geoview_SpatialReferenceChanged;
#if NETFX_CORE && !XAMARIN_FORMS
                        oldSceneView.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#elif NETCOREAPP || NETFRAMEWORK
                        DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(oldSceneView, GeoViewDocumentChanged);
#endif
                    }

                    if (_geoview is INotifyPropertyChanged oldViewAsINPC)
                    {
                        oldViewAsINPC.PropertyChanged -= GeoViewDocumentChanged;
                    }

                    _geoview = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoView)));

                    if (_geoview is MapView newMapView)
                    {
                        _geoview.SpatialReferenceChanged += Geoview_SpatialReferenceChanged;
#if NETFX_CORE && !XAMARIN_FORMS
                        _propertyChangedCallbackToken = newMapView.RegisterPropertyChangedCallback(MapView.MapProperty, GeoViewDocumentChanged);
#elif NETCOREAPP || NETFRAMEWORK // WPF
                        DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(newMapView, GeoViewDocumentChanged);
#endif
                    }
                    else if (_geoview is SceneView newSceneView)
                    {
                        _geoview.SpatialReferenceChanged += Geoview_SpatialReferenceChanged;
#if NETFX_CORE && !XAMARIN_FORMS
                        _propertyChangedCallbackToken = newSceneView.RegisterPropertyChangedCallback(SceneView.SceneProperty, GeoViewDocumentChanged);
#elif NETCOREAPP || NETFRAMEWORK // WPF
                        DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(newSceneView, GeoViewDocumentChanged);
#endif
                    }

                    if (_geoview is INotifyPropertyChanged newViewAsINPC)
                    {
                        newViewAsINPC.PropertyChanged += GeoViewDocumentChanged;
                    }

                    SelectedBasemap = null;

                    // SceneView always reports WGS84, but uses WebMercator basemaps
                    HandleSpatialReferenceChanged();
                    _ = HandleMapBasemapChanges();
                }
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

        private async Task HandleMapBasemapChanges()
        {
            // If current scene/map is null, selected item is null & pinned item is null
            Basemap? basemapFromView = null;

            if (GeoView is MapView mapView && mapView.Map?.Basemap != null)
            {
                basemapFromView = mapView.Map.Basemap;
            }
            else if (GeoView is SceneView sceneView && sceneView.Scene?.Basemap != null)
            {
                basemapFromView = sceneView.Scene.Basemap;
            }

            // Handle case where map and scene start with empty basemap
            if (basemapFromView != null && await BasemapIsActuallyNotABasemap(basemapFromView))
            {
                basemapFromView = null;
            }

            if (basemapFromView == null)
            {
                SelectedBasemap = null;
                return;
            }

            BasemapGalleryItem itemForBasemap = new BasemapGalleryItem(basemapFromView);
            await itemForBasemap.LoadAsync();

            // If current map/scene basemap is in collection, select it
            if (_galleryItems.Contains(itemForBasemap))
            {
                SelectedBasemap = itemForBasemap;
            }
            else
            {
                SelectedBasemap = null;
            }
            #if XAMARIN_FORMS
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBasemap)));
            #endif
        }

        private void GeoViewDocumentChanged(object? sender, object? e)
        {
            if (e is PropertyChangedEventArgs pcea && pcea.PropertyName != nameof(SceneView.Scene) && pcea.PropertyName != nameof(MapView.Map))
            {
                return;
            }

            if (_geoview is MapView mv && mv.Map is INotifyPropertyChanged mapINPC)
            {
                // Listen for load completion
                var listener = new WeakEventListener<INotifyPropertyChanged, object?, PropertyChangedEventArgs>(mapINPC)
                {
                    OnEventAction = (instance, source, eventArgs) => HandleDocPropertyChanged(source, eventArgs),
                    OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent,
                };
                mapINPC.PropertyChanged += listener.OnEvent;
            }
            else if (_geoview is SceneView sv && sv.Scene is INotifyPropertyChanged sceneINPC)
            {
                // Listen for load completion
                var listener = new WeakEventListener<INotifyPropertyChanged, object?, PropertyChangedEventArgs>(sceneINPC)
                {
                    OnEventAction = (instance, source, eventArgs) => HandleDocPropertyChanged(source, eventArgs),
                    OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent,
                };
                sceneINPC.PropertyChanged += listener.OnEvent;
            }

            _ = HandleMapBasemapChanges();
        }

        private void HandleDocPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Basemap")
            {
                _ = HandleMapBasemapChanges();
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

                    if (_selectedBasemap?.Basemap != null && GeoView is MapView mv)
                    {
                        if (mv.Map == null)
                        {
                            mv.Map = new Map(_selectedBasemap.Basemap.Clone());
                        }
                        else if (mv.Map.Basemap != _selectedBasemap.Basemap)
                        {
                            mv.Map.Basemap = _selectedBasemap.Basemap.Clone();
                        }
                    }
                    else if (_selectedBasemap?.Basemap != null && GeoView is SceneView sv)
                    {
                        if (sv.Scene == null)
                        {
                            sv.Scene = new Scene(_selectedBasemap.Basemap.Clone());
                        }
                        else if (sv.Scene.Basemap != _selectedBasemap.Basemap)
                        {
                            sv.Scene.Basemap = _selectedBasemap.Basemap.Clone();
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task UpdateForCurrentPortal(ArcGISPortal? portal)
        {
            if (portal == null)
            {
                await ConfigureFromDefaultList();
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

            _galleryItems.Clear();
            foreach (var item in basemaps)
            {
                _galleryItems.Add(new BasemapGalleryItem(item));
            }

            await Task.WhenAll(_galleryItems.Select(gi => gi.LoadAsync()));
            await HandleMapBasemapChanges();
        }

        private async Task ConfigureFromDefaultList()
        {
            if (_bakedInPortal == null)
            {
                _bakedInPortal = await ArcGISPortal.CreateAsync();
            }

            var results = await _bakedInPortal.GetDeveloperBasemapsAsync();

            _galleryItems.Clear();

            foreach (var basemap in results)
            {
                _galleryItems.Add(new BasemapGalleryItem(basemap));
            }

            await Task.WhenAll(_galleryItems.Select(gi => gi.LoadAsync()));

            await HandleMapBasemapChanges();
        }

        private void HandleSpatialReferenceChanged(BasemapGalleryItem? inputItem = null)
        {
            SpatialReference? currentSR = GeoView?.SpatialReference;
            if (GeoView is SceneView sv && sv.Scene is Scene scene)
            {
                currentSR = scene.SceneViewTilingScheme == SceneViewTilingScheme.WebMercator ? SpatialReferences.WebMercator : SpatialReferences.Wgs84;
            }

            if (inputItem == null)
            {
                _galleryItems?.ToList().ForEach(item => _ = item.NotifySpatialReferenceChanged(currentSR));
            }
            else
            {
                _ = inputItem.NotifySpatialReferenceChanged(currentSR);
            }
        }

        private void Geoview_SpatialReferenceChanged(object? sender, EventArgs? e) =>
            HandleSpatialReferenceChanged();

        /// <summary>
        /// Adds the specified item to the gallery.
        /// </summary>
        public void Add(BasemapGalleryItem item)
        {
            _galleryItems.Add(item);
            HandleSpatialReferenceChanged(item);
        }

        /// <summary>
        /// Removes the specified item from the gallery. Will not affect the item representing the current map or scene if that item is not present in the gallery.
        /// </summary>
        public void Remove(BasemapGalleryItem item) => _galleryItems.Remove(item);

        /// <summary>
        /// Adds the specified basemap to the gallery.
        /// </summary>
        public void Add(Basemap basemap) => Add(new BasemapGalleryItem(basemap));

        /// <summary>
        /// Removes the specified basemap from the gallery. Will not affect the basemap from the map or scene.
        /// </summary>
        public void Remove(Basemap basemap) => Remove(new BasemapGalleryItem(basemap));
    }
}
