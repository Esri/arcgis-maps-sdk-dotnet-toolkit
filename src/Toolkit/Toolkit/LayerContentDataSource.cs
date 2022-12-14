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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if MAUI
using Esri.ArcGISRuntime.Maui;
using View = Microsoft.Maui.Controls.View;
#else
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Core;
using View = Windows.UI.Xaml.DependencyObject;
#elif WINUI
using View = Microsoft.UI.Xaml.DependencyObject;
#elif __IOS__
using View = UIKit.UIView;
#elif __ANDROID__
using View = Android.Views.View;
#else
using View = System.Windows.DependencyObject;
#endif
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// A generic helper class for tracking changes to the layers in a MapView or SceneView and generate a bindable list of information about the map,
    /// handles changes on multiple threads, yet ensures the collection is only updated on the UI Thread.
    /// Used by Legend and Table of Contents controls.
    /// </summary>
    /// <typeparam name="T">List item type.</typeparam>
    internal abstract class LayerContentDataSource<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged, IList
        where T : ILayerContentItem
    {
        private readonly View _owner;
        private GeoView? _geoview;
        private List<T> _items = new List<T>();

        protected LayerContentDataSource(View owner)
        {
            _owner = owner;
        }

        protected IList<T> Items => _items;

        protected GeoView? GeoView => _geoview;

        private protected void RunOnUIThread(Action action)
        {
#if MAUI
            _owner.Dispatcher.Dispatch(action);
#else
            if (_owner == null)
            {
                action();
                return;
            }
#if __IOS__
            _owner.InvokeOnMainThread(action);
#elif __ANDROID__
            _owner.PostDelayed(action, 50);
#elif NETFX_CORE
            _ = _owner.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => action());
#elif WINUI
            _ = _owner.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => action());
#else
            _owner.Dispatcher.Invoke(action);
#endif
#endif
        }

#if WINDOWS_XAML
        private long _propertyChangedCallbackToken = 0;
