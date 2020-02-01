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

#if !__IOS__ && !__ANDROID__ && !NETSTANDARD2_0

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Core;
using Windows.UI.Xaml;
#endif
#endif

namespace Esri.ArcGISRuntime.Toolkit.Preview.UI
{
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    internal class TocDataSource : IList<TocItem>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private List<TocItem> _items = new List<TocItem>();
        private GeoView _geoview;
        private DependencyObject _owner;

        public TocDataSource(DependencyObject owner)
        {
            _owner = owner;
            _showLegend = true;
        }

        private bool _showLegend;

        public bool ShowLegend
        {
            get => _showLegend;
            set
            {
                if (_showLegend != value)
                {
                    _showLegend = value;
                    if (_items != null)
                    {
                        foreach (var item in _items)
                        {
                            item.ShowLegend = value;
                        }
                    }
                }
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        internal void SetGeoView(GeoView geoview)
        {
            if (geoview == _geoview)
            {
                return;
            }

            if (_geoview != null)
            {
#if XAMARIN || XAMARIN_FORMS
                (_geoview as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
#else
                if (_geoview is MapView mapview)
                {
#if NETFX_CORE
                    mapview.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoview is SceneView sceneview)
                {
#if NETFX_CORE
                    sceneview.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#endif
            }

            _geoview = geoview;
            if (_geoview != null)
            {
#if XAMARIN || XAMARIN_FORMS
                (_geoview as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#else
                if (_geoview is MapView mapview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = mapview.RegisterPropertyChangedCallback(MapView.MapProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoview is SceneView sceneview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = sceneview.RegisterPropertyChangedCallback(SceneView.SceneProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#endif
            }

            OnDocumentChanged();
        }

#if !XAMARIN && !XAMARIN_FORMS
        private void GeoViewDocumentChanged(object sender, object e)
        {
            OnDocumentChanged();
        }
#else
        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                OnDocumentChanged();
            }
        }
#endif

        private void SubscribeToDocument(INotifyPropertyChanged loadable)
        {
            var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(loadable)
            {
                OnEventAction = (instance, source, eventArgs) => DocumentPropertyChanged(instance, eventArgs.PropertyName),
                OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent
            };
            loadable.PropertyChanged += listener.OnEvent;
        }

        private void DocumentPropertyChanged(object sender, string propertyName)
        {
            if (propertyName == nameof(Map.Basemap))
            {
                MarkCollectionDirty(false);
            }
            else if (propertyName == nameof(Map.OperationalLayers))
            {
                MarkCollectionDirty(false);
            }
        }

        private void OnDocumentChanged()
        {
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

            MarkCollectionDirty(false);
        }

        private bool _isCollectionDirty;
        private object _dirtyLock = new object();

        private async void MarkCollectionDirty(bool delay = true)
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

            var newItems = BuildTocList() ?? new List<TocItem>();
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
                var newItem = i < newItems.Count ? newItems[i] : null;
                var oldItem = i < _items.Count ? _items[i] : null;
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
            System.Diagnostics.Debug.Assert(newItems.Count == _items.Count, "Legend entry count doesn't match");
            for (i = 0; i < newItems.Count; i++)
            {
                System.Diagnostics.Debug.Assert(newItems[i].Content == _items[i].Content, $"Legend entry {i} doesn't match");
            }
#endif
        }

        private List<TocItem> BuildTocList()
        {
            IEnumerable<Layer> layers = null;
            Basemap basemap = null;

            if (_geoview is MapView mv)
            {
                layers = mv.Map?.OperationalLayers;
                basemap = mv.Map?.Basemap;
            }
            else if (_geoview is SceneView sv)
            {
                layers = sv.Scene?.OperationalLayers;
                basemap = sv.Scene?.Basemap;
            }

            var result = new List<TocItem>();
            if (layers != null)
            {
                result.AddRange(layers.Reverse().Select(l => new TocItem(l, _showLegend)));
            }

            if (basemap != null)
            {
                result.Add(new TocItem(basemap, false));
            }

            return result;
        }

        private void RunOnUIThread(Action action)
        {
#if XAMARIN_FORMS
            global::Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
#elif __IOS__
            _owner.InvokeOnMainThread(action);
#elif __ANDROID__
            _owner.PostDelayed(action, 500);
#elif NETFX_CORE
            _ = _owner.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => action());
#else
            _owner.Dispatcher.Invoke(action);
#endif
        }

#region IList<T>

        public int Count => _items.Count;

        public bool IsReadOnly => true;

        public bool IsFixedSize => false;

        public bool IsSynchronized => (_items as ICollection)?.IsSynchronized ?? false;

        public object SyncRoot => (_items as ICollection)?.SyncRoot;

        public TocItem this[int index] { get => _items[index]; set => throw new NotSupportedException(); }

        public void Insert(int index, TocItem item) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void Add(TocItem item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(TocItem item) => _items.Contains(item);

        public void CopyTo(TocItem[] array, int arrayIndex) => throw new NotImplementedException();

        public bool Remove(TocItem item) => throw new NotSupportedException();

        public IEnumerator<TocItem> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(TocItem item) => _items.IndexOf(item);

#endregion

#region List

        object IList.this[int index] { get => _items[index]; set => throw new NotSupportedException(); }

        int IList.Add(object value) => throw new NotSupportedException();

        void IList.Remove(object value) => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

        bool IList.Contains(object value) => _items.Contains(value);

        int IList.IndexOf(object value) => ((IList)_items).IndexOf(value);

        void IList.Insert(int index, object value) => throw new NotSupportedException();

#endregion

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
            OnPropertyChanged("Item[]");
            if (args.Action != NotifyCollectionChangedAction.Move)
            {
                OnPropertyChanged(nameof(Count));
            }
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}

#endif