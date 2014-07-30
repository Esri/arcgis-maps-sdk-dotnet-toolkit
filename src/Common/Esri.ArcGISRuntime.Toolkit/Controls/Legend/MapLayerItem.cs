// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Toolkit.Controls.Primitives;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
	/// <summary>
	/// Internal class encapsulating a layer item representing a map layer.
	/// This class manages the events coming from the layer and the legend refresh process.
	/// </summary>
	internal sealed class MapLayerItem : LayerItemViewModel
	{
		#region Constructors
		internal const string MapLayerType = "MapLayer Layer";
		private bool _isQuerying;
		private double _serviceMinScale = double.PositiveInfinity; // Max and Min scale coming from the service : to combine with max and min scale coming from the layer at client side
		private double _serviceMaxScale;
        // Listen for layers DP changes
        private DependencyPropertyChangedListener<MapLayerItem> _layerDisplayNameChangedListener;
        private DependencyPropertyChangedListener<MapLayerItem> _layerMinScaleChangedListener;
        private DependencyPropertyChangedListener<MapLayerItem> _layerMaxScaleChangedListener;
        private DependencyPropertyChangedListener<MapLayerItem> _layerStatusChangedListener;
        private DependencyPropertyChangedListener<MapLayerItem> _layerIsVisibleChangedListener;

		/// <summary>
		/// Initializes a new instance of the <see cref="MapLayerItem"/> class.
		/// </summary>
		/// <param name="layer">The layer.</param>
		internal MapLayerItem(Layer layer)
			: base(layer)
		{
			LayerType = MapLayerType;

			Label = layer.DisplayName;
			MinimumScale = layer.MinScale;
			MaximumScale = layer.MaxScale;
			IsVisible = layer.IsVisible;
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="MapLayerItem"/> class. Only useful in Design.
		/// </summary>
		internal MapLayerItem()
		{
			LayerType = MapLayerType;
		}

		#endregion

		#region Events Handler
		private void AttachLayerEventHandler(Layer layer)
		{
			Debug.Assert(layer != null);
			if (layer is ILegendSupport)
				(layer as ILegendSupport).LegendChanged += OnLayerLegendChanged;

		    _layerDisplayNameChangedListener = new DependencyPropertyChangedListener<MapLayerItem>(this, layer, "DisplayName")
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };
            _layerMinScaleChangedListener = new DependencyPropertyChangedListener<MapLayerItem>(this, layer, "MinScale")
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };
            _layerMaxScaleChangedListener = new DependencyPropertyChangedListener<MapLayerItem>(this, layer, "MaxScale")
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };
            _layerIsVisibleChangedListener = new DependencyPropertyChangedListener<MapLayerItem>(this, layer, "IsVisible")
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };
            _layerStatusChangedListener = new DependencyPropertyChangedListener<MapLayerItem>(this, layer, "Status")
            {
                OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
            };
        }

		private void DetachLayerEventHandler(Layer layer)
		{
			if (layer != null)
			{
				if (layer is ILegendSupport)
					(layer as ILegendSupport).LegendChanged -= OnLayerLegendChanged;
                _layerDisplayNameChangedListener.Detach();
                _layerMinScaleChangedListener.Detach();
                _layerMaxScaleChangedListener.Detach();
                _layerIsVisibleChangedListener.Detach();
                _layerStatusChangedListener.Detach();
            }
		}

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var layer = sender as Layer;
			if (layer == null)
				return;

			if (e.PropertyName == "MinScale")
			{
				MinimumScale = Layer.MinScale != 0 && !double.IsNaN(Layer.MinScale)
					               ? Math.Min(_serviceMinScale, Layer.MinScale)
					               : _serviceMinScale;
				if (LegendTree != null)
					LegendTree.UpdateLayerVisibilities();
			}
			else if (e.PropertyName == "MaxScale")
			{
				MaximumScale = !double.IsNaN(Layer.MaxScale)
								   ? Math.Max(_serviceMaxScale, Layer.MaxScale)
								   : _serviceMaxScale;
				if (LegendTree != null)
					LegendTree.UpdateLayerVisibilities();
			}
			else if (e.PropertyName == "IsVisible")
			{
				if (LegendTree != null)
					LegendTree.UpdateLayerVisibilities();
			}
			else if (e.PropertyName == "DisplayName")
			{
				Label = layer.DisplayName;
			}
			else if (e.PropertyName == "Status")
			{
				//if (!(sender is GroupLayerBase)) // For group layers, we don't wait for initialized event 
				Refresh();
			}
		}

		private void OnLayerLegendChanged(object sender, EventArgs e)
		{
			// Structure of legend items has changed -> refresh
			Refresh();
		}

		#endregion

		#region Refresh
		/// <summary>
		/// Refreshes the legend from infos coming from the map layer.
		/// </summary>
		internal async void Refresh()
		{
			if (_isQuerying || Layer == null)
				return; // already querying

			//if (!(Layer is GroupLayerBase)) // GroupLayer : don't wait for layer intialized, so the user will see the layer hierarchy even if the group layer is not initialized yet (else would need to wait for all sublayers initialized)
			//{
				if (Layer.Status < LayerStatus.Initialized) 
				{
					IsBusy = true; // set busy indicator waiting for layer initialized
					return; // Refresh will be done on event Initialized
				}

				LayerItems = null;
			//}
			LegendItems = null;

			if (Layer is ILegendSupport)
			{
				IsBusy = true;
				_isQuerying = true;
				var legendSupport = Layer as ILegendSupport;

				LayerLegendInfo result = null;
				Exception exception = null;
				try
				{
					result = await legendSupport.GetLegendInfosAsync();
				}
				catch (Exception ex)
				{
					exception = ex;
				}

				if (LegendTree == null)
				{
					// The legend item has been detached ==> result no more needed
					IsBusy = _isQuerying = false;
					return;
				}

				if (exception != null)
				{
					// Fire event Refreshed with an exception
					if (LegendTree != null)
						LegendTree.OnRefreshed(this, new Legend.RefreshedEventArgs(this, exception));

					_isQuerying = false;
					IsBusy = false;
					return;
				}

				if (result != null)
				{
					Description = result.LayerDescription;
					if (string.IsNullOrEmpty(Label)) // Label is set with LayerID : keep it if not null
						Label = result.LayerName;

					// Combine Layer and Service scale
					double minScale = result.MinimumScale == 0.0 ? double.PositiveInfinity : result.MinimumScale;
					_serviceMinScale = minScale;
					if (Layer.MinScale != 0.0 && !double.IsNaN(Layer.MinScale))
						minScale = Math.Min(minScale, Layer.MinScale);
					double maxScale = result.MaximumScale;
					_serviceMaxScale = maxScale;
					if (!double.IsNaN(Layer.MaxScale))
						maxScale = Math.Max(maxScale, Layer.MaxScale);

					MinimumScale = minScale;
					MaximumScale = maxScale;

					IsHidden = result.IsHidden;

					// For feature layers, force the geometry type since GeometryType.Unknown doesn't work well with advanced symbology.
					Geometry.GeometryType geometryType = Geometry.GeometryType.Unknown;
					if (Layer is FeatureLayer)
					{
						var fl = Layer as FeatureLayer;
						geometryType = fl.FeatureTable.GeometryType;
					}

					if (result.LayerLegendInfos != null)
					{
						LayerItems = result.LayerLegendInfos.Select(info => new LayerItemViewModel(Layer, info, Description)).ToObservableCollection();
					}

					if (result.LegendItemInfos != null)
					{
						LegendItems = result.LegendItemInfos.Select(info => new LegendItemViewModel(info, geometryType)).ToObservableCollection();
					}
				}

				// If groupLayer -> add the child layers 
				//AddGroupChildLayers();

				// Kml layer particular case : if a KML layer has only a child which is not another KML layer ==> set the child item as transparent so it doesn't appear in the legend
				//ProcessKmlLayer();

				LegendTree.UpdateLayerVisibilities();
				_isQuerying = false;
				// Fire event Refreshed without exception
				LegendTree.OnRefreshed(this, new Legend.RefreshedEventArgs(this, null));
				IsBusy = false;
			}
			else
			{
				IsBusy = false;
				// Fire event Refreshed
				if (LegendTree != null)
					LegendTree.OnRefreshed(this, new Legend.RefreshedEventArgs(this, null));

			}
		}
		
		//private void AddGroupChildLayers()
		//{
		//	if (Layer is GroupLayerBase)
		//	{
		//		ObservableCollection<LayerItemViewModel> mapLayerItems = new ObservableCollection<LayerItemViewModel>();
		//		if ((Layer as GroupLayerBase).ChildLayers != null)
		//		{
		//			foreach (Layer layer in (Layer as GroupLayerBase).ChildLayers)
		//			{
		//				Layer layerToFind = layer;
		//				MapLayerItem mapLayerItem = LayerItems == null ? null : LayerItems.FirstOrDefault(item => item.Layer == layerToFind) as MapLayerItem;

		//				if (mapLayerItem == null || mapLayerItems.Contains(mapLayerItem)) // else reuse existing map layer item to avoid querying again the legend and lose the item states (note : contains test if for the degenerated case where a layer is twice or more in a group layer)
		//				{
		//					// Create a new map layer item
		//					mapLayerItem = new MapLayerItem(layer) { LegendTree = this.LegendTree };
		//					if (_dispatcher != null)
		//						_dispatcher.BeginInvoke(new Action(() => mapLayerItem.Refresh()));
		//					else
		//						mapLayerItem.Refresh();
		//				}
		//				mapLayerItems.Add(mapLayerItem);
		//			}
		//		}
		//		LayerItems = mapLayerItems;

		//		// Don't display the AcceleratedDisplayLayers root node
		//		if (Layer is AcceleratedDisplayLayers)
		//			IsTransparent = true;
		//	}
		//}

		//// KML Layer case : if a KML layer has only a child which is not another KML layer ==> set the child item as transparent so it doesn't appear in the legend
		//private void ProcessKmlLayer()
		//{
		//	// Must be a group layer (KmlLayer inherits from GroupLayer)
		//	if (!(Layer is GroupLayer))
		//		return;

		//	// Must have only one child
		//	LayerCollection layers = (Layer as GroupLayer).ChildLayers;
		//	if (layers == null || layers.Count() != 1)
		//		return;

		//	// The child must not be a KMLLayer i.e. not a group layer (sub folder and sub document must not be removed from the legend)
		//	Layer childLayer = layers.FirstOrDefault();
		//	if (childLayer is GroupLayer)
		//		return;

		//	// The layer must be a KmlLayer
		//	if (!IsKmlLayer())
		//		return;

		//	// Set the child as transparent
		//	LayerItemViewModel childLayerItem = LayerItems == null ? null : LayerItems.FirstOrDefault();
		//	if (childLayerItem != null)
		//		childLayerItem.IsTransparent = true;
		//}

		// check that the layer inherits from kmllayer by using name in order to avoid a reference to DataSources project
		//private bool IsKmlLayer()
		//{
		//	Type type = Layer.GetType();
		//	while (type != null)
		//	{
		//		if ( type.Name == "KmlLayer")
		//			return true;
		//		type = type.BaseType;
		//	}
		//	return false;
		//}

		#endregion

		#region Attach/Detach

		internal override void Attach(LegendTree legendTree)
		{
			if (legendTree == null)
				return;

			base.Attach(legendTree);

			AttachLayerEventHandler(Layer);
		}

		internal override void Detach()
		{
			DetachLayerEventHandler(Layer);

			base.Detach();
		}
		#endregion
	}
}
