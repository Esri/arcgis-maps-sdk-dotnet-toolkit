using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// This class manages the tree of layer contents, and tracks their state (loading, changing etc)
    /// </summary>
    internal class ObservableLayerContentList : IReadOnlyList<LayerContentViewModel>, System.ComponentModel.INotifyPropertyChanged, System.Collections.Specialized.INotifyCollectionChanged
    {
        private List<LayerContentViewModel> _activeLayers = new List<LayerContentViewModel>();
        private IReadOnlyList<Mapping.Layer> _allLayers;
        private bool _showLegend;
        private WeakReference<ArcGISRuntime.UI.Controls.GeoView> _owningView;

        private ObservableLayerContentList(WeakReference<ArcGISRuntime.UI.Controls.GeoView> owningView, IReadOnlyList<Layer> allLayers, bool showLegend)
        {
            (allLayers as INotifyCollectionChanged).CollectionChanged += Layers_CollectionChanged;
            this._allLayers = allLayers;
            this._showLegend = showLegend;
            this._owningView = owningView;

            foreach (var item in allLayers.Where(l => IncludeLayer(l)))
            {
                LayerAdded(item);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="ObservableLayerContentList"/>
        /// </summary>
        /// <param name="mapView">MapView and its Map to monitor</param>
        /// <param name="showLegend">Also generate the legend for the layer contents</param>
        public ObservableLayerContentList(ArcGISRuntime.UI.Controls.MapView mapView, bool showLegend) : this(new WeakReference<ArcGISRuntime.UI.Controls.GeoView>(mapView), mapView.Map.AllLayers, showLegend)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="ObservableLayerContentList"/>
        /// </summary>
        /// <param name="sceneView">SceneView and its Scene to monitor</param>
        /// <param name="showLegend">Also generate the legend for the layer contents</param>
        public ObservableLayerContentList(ArcGISRuntime.UI.Controls.SceneView sceneView, bool showLegend) : this(new WeakReference<ArcGISRuntime.UI.Controls.GeoView>(sceneView), sceneView.Scene.AllLayers, showLegend)
        {
        }
        

        private bool IncludeLayer(Mapping.Layer layer)
        {
            if (layer.ShowInLegend)
                return true;

            return false;
        }

        private int GetIndexOfLayer(Mapping.Layer layer)
        {
            int i = 0;
            foreach (var item in _allLayers)
            {
                if (item == layer)
                    return i;
                if (layer.ShowInLegend)
                {
                    i++;
                }
            }
            return -1;
        }

        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Mapping.Layer>())
                {
                    LayerRemoved(item);
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Mapping.Layer>().Where(l=>IncludeLayer(l)))
                {
                    LayerAdded(item);
                }
            }
        }

        private void LayerAdded(Mapping.Layer layer)
        {
            var incc = layer as INotifyPropertyChanged;
            var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(incc);
            listener.OnEventAction = (instance, source, eventArgs) => { Layer_PropertyChanged(source, eventArgs); };
            listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
            incc.PropertyChanged += listener.OnEvent;
            if (IncludeLayer(layer))
            {
                AddToActiveLayers(layer);
            }
        }
        
        private void AddToActiveLayers(Layer layer)
        {
            var idx = GetIndexOfLayer(layer);
            if (idx < 0)
            {
                // Shouldn't really happen
                return;
            }
            var vm = new LayerContentViewModel(layer, _owningView, null, _showLegend);
            _activeLayers.Insert(idx, vm);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItem: vm, index: idx));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        private void LayerRemoved(Mapping.Layer layer)
        {
            layer.PropertyChanged -= Layer_PropertyChanged;
            if (!IncludeLayer(layer))
                return;
            RemoveFromActiveLayers(layer);
        }

        private void RemoveFromActiveLayers(Mapping.Layer layer)
        { 
            var vm = _activeLayers.Where(l => l.LayerContent == layer).FirstOrDefault();
            if (vm != null)
            {
                var idx = _activeLayers.IndexOf(vm);
                if (idx >= 0)
                {
                    _activeLayers.RemoveAt(idx);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedItem: vm, index: idx));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Items[]"));
                }
            }
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Mapping.Layer layer = (Mapping.Layer)sender;
            if (e.PropertyName == nameof(Mapping.Layer.ShowInLegend))
            {
                bool include = IncludeLayer(layer);
                if(include && !_activeLayers.Where(vm=>vm.LayerContent == layer).Any())
                {
                    AddToActiveLayers(layer);
                }
                else if(!include && _activeLayers.Where(vm => vm.LayerContent == layer).Any())
                {
                    RemoveFromActiveLayers(layer);
                }
            }
        }

        #region IReadOnlyList<T> Implementation

        /// <inheritdoc cref="IReadOnlyList{T}.this"/>
        public LayerContentViewModel this[int index]
        {
            get { return _activeLayers[Count - index - 1]; }
            set { throw new InvalidOperationException("ReadOnly"); }
        }

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
        public int Count
        {
            get { return _activeLayers.Count; }
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<LayerContentViewModel> GetEnumerator()
        {
            for (int i = _activeLayers.Count - 1; i >= 0 ; i--)
            {
                yield return _activeLayers[i];
            }
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<LayerContentViewModel>)this).GetEnumerator();
        }

        #endregion

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc cref="INotifyCollectionChanged.CollectionChanged"/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
