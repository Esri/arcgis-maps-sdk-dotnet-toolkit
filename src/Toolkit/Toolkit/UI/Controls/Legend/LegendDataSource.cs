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
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Core;
#endif
#endif

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    internal class LegendDataSource : IList<object>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private readonly ConcurrentDictionary<ILayerContent, Task<IReadOnlyList<LegendInfo>>> _legendInfoTasks = new ConcurrentDictionary<ILayerContent, Task<IReadOnlyList<LegendInfo>>>();

        private List<object> _items = new List<object>();
        private GeoView _geoview;
        private bool _filterByVisibleScaleRange = true;
        private bool _filterHiddenLayers = true;
        private bool _reverseLayerOrder = true;

        public LegendDataSource(GeoView geoview)
        {
            if (geoview != null)
            {
                SetGeoView(geoview);
            }
        }

        public bool FilterByVisibleScaleRange
        {
            get => _filterByVisibleScaleRange;
            set
            {
                if (_filterByVisibleScaleRange != value)
                {
                    _filterByVisibleScaleRange = value;
                    MarkCollectionDirty(false);
                }
            }
        }

        public bool FilterHiddenLayers
        {
            get => _filterHiddenLayers;
            set
            {
                if (_filterHiddenLayers != value)
                {
                    _filterHiddenLayers = value;
                    MarkCollectionDirty(false);
                }
            }
        }

        public bool ReverseLayerOrder
        {
            get => _reverseLayerOrder;
            set
            {
                if (_reverseLayerOrder != value)
                {
                    _reverseLayerOrder = value;
                    MarkCollectionDirty(false);
                }
            }
        }

#if NETFX_CORE
        private long _propertyChangedCallbackToken = 0;
#endif

        public void SetGeoView(GeoView geoview)
        {
            if (geoview == _geoview)
            {
                return;
            }

            if (_geoview != null)
            {
                _geoview.LayerViewStateChanged -= GeoView_LayerViewStateChanged;
                (_geoview as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
#if !XAMARIN && !XAMARIN_FORMS
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
            _currentScale = double.NaN;
            if (_geoview != null)
            {
                _geoview.LayerViewStateChanged += GeoView_LayerViewStateChanged;
                (_geoview as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#if !XAMARIN && !XAMARIN_FORMS
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
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(MapView)).AddValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#endif
                if (_geoview is MapView mv)
                {
                    _currentScale = mv.MapScale;
                }
            }

            UpdateItemsSource();
        }

#if !XAMARIN && !XAMARIN_FORMS
        private void GeoViewDocumentChanged(object sender, object e)
        {
            UpdateItemsSource();
        }
#endif

        private void RefreshOnLoad(ILoadable loadable)
        {
            var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(loadable)
            {
                OnEventAction = (instance, source, eventArgs) => RunOnUIThread(UpdateItemsSource),
                OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent
            };
            loadable.Loaded += listener.OnEvent;
        }

        private void UpdateItemsSource()
        {
            _legendInfoTasks.Clear();
            IEnumerable<Layer> layers = null;
            _currentScale = double.NaN;
            if (_geoview is MapView mv)
            {
                if (mv.Map != null && mv.Map.OperationalLayers == null)
                {
                    RefreshOnLoad(mv.Map);
                }
                else
                {
                    layers = mv.Map?.OperationalLayers;
                }

                _currentScale = mv.MapScale;
            }
            else if (_geoview is SceneView sv)
            {
                if (sv.Scene != null && sv.Scene.OperationalLayers == null)
                {
                    RefreshOnLoad(sv.Scene);
                }
                else
                {
                    layers = sv.Scene?.OperationalLayers;
                }
            }

            if (layers is INotifyCollectionChanged incc)
            {
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                {
                    OnEventAction = (instance, source, eventArgs) => Layers_CollectionChanged(source, eventArgs),
                    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
                };
                incc.CollectionChanged += listener.OnEvent;
            }

            TrackLayers(layers);

            _items = BuildLegendList(_reverseLayerOrder ? layers?.Reverse() : layers) ?? new List<object>();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private IEnumerable<Layer> _currentLayers;

        private void TrackLayers(IEnumerable<Layer> layers)
        {
            if (_currentLayers != null)
            {
                foreach (var layer in _currentLayers)
                {
                    layer.PropertyChanged -= Layer_PropertyChanged;
                }
            }

            _currentLayers = layers;
            if (_currentLayers != null)
            {
                foreach (var layer in _currentLayers)
                {
                    layer.PropertyChanged += Layer_PropertyChanged;
                }
            }

            // TODO: Remove entries in _legendInfoTasks that are no longer needed
            // This does happen when the items source gets replaced, but we could be a bit more aggressive
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var layer = sender as Layer;
            if (e.PropertyName == nameof(Layer.LoadStatus) && layer.LoadStatus == LoadStatus.Loaded)
            {
                MarkCollectionDirty();
            }
            else if ((e.PropertyName == nameof(layer.IsVisible) && _filterHiddenLayers) || e.PropertyName == nameof(layer.ShowInLegend))
            {
                MarkCollectionDirty(false);
            }
        }

        private void Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TrackLayers(sender as IEnumerable<Layer>);
            MarkCollectionDirty();
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

        private void RunOnUIThread(Action action)
        {
#if XAMARIN_FORMS
            global::Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
#elif __IOS__
            _geoview.InvokeOnMainThread(action);
#elif __ANDROID__
            _geoview.PostDelayed(action, 500);
#elif NETFX_CORE
            _ = _geoview.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => action());
#else
            _geoview.Dispatcher.Invoke(action);
#endif
        }

        private void RebuildCollection()
        {
            lock (_dirtyLock)
            {
                _isCollectionDirty = false;
            }

            var newItems = BuildLegendList(_reverseLayerOrder ? _currentLayers?.Reverse() : _currentLayers) ?? new List<object>();
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
                var changedObjects = new List<object>();
                var newItem = i < newItems.Count ? newItems[i] : null;
                var oldItem = i < _items.Count ? _items[i] : null;
                if (newItem == oldItem)
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
                System.Diagnostics.Debug.Assert(newItems[i] == _items[i], $"Legend entry {i} doesn't match");
            }
#endif
            _items = newItems;
        }

        private void GeoView_LayerViewStateChanged(object sender, LayerViewStateChangedEventArgs e) => MarkCollectionDirty();

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
#if XAMARIN || XAMARIN_FORMS
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                UpdateItemsSource();
            }