#endif

        internal void SetGeoView(GeoView? geoview)
        {
            if (geoview == _geoview)
            {
                return;
            }

            if (_geoview != null)
            {
                _geoview.LayerViewStateChanged -= GeoView_LayerViewStateChanged;
                (_geoview as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#if MAUI
                (_geoview as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
#else
                if (_geoview is MapView mapview)
                {
#if WINDOWS_XAML
                    mapview.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoview is SceneView sceneview)
                {
#if WINDOWS_XAML
                    sceneview.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#endif
            }

            var oldGeoview = _geoview;
            _geoview = geoview;
            if (_geoview != null)
            {
                _geoview.LayerViewStateChanged += GeoView_LayerViewStateChanged;
                (_geoview as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#if MAUI
#else
                if (_geoview is MapView mapview)
                {
#if WINDOWS_XAML
                    _propertyChangedCallbackToken = mapview.RegisterPropertyChangedCallback(MapView.MapProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoview is SceneView sceneview)
                {
#if WINDOWS_XAML
                    _propertyChangedCallbackToken = sceneview.RegisterPropertyChangedCallback(SceneView.SceneProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#endif
            }

            OnGeoViewChanged(oldGeoview, geoview);
            OnDocumentChanged();
        }

        protected virtual void OnGeoViewChanged(GeoView? oldGeoview, GeoView? newGeoview)
        {
        }

        private void GeoView_LayerViewStateChanged(object? sender, LayerViewStateChangedEventArgs e)
        {
            OnLayerViewStateChanged(sender as Layer, e);
        }

        protected virtual void OnLayerViewStateChanged(Layer? layer, LayerViewStateChangedEventArgs layerViewState)
        {
        }

#if WPF || WINDOWS_XAML
        private void GeoViewDocumentChanged(object? sender, object e)
        {
            OnDocumentChanged();
            if (sender is MapView mapView)
            {
                OnGeoViewPropertyChanged(mapView, nameof(MapView.Map));
            }
            else if (sender is SceneView sceneView)
            {
                OnGeoViewPropertyChanged(sceneView, nameof(SceneView.Scene));
            }
        }
#endif

        private void GeoView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is GeoView geoview)
            {
#if MAUI
                if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                    (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
                {
                    OnDocumentChanged();
                }
#endif
                OnGeoViewPropertyChanged(geoview, e.PropertyName);
            }
        }

        protected virtual void OnGeoViewPropertyChanged(GeoView geoView, string? propertyName)
        {
        }

        private WeakEventListener<LayerContentDataSource<T>, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _documentListener;

        private void SubscribeToDocument(GeoModel? document)
        {
            if (_documentListener != null)
            {
                _documentListener.Detach();
                _documentListener = null;
            }

            if (document != null)
            {
                _documentListener = new WeakEventListener<LayerContentDataSource<T>, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, document)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.DocumentPropertyChanged(instance, eventArgs.PropertyName),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                document.PropertyChanged += _documentListener.OnEvent;
            }
        }

        private void DocumentPropertyChanged(object? sender, string? propertyName)
        {
            if (propertyName == nameof(Mapping.Map.OperationalLayers))
            {
                TrackOperationalLayers();
                MarkCollectionDirty(false);
            }
            else if (propertyName == nameof(Mapping.Map.Basemap))
            {
                SubscribeToBasemap((sender as Mapping.Map)?.Basemap ?? (sender as Scene)?.Basemap);
            }

            OnDocumentPropertyChanged(sender, propertyName);
        }

        protected virtual void OnDocumentPropertyChanged(object? sender, string? propertyName)
        {
        }

        private WeakEventListener<LayerContentDataSource<T>, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _basemapListener;

        private void SubscribeToBasemap(Basemap? basemap)
        {
            if (_basemapListener != null)
            {
                _basemapListener.Detach();
                _basemapListener = null;
            }

            if (basemap != null)
            {
                _basemapListener = new WeakEventListener<LayerContentDataSource<T>, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, basemap)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.BasemapPropertyChanged(instance, eventArgs.PropertyName),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                basemap.PropertyChanged += _basemapListener.OnEvent;
            }
        }

        private void BasemapPropertyChanged(INotifyPropertyChanged instance, string? propertyName)
        {
            OnBasemapPropertyChanged((Basemap)instance, propertyName);
        }

        protected void OnBasemapPropertyChanged(Basemap basemap, string? propertyName)
        {
        }

        private void OnDocumentChanged()
        {
            SubscribeToDocument(null); // Detaches listeners
            if (_geoview is MapView mv)
            {
                if (mv.Map != null)
                {
                    SubscribeToDocument(mv.Map);
                }
            }
            else if (_geoview is SceneView sv)
            {
                if (sv.Scene != null)
                {
                    SubscribeToDocument(sv.Scene);
                }
            }

            TrackOperationalLayers();

            OnDocumentReset();
            MarkCollectionDirty(false);
        }

        protected virtual void OnDocumentReset()
        {
        }

        private void Layer_PropertyChanged(ILayerContent sender, string? propertyName)
        {
            var layer = sender as ILayerContent;
            if (layer is ILoadable loadable && propertyName == nameof(ILoadable.LoadStatus) && loadable.LoadStatus == LoadStatus.Loaded)
            {
                MarkCollectionDirty();
            }
            else if (propertyName == nameof(ILayerContent.SublayerContents))
            {
                TrackOperationalLayers();
                MarkCollectionDirty();
            }

            OnLayerPropertyChanged(layer, propertyName);
        }

        protected virtual void OnLayerPropertyChanged(ILayerContent layer, string? propertyName)
        {
        }

        private List<Action>? _operationalLayerTrackingDetachActions;

        private void TrackOperationalLayers()
        {
            // Detach all listeners, and create a new recursive set
            if (_operationalLayerTrackingDetachActions != null)
            {
                foreach (var action in _operationalLayerTrackingDetachActions)
                {
                    action.Invoke();
                }

                _operationalLayerTrackingDetachActions = null;
            }

            IEnumerable<Layer>? layers = null;

            if (_geoview is MapView mv)
            {
                layers = mv.Map?.OperationalLayers;
            }
            else if (_geoview is SceneView sv)
            {
                layers = sv.Scene?.OperationalLayers;
            }

            _operationalLayerTrackingDetachActions = new List<Action>(TrackLayerContentsRecursive(layers));
        }

        private IEnumerable<Action> TrackLayerContentsRecursive(IEnumerable<ILayerContent>? layers)
        {
            if (layers != null)
            {
                foreach (var layer in layers)
                {
                    if (layer is INotifyPropertyChanged inpc)
                    {
                        var listener = new WeakEventListener<LayerContentDataSource<T>, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpc)
                        {
                            OnEventAction = static (instance, source, eventArgs) => instance.Layer_PropertyChanged((ILayerContent)source, eventArgs.PropertyName),
                            OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                        };
                        inpc.PropertyChanged += listener.OnEvent;
                        yield return listener.Detach;
                    }

                    foreach (var sublayerAction in TrackLayerContentsRecursive(layer.SublayerContents))
                    {
                        yield return sublayerAction;
                    }
                }

                if (layers is INotifyCollectionChanged incc)
                {
                    var listener = new WeakEventListener<LayerContentDataSource<T>, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, incc)
                    {
                        OnEventAction = static (instance, source, eventArgs) => instance.Layers_CollectionChanged(source, eventArgs),
                        OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
                    };
                    incc.CollectionChanged += listener.OnEvent;
                    yield return listener.Detach;
                }
            }
        }

        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            TrackOperationalLayers();
            MarkCollectionDirty();
        }

        private bool _isCollectionDirty;
        private object _dirtyLock = new object();

        protected async void MarkCollectionDirty(bool delay = true)
        {
            lock (_dirtyLock)
            {
                if (_isCollectionDirty)
                {
                    return;
                }

                _isCollectionDirty = true;
            }

            if (delay)
            {
                // Delay update in case of frequent events to reduce load
                await Task.Delay(250).ConfigureAwait(false);
            }

            RunOnUIThread(RebuildCollection);
        }

        private void RebuildCollection()
        {
            lock (_dirtyLock)
            {
                _isCollectionDirty = false;
            }

            var newItems = OnRebuildCollection();
            if (newItems.Count == 0 && _items.Count == 0)
            {
                return;
            }

            if (newItems.Count == 0 || _items.Count == 0)
            {
                _items = newItems;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                return;
            }

            int i = 0;
            for (; i < newItems.Count || i < _items.Count; i++)
            {
                var newItem = i < newItems.Count ? newItems[i] : default(T);
                var oldItem = i < _items.Count ? _items[i] : default(T);
                if (newItem?.Content == oldItem?.Content)
                {
                    continue;
                }

                if (newItem == null)
                {
                    // Item was removed from the end
                    var removedItem = oldItem;
                    _items.RemoveAt(i);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, i));
                    i--;
                }
                else if (!_items.Contains(newItem))
                {
                    // Item was added
                    _items.Insert(i, newItem);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, i));
                }
                else
                {
                    // Item was removed (or moved)
                    var removedItem = oldItem;
                    _items.RemoveAt(i);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, i));
                    i--;
                }
            }
