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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#endif

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    internal class BookmarksViewDataSource : IList<Bookmark>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private GeoView _geoView;
        private IList<Bookmark> _overrideList;

        private IList<Bookmark> ActiveBookmarkList
        {
            get
            {
                if (_overrideList != null)
                {
                    return _overrideList;
                }
                else if (_geoView is MapView mv && mv.Map?.Bookmarks != null)
                {
                    return mv.Map.Bookmarks;
                }
                else if (_geoView is SceneView sv && sv.Scene?.Bookmarks != null)
                {
                    return sv.Scene?.Bookmarks;
                }

                return new Bookmark[] { };
            }
        }

        /// <summary>
        /// Sets the override bookmark list that will be shown instead of the Map's bookmark list.
        /// </summary>
        /// <param name="bookmarks">List of bookmarks to show.</param>
        public void SetOverrideList(IEnumerable<Bookmark> bookmarks)
        {
            // Skip if collection is the same
            if (_overrideList == bookmarks)
            {
                return;
            }

            // Set new list
            if (bookmarks == null)
            {
                _overrideList = null;
            }
            else if (bookmarks is IList<Bookmark> listOfBookmarks)
            {
                _overrideList = listOfBookmarks;
            }
            else
            {
                _overrideList = bookmarks.ToList();
            }

            // Refresh
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            // Subscribe to events if applicable
            if (bookmarks is INotifyCollectionChanged iCollectionChanged)
            {
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(iCollectionChanged);
                listener.OnEventAction = (instance, source, eventArgs) => HandleOverrideListCollectionChanged(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
                iCollectionChanged.CollectionChanged += listener.OnEvent;
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        /// <summary>
        /// Sets the GeoView from which bookmarks will be shown.
        /// </summary>
        /// <param name="view">The view from which to get Map/Scene bookmarks.</param>
        public void SetGeoView(GeoView view)
        {
            if (_geoView == view)
            {
                return;
            }

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    mapview.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoView is SceneView sceneview)
                {
#if NETFX_CORE
                    sceneview.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#else
                (_geoView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
#endif
            }

            _geoView = view;

            if (_overrideList == null)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = mapview.RegisterPropertyChangedCallback(MapView.MapProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoView is SceneView sceneview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = sceneview.RegisterPropertyChangedCallback(SceneView.SceneProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#else

                (_geoView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#endif

                // Handle case where geoview loads map while events are being set up
                GeoViewDocumentChanged(null, null);
            }
        }

#if XAMARIN || XAMARIN_FORMS
        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                GeoViewDocumentChanged(sender, e);
            }
        }
#endif

        private void GeoViewDocumentChanged(object sender, object e)
        {
            if (_geoView is MapView mv && mv.Map is ILoadable mapLoadable)
            {
                // Listen for load completion
                var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(mapLoadable);
                listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                mapLoadable.Loaded += listener.OnEvent;

                // Ensure event is raised even if already loaded
                _ = mv.Map.RetryLoadAsync();
            }
            else if (_geoView is SceneView sv && sv.Scene is ILoadable sceneLoadable)
            {
                // Listen for load completion
                var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(sceneLoadable);
                listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                sceneLoadable.Loaded += listener.OnEvent;

                // Ensure event is raised even if already loaded
                _ = sv.Scene.RetryLoadAsync();
            }

            if (_overrideList == null)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void Doc_Loaded(object sender, EventArgs e)
        {
            // Get new bookmarks collection
            BookmarkCollection bmCollection;
            if (sender is Map map)
            {
                bmCollection = map.Bookmarks;
            }
            else if (sender is Scene scene)
            {
                bmCollection = scene.Bookmarks;
            }
            else
            {
                return;
            }

            // Update list
            if (_overrideList == null)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(bmCollection);
            listener.OnEventAction = (instance, source, eventArgs) => HandleGeoViewBookmarksCollectionChanged(source, eventArgs);
            listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
            bmCollection.CollectionChanged += listener.OnEvent;
        }

        private void HandleGeoViewBookmarksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Don't do anything if the override list is there
            if (_overrideList != null)
            {
                return;
            }

            OnCollectionChanged(e);
        }

        private void HandleOverrideListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_overrideList != null)
            {
                OnCollectionChanged(e);
            }
        }

        private void RunOnUIThread(Action action)
        {
#if XAMARIN_FORMS
            global::Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
#elif __IOS__
            _geoView.InvokeOnMainThread(action);
#elif __ANDROID__
            _geoView.PostDelayed(action, 500);
#elif NETFX_CORE
            _ = _geoView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => action());
#else
            _geoView.Dispatcher.Invoke(action);
#endif
        }

        #region IList<Bookmark> implementation
        Bookmark IList<Bookmark>.this[int index] { get => ActiveBookmarkList?[index]; set => throw new NotImplementedException(); }

        int ICollection<Bookmark>.Count => ActiveBookmarkList?.Count ?? 0;

        bool ICollection<Bookmark>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        int ICollection.Count => ActiveBookmarkList?.Count ?? 0;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        object IList.this[int index] { get => ActiveBookmarkList?[index]; set => throw new NotImplementedException(); }

        void ICollection<Bookmark>.Add(Bookmark item) => throw new NotImplementedException();

        void ICollection<Bookmark>.Clear() => throw new NotImplementedException();

        bool ICollection<Bookmark>.Contains(Bookmark item) => ActiveBookmarkList?.Contains(item) ?? false;

        void ICollection<Bookmark>.CopyTo(Bookmark[] array, int arrayIndex) => ActiveBookmarkList?.CopyTo(array, arrayIndex);

        IEnumerator<Bookmark> IEnumerable<Bookmark>.GetEnumerator() => ActiveBookmarkList?.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ActiveBookmarkList?.GetEnumerator();

        int IList<Bookmark>.IndexOf(Bookmark item) => ActiveBookmarkList?.IndexOf(item) ?? -1;

        void IList<Bookmark>.Insert(int index, Bookmark item) => throw new NotImplementedException();

        bool ICollection<Bookmark>.Remove(Bookmark item) => throw new NotImplementedException();

        void IList<Bookmark>.RemoveAt(int index) => throw new NotImplementedException();

        int IList.Add(object value) => throw new NotImplementedException();

        bool IList.Contains(object value) => ActiveBookmarkList?.Contains(value) ?? false;

        void IList.Clear() => throw new NotImplementedException();

        int IList.IndexOf(object value) => ActiveBookmarkList?.IndexOf(value as Bookmark) ?? -1;

        void IList.Insert(int index, object value) => throw new NotImplementedException();

        void IList.Remove(object value) => throw new NotImplementedException();

        void IList.RemoveAt(int index) => throw new NotImplementedException();

        void ICollection.CopyTo(Array array, int index) => (ActiveBookmarkList as ICollection)?.CopyTo(array, index);
        #endregion IList<Bookmark> implementation

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            RunOnUIThread(() =>
            {
                CollectionChanged?.Invoke(this, args);
                OnPropertyChanged("Item[]");
                if (args.Action != NotifyCollectionChangedAction.Move)
                {
                    OnPropertyChanged(nameof(IList.Count));
                }
            });
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