#endif
            if (e.PropertyName == nameof(MapView.MapScale) && sender is MapView mv)
            {
                _currentScale = mv.MapScale;
                MarkCollectionDirty();
            }
        }

        private double _currentScale = double.NaN;

        private List<object> BuildLegendList(IEnumerable<ILayerContent> layers)
        {
            if (layers == null)
            {
                return null;
            }

            List<object> data = new List<object>();
            foreach (var layer in layers)
            {
                if (!layer.ShowInLegend || (!layer.IsVisible && _filterHiddenLayers))
                {
                    continue;
                }

                if (layer is Layer l)
                {
                    var state = _geoview.GetLayerViewState(l);
                    if (state != null &&
                        ((state.Status == LayerViewStatus.NotVisible && _filterHiddenLayers) ||
                        (state.Status == LayerViewStatus.OutOfScale && _filterByVisibleScaleRange)))
                    {
                        continue;
                    }
                }
                else if (layer is ILayerContent ilc)
                {
                    if (!ilc.IsVisible && _filterHiddenLayers)
                    {
                        continue;
                    }
                    else if (_filterByVisibleScaleRange && !double.IsNaN(_currentScale) && _currentScale > 0 && !ilc.IsVisibleAtScale(_currentScale))
                    {
                        continue;
                    }
                }

                data.Add(layer);
                if (!(layer is Layer) || ((Layer)layer).LoadStatus == LoadStatus.Loaded)
                {
                    // Generate the legend infos
                    // For layers, we'll wait with entering here, until the GeoView decides to load them before generating the legend
                    if (!_legendInfoTasks.ContainsKey(layer))
                    {
                        var task = LoadLegend(layer);
                        _legendInfoTasks[layer] = task;
                    }
                    else
                    {
                        var task = _legendInfoTasks[layer];
                        if (task.Status == TaskStatus.RanToCompletion)
                        {
                            var legends = _legendInfoTasks[layer].Result;
                            data.AddRange(legends);
                        }
                    }
                }

                if (layer.SublayerContents != null)
                {
                    data.AddRange(BuildLegendList(_reverseLayerOrder ? layer.SublayerContents : layer.SublayerContents?.Reverse() )); // This might seem counter-intuitive, but sublayers are already top-to-bottom, as opposed to the layer collection
                }
            }

            return data;
        }

        private async Task<IReadOnlyList<LegendInfo>> LoadLegend(ILayerContent layer)
        {
            var result = await layer.GetLegendInfosAsync().ConfigureAwait(false);
            if (result.Count > 0)
            {
                MarkCollectionDirty();
            }

            return result;
        }

#region IList<T>

        public int Count => _items.Count;

        public bool IsReadOnly => true;

        public bool IsFixedSize => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public object this[int index] { get => _items[index]; set => throw new NotSupportedException(); }

        public void Insert(int index, object item) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void Add(object item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(object item) => _items.Contains(item);

        public void CopyTo(object[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public bool Remove(object item) => throw new NotSupportedException();

        public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(object item) => _items.IndexOf(item);

#endregion

#region List

        int IList.Add(object value) => throw new NotSupportedException();

        void IList.Remove(object value) => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

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

        public event PropertyChangedEventHandler PropertyChanged;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
