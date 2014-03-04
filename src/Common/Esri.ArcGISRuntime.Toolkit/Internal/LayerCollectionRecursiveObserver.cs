// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using Esri.ArcGISRuntime.Layers;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;

#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Helper class for observing recursively layers added/removed/moved from a layer collection.
    /// Takes care of GroupLayers by observing recursively the collection GroupLayer.ChildLayers
    /// and by observing the GroupLayer.ChildLayers property changed 
    /// </summary>
    internal sealed class LayerCollectionRecursiveObserver
    {
        // Observer (generally a control that implements ILayerCollectionObserver interface) that delegates the observation to this object
        readonly ILayerCollectionObserver _observer;

        // Listen for Layers Collection changed
        private WeakEventListener<LayerCollectionRecursiveObserver, object, NotifyCollectionChangedEventArgs> _layersWeakEventListener;

        // Listen for GroupLayer.ChildLayers property changed
        private readonly DependencyPropertyChangedListeners<LayerCollectionRecursiveObserver> _layerPropertyChangedListeners;

        // LayerCollectionRecursiveObserver (one by GroupLayer) for listening recursively to ChildLayers
        private IDictionary<GroupLayer, LayerCollectionRecursiveObserver> _observersByGroupLayer = new Dictionary<GroupLayer, LayerCollectionRecursiveObserver>();

        // Reset Collection doesn't provide any infos about the previous collection , so we keep as private member a copy of layers collection to be able to unsubscibe
        private IEnumerable<Layer> _currentLayers;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerCollectionRecursiveObserver"/> class.
        /// </summary>
        /// <param name="observer">The observer that delegates the observation to this object.</param>
        public LayerCollectionRecursiveObserver(ILayerCollectionObserver observer)
        {
            Debug.Assert(observer != null);
            _observer = observer;

            // Initialize the collection of listeners to grouplayers ChildLayers changed
            _layerPropertyChangedListeners = new DependencyPropertyChangedListeners<LayerCollectionRecursiveObserver>(this)
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="LayerCollectionRecursiveObserver"/> class.
        /// </summary>
        ~LayerCollectionRecursiveObserver()
        {
            // Detach WeakEventListeners for avoiding small memory leaks
            if (_layersWeakEventListener != null)
                _layersWeakEventListener.Detach();
        }

        /// <summary>
        /// Starts observing a collection of layer.
        /// </summary>
        /// <param name="layers">The layers.</param>
        public void StartObserving(IEnumerable<Layer> layers)
        {
            Debug.Assert(_currentLayers == null, "should call StopObserving first");
            if (layers != null)
            {
                var layersINotifyCollectionChanged = layers as INotifyCollectionChanged;
                if (layersINotifyCollectionChanged != null)
                {
                    Debug.Assert(_layersWeakEventListener == null);
                    _layersWeakEventListener = new WeakEventListener<LayerCollectionRecursiveObserver, object, NotifyCollectionChangedEventArgs>(this)
                    {
                        OnEventAction = (instance, source, eventArgs) => instance.OnLayerCollectionChanged(source, eventArgs),
                        OnDetachAction = weakEventListener => layersINotifyCollectionChanged.CollectionChanged -= weakEventListener.OnEvent
                    };
                    layersINotifyCollectionChanged.CollectionChanged += _layersWeakEventListener.OnEvent;
                }
                foreach (var layer in layers)
                    AddLayer(layer);
                _currentLayers = layers.ToArray();
            }
        }

        /// <summary>
        /// Stops observing the collection of layers.
        /// </summary>
        public void StopObserving()
        {
            if (_layersWeakEventListener != null)
            {
                _layersWeakEventListener.Detach();
                _layersWeakEventListener = null;
            }
            if (_currentLayers != null)
            {
                foreach (var layer in _currentLayers)
                    RemoveLayer(layer);
                _currentLayers = null;
            }
        }

        private void AddLayer(Layer layer)
        {
            Debug.Assert(layer != null);
            _observer.LayerAdded(layer);

            var glayer = layer as GroupLayer;
            if (glayer != null)
            {
                _layerPropertyChangedListeners.Attach(glayer, "ChildLayers");
                Debug.Assert(!_observersByGroupLayer.ContainsKey(glayer), "twice the same layer in the layer collection?");

                // Create one LayerCollectionRecursiveObserver to listen to sublayers
                if (!_observersByGroupLayer.ContainsKey(glayer))
                {
                    var observer = new LayerCollectionRecursiveObserver(_observer);
                    observer.StartObserving(glayer.ChildLayers);
                    _observersByGroupLayer.Add(glayer, observer);
                }
            }
        }

        private void RemoveLayer(Layer layer)
        {
            Debug.Assert(layer != null);
            Debug.Assert(_currentLayers.Contains(layer));

            var glayer = layer as GroupLayer;
            if (glayer != null)
            {
                _layerPropertyChangedListeners.Detach(layer);
                Debug.Assert(_observersByGroupLayer.ContainsKey(glayer), "group layer never attached?");
                if (_observersByGroupLayer.ContainsKey(glayer))
                {
                    var observer = _observersByGroupLayer[glayer];
                    observer.StopObserving();
                    _observersByGroupLayer.Remove(glayer);
                }
            }
            _observer.LayerRemoved(layer);
        }

        private void OnLayerPropertyChanged(DependencyObject sender, PropertyChangedEventArgs e)
        {
            var layer = sender as Layer;
            if (layer == null)
                return;

            Debug.Assert(e.PropertyName == "ChildLayers");
            if (e.PropertyName == "ChildLayers")
            {
                var glayer = layer as GroupLayer;
                Debug.Assert(glayer != null);
                Debug.Assert(_observersByGroupLayer.ContainsKey(glayer), "group layer observer not created");
                if (glayer != null && _observersByGroupLayer.ContainsKey(glayer))
                {
                    var observer = _observersByGroupLayer[glayer];
                    observer.StopObserving();
                    observer.StartObserving(glayer.ChildLayers);
                }
            }
        }

        private void OnLayerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var layers = sender as ICollection<Layer>;
            if (layers == null) return;

            IEnumerable<Layer> added = Enumerable.Empty<Layer>();
            IEnumerable<Layer> removed = Enumerable.Empty<Layer>();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                        removed = e.OldItems.OfType<Layer>();
                    if (e.NewItems != null)
                        added = e.NewItems.OfType<Layer>();
                    break;

                case NotifyCollectionChangedAction.Move:
                    _observer.LayersMoved();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    removed = _currentLayers.Except(layers).ToArray();
                    added = layers.Except(_currentLayers).ToArray();
                    if (!removed.Any() && !added.Any())
                        _observer.LayersMoved(); // likeky a sort
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            foreach (var layer in removed)
            {
                RemoveLayer(layer);
            }
            foreach (var layer in added)
            {
                AddLayer(layer);
            }
            _currentLayers = layers.ToArray();
        }
    }


    internal interface ILayerCollectionObserver
    {
        void LayerAdded(Layer layer);
        void LayerRemoved(Layer layer);
        void LayersMoved();
    }
}