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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    internal class LegendDataSource : IList<object>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private List<object> _items = new List<object>();
        private GeoView _geoview;
        private CancellationTokenSource _cancellationTokenSource;

        public LegendDataSource(GeoView geoview)
        {
            if (geoview != null)
            {
                SetGeoView(geoview);
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
                _geoview.ViewpointChanged -= GeoView_ViewpointChanged;
            }

            _geoview = geoview;
            if (_geoview != null)
            {
                (_geoview as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
                _geoview.LayerViewStateChanged += GeoView_LayerViewStateChanged;
                _geoview.ViewpointChanged += GeoView_ViewpointChanged;
            }

            UpdateItemsSource();
        }

        private async void UpdateItemsSource()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            IEnumerable<Layer> layers = null;
            if (_geoview is MapView mv)
            {
                if (mv.Map != null && mv.Map.LoadStatus != LoadStatus.Loaded)
                {
                    try
                    {
                        await mv.Map.LoadAsync();
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
                        await sv.Scene.LoadAsync();
                        layers = sv.Scene.OperationalLayers;
                    }
                    catch { }
                }
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            _items = (await BuildLegendList(layers, token)) ?? new List<object>();
            if (token.IsCancellationRequested)
            {
                return;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            _cancellationTokenSource = null;
        }

        private void GeoView_ViewpointChanged(object sender, EventArgs e)
        {
            // TODO
        }

        private void GeoView_LayerViewStateChanged(object sender, LayerViewStateChangedEventArgs e)
        {
            // TODO
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                UpdateItemsSource();
            }
        }


        private async Task<List<object>> BuildLegendList(IEnumerable<ILayerContent> layers, CancellationToken token)
        {
            if (layers == null)
            {
                return null;
            }

            List<object> data = new List<object>();
            foreach (var layer in layers)
            {
                if (!layer.ShowInLegend)
                {
                    continue;
                }

                if (layer is ILoadable loadable)
                {
                    if (loadable.LoadStatus != LoadStatus.Loaded && loadable.LoadStatus != LoadStatus.FailedToLoad)
                    {
                        try
                        {
                            await loadable.LoadAsync();
                            if (token.IsCancellationRequested)
                            {
                                return data;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                if (!layer.ShowInLegend)
                {
                    // This could have changed after load
                    continue;
                }

                data.Add(layer);
                var infos = await layer.GetLegendInfosAsync();
                if (token.IsCancellationRequested)
                {
                    return data;
                }
                data.AddRange(infos);
                if (layer.SublayerContents != null)
                {
                    data.AddRange(await BuildLegendList(layer.SublayerContents, token));
                    if (token.IsCancellationRequested)
                    {
                        return data;
                    }
                }
            }

            return data;
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
