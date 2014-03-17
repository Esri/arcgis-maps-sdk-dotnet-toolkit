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
using Esri.ArcGISRuntime.Toolkit.Internal;

#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Helpers
{
    /// <summary>
    /// Helper class for observing recursively layers added/removed/moved from a layer collection.
    /// Takes care of GroupLayers by observing recursively the collection GroupLayer.ChildLayers
    /// and by observing the GroupLayer.ChildLayers property changed 
    /// </summary>
    public sealed class LayerCollectionRecursiveObserver
    {
        // Listen for Layers Collection changed
        private WeakEventListener<LayerCollectionRecursiveObserver, object, NotifyCollectionChangedEventArgs> _layersWeakEventListener;

        // Listen for GroupLayer.ChildLayers property changed
        private DependencyPropertyChangedListeners<LayerCollectionRecursiveObserver> _childLayersPropertyChangedListeners;

        // LayerCollectionRecursiveObserver (one by GroupLayer) for listening recursively to ChildLayers collection changed
        private readonly IDictionary<GroupLayer, LayerCollectionRecursiveObserver> _observersByGroupLayer = new Dictionary<GroupLayer, LayerCollectionRecursiveObserver>();

        // Helper to raise LayerAdded/LayerRemoved/LayerMoved events.
        // All sub _observersByGroupLayer share the same helper 
        readonly RaiseEventsHelper _raiseEventsHelper;

        // Reset Collection doesn't provide any infos about the previous collection , so we keep as private member a copy of layers collection to be able to unsubscribe
        private IEnumerable<Layer> _currentLayers;


        /// <summary>
        /// Initializes a new instance of the <see cref="LayerCollectionRecursiveObserver"/> class.
        /// </summary>
        public LayerCollectionRecursiveObserver()
        {
            _raiseEventsHelper = new RaiseEventsHelper(this);
            InitChildLayersListener();
        }

        // Private constructor for sharing the same eventsHelper
        private LayerCollectionRecursiveObserver(RaiseEventsHelper raiseEventsHelper)
        {
            Debug.Assert(raiseEventsHelper != null);
            _raiseEventsHelper = raiseEventsHelper;
            InitChildLayersListener();
        }

        // Initialize the listener to ChildLayers property changed
        private void InitChildLayersListener()
        {
            _childLayersPropertyChangedListeners = new DependencyPropertyChangedListeners<LayerCollectionRecursiveObserver>(this)
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
                    OnLayerAdded(layer);
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
                    OnLayerRemoved(layer);
                _currentLayers = null;
            }
        }

        // A layer has been added in the current collection
        private void OnLayerAdded(Layer layer)
        {
            Debug.Assert(layer != null);
            _raiseEventsHelper.RaiseLayerAdded(layer);

            var glayer = layer as GroupLayer;
            if (glayer != null)
            {
                // If the layer added is a groupLayer, we need:
                // 1) To subscribe to the ChildLayers property changed
                // 2) To observe the changes in the hierarchy of ChildLayers (layers added/removed/moved)
                _childLayersPropertyChangedListeners.Attach(glayer, "ChildLayers");
                Debug.Assert(!_observersByGroupLayer.ContainsKey(glayer), "twice the same layer in the layer collection?");

                // Create one LayerCollectionRecursiveObserver to listen to sublayers
                if (!_observersByGroupLayer.ContainsKey(glayer))
                {
                    var observer = new LayerCollectionRecursiveObserver(_raiseEventsHelper);
                    observer.StartObserving(glayer.ChildLayers);
                    _observersByGroupLayer.Add(glayer, observer);
                }
            }
        }

        // A layer has been removed from the layer collection
        private void OnLayerRemoved(Layer layer)
        {
            Debug.Assert(layer != null);
            Debug.Assert(_currentLayers.Contains(layer));

            var glayer = layer as GroupLayer;
            if (glayer != null)
            {
                _childLayersPropertyChangedListeners.Detach(layer);
                Debug.Assert(_observersByGroupLayer.ContainsKey(glayer), "group layer never attached?");
                if (_observersByGroupLayer.ContainsKey(glayer))
                {
                    var observer = _observersByGroupLayer[glayer];
                    observer.StopObserving();
                    _observersByGroupLayer.Remove(glayer);
                }
            }
            _raiseEventsHelper.RaiseLayerRemoved(layer);
        }

        private void OnLayerPropertyChanged(DependencyObject sender, PropertyChangedEventArgs e)
        {
            var layer = sender as Layer;
            if (layer == null)
                return;

            Debug.Assert(e.PropertyName == "ChildLayers", "Only ChildLayers property is supposed to be observed");
            if (e.PropertyName == "ChildLayers")
            {
                // The ChildLayers property of a group layer has changed --> reuse the same observer for observing the new ChildLayers collection 
                var glayer = layer as GroupLayer;
                Debug.Assert(glayer != null);
                Debug.Assert(_observersByGroupLayer.ContainsKey(glayer), "group layer observer not created");
                if (glayer != null && _observersByGroupLayer.ContainsKey(glayer))
                {
                    var observer = _observersByGroupLayer[glayer];
                    observer.StopObserving(); // stop observing the previousChildLayers collection
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
                    _raiseEventsHelper.RaiseLayersMoved(e.NewItems.OfType<Layer>());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    removed = _currentLayers.Except(layers).ToArray();
                    added = layers.Except(_currentLayers).ToArray();
                    if (!removed.Any() && !added.Any())
                        _raiseEventsHelper.RaiseLayersMoved(layers); // likeky a sort
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            foreach (var layer in removed)
            {
                OnLayerRemoved(layer);
            }
            foreach (var layer in added)
            {
                OnLayerAdded(layer);
            }
            _currentLayers = layers.ToArray();
        }

        /// <summary>
        /// Occurs when a layer is added in the hierarchy of the layer collection.
        /// </summary>
        public event EventHandler<LayerEventsArgs> LayerAdded;

        /// <summary>
        /// Occurs when a layer is removed in the hierarchy of the layer collection.
        /// </summary>
        public event EventHandler<LayerEventsArgs> LayerRemoved;

        /// <summary>
        /// Occurs when a layer is moved in the hierarchy of the layer collection.
        /// </summary>
        public event EventHandler<LayerEventsArgs> LayerMoved;

        internal void RaiseLayerAdded(Layer layer)
        {
            if (LayerAdded != null)
                LayerAdded(this, new LayerEventsArgs(layer));
        }

        internal void RaiseLayerRemoved(Layer layer)
        {
            if (LayerRemoved != null)
                LayerRemoved(this, new LayerEventsArgs(layer));
        }

        internal void RaiseLayerMoved(Layer layer)
        {
            if (LayerMoved != null)
                LayerMoved(this, new LayerEventsArgs(layer));
        }

    }

	/// <summary>
	///  EventArgs type for LayerCollectionRecursiveObserver events.
	/// </summary>
    public class LayerEventsArgs : EventArgs
    {
        internal LayerEventsArgs(Layer layer)
        {
            Layer = layer;
        }

		/// <summary>
		/// Gets the layer.
		/// </summary>
		/// <value>
		/// The layer.
		/// </value>
        public Layer Layer { get; private set; }
    }

    // Helper class that raises LayerCollectionRecursiveObserver events 
    internal class RaiseEventsHelper
    {
        private readonly LayerCollectionRecursiveObserver _recursiveObserver;
        internal RaiseEventsHelper(LayerCollectionRecursiveObserver recursiveObserver)
        {
            _recursiveObserver = recursiveObserver;
        }

        public void RaiseLayerAdded(Layer layer)
        {
            _recursiveObserver.RaiseLayerAdded(layer);
        }

        public void RaiseLayerRemoved(Layer layer)
        {
            _recursiveObserver.RaiseLayerRemoved(layer);
        }

        public void RaiseLayersMoved(IEnumerable<Layer> layers)
        {
            if (layers != null)
            {
                foreach (var layer in layers)
                    _recursiveObserver.RaiseLayerMoved(layer);
            }
        }
    }
}