#if DEBUG
            // Validate the calculated collection is in sync
            System.Diagnostics.Debug.Assert(newItems.Count == _items.Count, "Entry count doesn't match");
            for (i = 0; i < newItems.Count; i++)
            {
                System.Diagnostics.Debug.Assert(newItems[i].Content == _items[i].Content, "Entry " + i + " doesn't match");
            }
#endif
        }

        protected abstract List<T> OnRebuildCollection();

        #region IList<T>

        public int Count => Items.Count;

        public bool IsReadOnly => true;

        public bool IsFixedSize => false;

        public bool IsSynchronized => (Items as ICollection)?.IsSynchronized ?? false;

        public object SyncRoot => ((ICollection)Items).SyncRoot;

        public T this[int index] { get => Items[index]; set => throw new NotSupportedException(); }

        public void Insert(int index, T item) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void Add(T item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(T item) => Items.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

        public bool Remove(T item) => throw new NotSupportedException();

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        public int IndexOf(T item) => Items.IndexOf(item);

        #endregion

        #region List

        object? IList.this[int index] { get => Items[index]; set => throw new NotSupportedException(); }

        int IList.Add(object? value) => throw new NotSupportedException();

        void IList.Remove(object? value) => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int index) => ((ICollection)Items).CopyTo(array, index);

        bool IList.Contains(object? value) => ((IList)Items).Contains(value);

        int IList.IndexOf(object? value) => ((IList)Items).IndexOf(value);

        void IList.Insert(int index, object? value) => throw new NotSupportedException();

        #endregion

        private protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
            OnPropertyChanged("Item[]");
            if (args.Action != NotifyCollectionChangedAction.Move)
            {
                OnPropertyChanged(nameof(Count));
            }
        }

        private protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
    }
}
