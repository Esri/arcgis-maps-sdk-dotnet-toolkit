// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Toolkit.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// Internal class encapsulating a layer item representing the virtual root item for the legend tree.
    /// The LayerItems collection of this item is the collection of map layer item displayed at the first level of the TOC.
    /// This class manages the events coming from the map, from the map layers and from the map layer items.
    /// </summary>
    internal sealed class LegendTree : LayerItemViewModel
    {
        #region Constructor

        // Listen for layers DP changes (listen for "ShowLegend" changes)
        private readonly DependencyPropertyChangedListeners<LegendTree> _layerPropertyChangedListeners;

        public LegendTree()
        {
            LayerItemsOptions = new LayerItemsOpts(returnMapLayerItems: false, returnGroupLayerItems: false,
                                                   returnLegendItems: false,
                                                   showOnlyVisibleLayers: true, reverseLayersOrder: false);
            Attach(this);
            _layerPropertyChangedListeners = new DependencyPropertyChangedListeners<LegendTree>(this)
            {
                OnEventAction = (instance, source, eventArgs) => instance.Layer_PropertyChanged(source, eventArgs)
            };
        }
        ~LegendTree()
        {
            Detach();
        } 
        #endregion

        #region LegendItemTemplate
        /// <summary>
        /// Gets or sets the legend item template.
        /// </summary>
        /// <value>The legend item template.</value>
        private DataTemplate _legendItemTemplate;
        internal DataTemplate LegendItemTemplate
        {
            get
            {
                return _legendItemTemplate;
            }
            set
            {
                if (_legendItemTemplate != value)
                {
                    _legendItemTemplate = value;
                    PropagateTemplate();
                    UpdateLayerItemsOptions();
                }
            }
        }
        #endregion

        #region LayerTemplate
        private DataTemplate _layerTemplate;
        /// <summary>
        /// Gets or sets the layer template i.e. the template used to display a layer in the legend.
        /// </summary>
        /// <value>The layer template.</value>
        internal DataTemplate LayerTemplate
        {
            get
            {
                return _layerTemplate;
            }
            set
            {
                if (_layerTemplate != value)
                {
                    _layerTemplate = value;
                    PropagateTemplate();
                }
            }
        }
        #endregion

        #region MapLayerTemplate
        private DataTemplate _mapLayerTemplate;
        /// <summary>
        /// Gets or sets the map layer template.
        /// </summary>
        /// <value>The map layer template.</value>
        internal DataTemplate MapLayerTemplate
        {
            get
            {
                return _mapLayerTemplate;
            }
            set
            {
                if (_mapLayerTemplate != value)
                {
                    _mapLayerTemplate = value;
                    PropagateTemplate();
                }
            }
        }
        #endregion

        private IEnumerable<Layer> _layers;

        internal IEnumerable<Layer> Layers
        {
            get { return _layers; }
            set {
                if (_layers is INotifyCollectionChanged)
                    (_layers as INotifyCollectionChanged).CollectionChanged -= Layers_CollectionChanged;
                if (_layers != null)
                    foreach (var layer in _layers)
                        DetachLayerHandler(layer);

                _layers = value;

                if (_layers is INotifyCollectionChanged)
                    (_layers as INotifyCollectionChanged).CollectionChanged += Layers_CollectionChanged;
                if (_layers != null)
                    foreach (var layer in _layers)
                        AttachLayerHandler(layer);
                UpdateMapLayerItems();
            }
        }

        private double _scale;

        internal double Scale
        {
            get { return _scale; }
            set { _scale = value; OnScaleChanged(); }
        }

        #region ShowOnlyVisibleLayers
        private bool _showOnlyVisibleLayers = true;
        /// <summary>
        /// Gets or sets a value indicating whether only the visible layers are participating to the legend.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if only the visible layers are participating to the legend; otherwise, <c>false</c>.
        /// </value>
        internal bool ShowOnlyVisibleLayers
        {
            get
            {
                return _showOnlyVisibleLayers;
            }
            set
            {
                _showOnlyVisibleLayers = value;
                LayerItemsOpts mode = LayerItemsOptions;
                mode.ShowOnlyVisibleLayers = value;
                PropagateLayerItemsOptions(mode);
            }
        }
        #endregion

        #region ReverseLayersOrder
        private bool _reverseLayersOrder;
        /// <summary>
        /// Gets or sets a value indicating whether only the visible layers are participating to the legend.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if only the visible layers are participating to the legend; otherwise, <c>false</c>.
        /// </value>
        internal bool ReverseLayersOrder
        {
            get
            {
                return _reverseLayersOrder;
            }
            set
            {
                _reverseLayersOrder = value;
                LayerItemsOpts mode = LayerItemsOptions;
                mode.ReverseLayersOrder = value;
                PropagateLayerItemsOptions(mode);
            }
        }
        #endregion

        #region Refresh
        /// <summary>
        /// Refreshes the legend control.
        /// </summary>
        /// <remarks>Note : In most cases, the control is always up to date without calling the refresh method.</remarks>
        internal void Refresh()
        {
            // refresh all map layer items (due to group layers we have to go through the legend hierarchy
            LayerItems.Descendants(item => item.LayerItems).OfType<MapLayerItem>().ForEach(mapLayerItem => mapLayerItem.Refresh());
        }
        #endregion

        #region Event Refreshed
        /// <summary>
        /// Occurs when the legend is refreshed. 
        /// Give the opportunity for an application to add or remove legend items.
        /// </summary>
        internal event EventHandler<Legend.RefreshedEventArgs> Refreshed;

        internal void OnRefreshed(object sender, Legend.RefreshedEventArgs args)
        {
            EventHandler<Legend.RefreshedEventArgs> refreshed = Refreshed;

            if (refreshed != null)
            {
                refreshed(sender, args);
            }
        }
        #endregion

        #region Map Event Handlers

        private ThrottleTimer _updateTimer;

        private void OnScaleChanged()
        {
            //Update Layer Visibilities is expensive, so wait for the map to stop navigating so
            //map navigation performance doesn't suffer from it.
            if (_updateTimer == null)
            {
                _updateTimer = new ThrottleTimer(100) { Action = UpdateLayerVisibilities };
            }
            _updateTimer.Invoke();
        }

        private void Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Layer layer in e.OldItems.OfType<Layer>())
                    DetachLayerHandler(layer);
            }
            if (e.NewItems != null)
            {
                foreach (Layer layer in e.NewItems.OfType<Layer>())
                    AttachLayerHandler(layer);
            }
            UpdateMapLayerItems();
        }

        private void AttachLayerHandler(Layer layer)
        {
            _layerPropertyChangedListeners.Attach(layer, "ShowLegend");
        }

        private void DetachLayerHandler(Layer layer)
        {
            _layerPropertyChangedListeners.Detach(layer);
        }

        void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowLegend")
                UpdateMapLayerItems();
        }

        #endregion

        #region Propagate methods propagating a property to all legend items of the legend tree
        private void PropagateTemplate()
        {
            // set the template on all descendants including the legend items
            LayerItems.Descendants(item => item.LayerItems).ForEach(item =>
            {
                item.Template = item.GetTemplate();
                item.LegendItems.ForEach(legendItem => legendItem.Template = legendItem.GetTemplate());
            });
        }

        private void PropagateLayerItemsOptions(LayerItemsOpts layerItemsOptions)
        {

            if (!LayerItemsOptions.Equals(layerItemsOptions))
            {
                DeferLayerItemsSourceChanged = true;
                LayerItemsOptions = layerItemsOptions;
                // set value on all descendants
                LayerItems.Descendants(layerItem => layerItem.LayerItems).ForEach(layerItem => layerItem.LayerItemsOptions = layerItemsOptions);
                DeferLayerItemsSourceChanged = false;
            }
        } 
        #endregion

        #region Private Methods

        private void UpdateMapLayerItems()
        {
            var mapLayerItems = new ObservableCollection<LayerItemViewModel>();

            if (Layers != null)
            {
                UpdateMapLayerItemsRecursive(mapLayerItems, Layers);
            }
            LayerItems = mapLayerItems;
        }

        private void UpdateMapLayerItemsRecursive(ObservableCollection<LayerItemViewModel> mapLayerItems, IEnumerable<Layer> layers)
        {
            foreach (Layer layer in layers.Where(l => l.ShowLegend))
            {
                MapLayerItem mapLayerItem = FindMapLayerItem(layer);

                if (mapLayerItem == null) // else reuse existing map layer item to avoid query again the legend and to keep the current state (selected, expansed, ..)
                {
                    // Create a new map layer item
                    mapLayerItem = new MapLayerItem(layer) { LegendTree = this };
                    mapLayerItem.Refresh();
                }

                mapLayerItems.Add(mapLayerItem);
                if(layer is GroupLayer)
                    UpdateMapLayerItemsRecursive(mapLayerItems, (layer as GroupLayer).ChildLayers);
            }
        }

        private IEnumerable<MapLayerItem> MapLayerItems
        {
            get
            {
                if (LayerItems == null)
                    return null;

                return LayerItems.OfType<MapLayerItem>();
            }
        }

        private MapLayerItem FindMapLayerItem(Layer layer)
        {
            return MapLayerItems == null ? null : MapLayerItems.FirstOrDefault(mapLayerItem => mapLayerItem.Layer == layer);
        }

        internal void UpdateLayerVisibilities()
        {
            LayerItems.ForEach(layerItem =>
                {
                    layerItem.DeferLayerItemsSourceChanged = true;
                    layerItem.UpdateLayerVisibilities(true, true, true);
                    layerItem.DeferLayerItemsSourceChanged = false;
                }
            );
        }
        #endregion

        #region LayerItemsMode

        private void UpdateLayerItemsOptions()
        {
            LayerItemsOpts layerItemsOptions;
            bool returnsLegendItems = (LegendItemTemplate != null);

            layerItemsOptions = new LayerItemsOpts(false, false, returnsLegendItems, ShowOnlyVisibleLayers, ReverseLayersOrder);

            PropagateLayerItemsOptions(layerItemsOptions);
        }

        #endregion

    }
}
