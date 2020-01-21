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
        private CancellationTokenSource _cancellationTokenSource;
        private bool _filterByVisibleScaleRange = true;
        private bool _filterHiddenLayers = true;

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
                    MarkCollectionDirty();
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
                    MarkCollectionDirty();
                }
            }
        }

        public void SetGeoView(GeoView geoview)
        {
            if (geoview == _geoview)
            {
                return;
            }

            if (_geoview != null)
            {
                (_geoview as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
                _geoview.LayerViewStateChanged -= GeoView_LayerViewStateChanged;
            }

            _geoview = geoview;
            _currentScale = double.NaN;
            if (_geoview != null)
            {
                (_geoview as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
                _geoview.LayerViewStateChanged += GeoView_LayerViewStateChanged;

                if (_geoview is MapView mv)
                {
                    _currentScale = mv.MapScale;
                }
            }

            UpdateItemsSource();
        }

        private async void UpdateItemsSource()
        {
            _legendInfoTasks.Clear();
            _items = new List<object>();
            IEnumerable<Layer> layers = null;
            if (_geoview is MapView mv)
            {
                if (mv.Map != null && mv.Map.LoadStatus != LoadStatus.Loaded)
                {
                    try
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        await mv.Map.LoadAsync(); // TODO: Don't force-load
                        layers = mv.Map.OperationalLayers;
                    }
                    catch { }
                }
            }
            else if (_geoview is SceneView sv)
            {
                if (sv.Scene != null && sv.Scene.LoadStatus != LoadStatus.Loaded)
                {
                    try
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        await sv.Scene.LoadAsync(); // TODO: Don't force-load
                        layers = sv.Scene.OperationalLayers;
                    }
                    catch { }
                }
            }

            _items = new List<object>();
            if (layers is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += Layers_CollectionChanged; // TODO: Make weak
            }

            TrackLayers(layers);

            _items = BuildLegendList(layers) ?? new List<object>();
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

            // TODO: Remove _legendInfoTasks no longer being tracked
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var layer = sender as Layer;
            if ((e.PropertyName == nameof(Layer.LoadStatus) && layer.LoadStatus == LoadStatus.Loaded) ||
                (e.PropertyName == nameof(layer.IsVisible) && _filterHiddenLayers) || e.PropertyName == nameof(layer.ShowInLegend))
            {
                MarkCollectionDirty();
            }
        }

        private void Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TrackLayers(sender as IEnumerable<Layer>);
            MarkCollectionDirty();
        }

        private bool _isCollectionDirty;
        private object _dirtyLock = new object();

        private void MarkCollectionDirty()
        {
            lock (_dirtyLock)
            {
                if (_isCollectionDirty)
                {
                    return;
                }

                _isCollectionDirty = true;
            }

#if XAMARIN_FORMS
            global::Xamarin.Forms.Device.BeginInvokeOnMainThread(RebuildCollection);
#elif __IOS__
            _geoview.InvokeOnMainThread(RebuildCollection);
#elif __ANDROID__
            _geoview.PostDelayed(RebuildCollection, 500);
#elif NETFX_CORE
            _ = _geoview.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, RebuildCollection);
#else
            _geoview.Dispatcher.Invoke(RebuildCollection);
#endif
        }

        private void RebuildCollection()
        {
            //await Task.Delay(500); // Delay rebuilding a bit so we don't do it too often.
            lock (_dirtyLock)
            {
                _isCollectionDirty = false;
            }

            var newItems = BuildLegendList(_currentLayers) ?? new List<object>();
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
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                _currentScale = double.NaN;
                UpdateItemsSource();
            }

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
                if (!layer.ShowInLegend || !layer.IsVisible)
                {
                    continue;
                }

                if (layer is Layer l)
                {
                    var state = _geoview.GetLayerViewState(l);
                    if (state.Status == LayerViewStatus.NotVisible && _filterHiddenLayers)
                    {
                        continue;
                    }

                    if (state.Status == LayerViewStatus.OutOfScale && _filterByVisibleScaleRange)
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
                    else if (_filterByVisibleScaleRange && !double.IsNaN(_currentScale) && _currentScale > 0 && ilc.IsVisibleAtScale(_currentScale))
                    {
                        continue;
                    }
                }

                data.Add(layer);
                if (layer is ILoadable loadable && loadable.LoadStatus == LoadStatus.Loaded)
                {
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
                    data.AddRange(BuildLegendList(layer.SublayerContents));
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
