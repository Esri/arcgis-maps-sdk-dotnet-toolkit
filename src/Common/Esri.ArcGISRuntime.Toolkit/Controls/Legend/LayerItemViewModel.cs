// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls.Primitives
{
	/// <summary>
	/// Represents a layer in the TOC/legend.
	/// </summary>
	public class LayerItemViewModel : LegendItemViewModel
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LayerItemViewModel"/> class.
		/// </summary>
		/// <param name="layer">The layer this item represents.</param>
		public LayerItemViewModel(Layer layer)
		{
			Layer = layer;
			if (layer != null)
				_isEnabled = layer.IsVisible;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LayerItemViewModel"/> class without layer.
		/// </summary>
		internal LayerItemViewModel()
		{
			Layer = null;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="LayerItemViewModel"/> class from a <see cref="LayerLegendInfo"/>..
		/// </summary>
		/// <param name="layer">The layer.</param>
		/// <param name="layerLegendInfo">The layer legend info.</param>
		/// <param name="defaultLayerDescription">The default layer description (= the map layer description).</param>
		internal LayerItemViewModel(Layer layer, LayerLegendInfo layerLegendInfo, string defaultLayerDescription)
			: this(layer)
		{
			Debug.Assert(layerLegendInfo != null);

			SubLayerID = layerLegendInfo.SubLayerID;
			Label = layerLegendInfo.LayerName;
			ParentLabel = layer.DisplayName;
			LayerType = layerLegendInfo.LayerType;
			IsHidden = layerLegendInfo.IsHidden;

			Description = string.IsNullOrEmpty(layerLegendInfo.LayerDescription) ? defaultLayerDescription : layerLegendInfo.LayerDescription;

			// Convert scale to resolution
			MinimumScale = layerLegendInfo.MinimumScale == 0.0 ? double.PositiveInfinity : layerLegendInfo.MinimumScale;
			MaximumScale = layerLegendInfo.MaximumScale;

			if (layerLegendInfo.LayerLegendInfos != null)
			{
				LayerItems = layerLegendInfo.LayerLegendInfos.Select(info => new LayerItemViewModel(layer, info, defaultLayerDescription)).ToObservableCollection();
			}

			if (layerLegendInfo.LegendItemInfos != null)
			{
				LegendItems = layerLegendInfo.LegendItemInfos.Select(info => new LegendItemViewModel(info, Geometry.GeometryType.Unknown)).ToObservableCollection();
			}
		}

		#endregion

		#region Layer
		/// <summary>
		/// The map layer associated to the layer item.
		/// </summary>
		/// <value>The layer.</value>
		public Layer Layer { get; internal set; } 
		#endregion

		#region ParentLabel
		/// <summary>
		/// Gets the label of the parent layer.
		/// </summary>
		/// <value>The label of the parent layer.</value>
		public string ParentLabel { get; internal set; }
		#endregion

		#region SubLayerID
		/// <summary>
		/// The ID of the sub layer.
		/// </summary>
		public int SubLayerID { get; internal set; } 
		#endregion

		#region LayerType

		private string _layerType;

		/// <summary>
		/// The type of layer.
		/// </summary>
		public string LayerType
		{
			get { return _layerType; }
			set
			{
				if (_layerType != value)
				{
					_layerType = value;
					OnPropertyChanged("LayerType");
				}
			}
		}

		#endregion

		#region MinimumScale/MaximumScale

		private double _minimumScale;

		/// <summary>
		/// The minimum resolution in which this layer can be viewed.
		/// </summary>
		public double MinimumScale
		{
			get { return _minimumScale; }
			internal set
			{
				if (_minimumScale != value)
				{
					_minimumScale = value;
					OnPropertyChanged("MinimumScale");
				}
			}
		}

		private double _maximumScale;

		/// <summary>
		/// The maximum scale in which this layer can be viewed.
		/// </summary>
		public double MaximumScale
		{
			get { return _maximumScale; }
			internal set
			{
				if (_maximumScale != value)
				{
					_maximumScale = value;
					OnPropertyChanged("MaximumScale");
				}
			}
		}

		#endregion
		
		#region IsEnabled
		private bool _isEnabled = true;
		/// <summary>
		/// Whether the layer is currently enabled i.e. is checked on.
		/// </summary>
		/// <value><c>true</c> if the layer is currently enabled; otherwise, <c>false</c>.</value>
		public bool IsEnabled
		{
			get
			{
				return _isEnabled;
			}
			set
			{
				if (value != _isEnabled)
				{
					_isEnabled = value;
					if (Layer != null)
						Layer.IsVisible = value;
					OnPropertyChanged("IsEnabled");
				}
			}
		}
		#endregion

		#region IsVisible
		private bool _isVisible = true;
		/// <summary>
		/// Whether the layer is currently visible (taking care of the ascendants visibility, of the scale range and of the time extent).
		/// </summary>
		/// <remarks>The IsVisible property is also taking care of the layer intialization failure (i.e is false whether a layer failed to initialize)
		/// and of the tiled layers spatial reference mismatchs (i.e. is false whether the spatial reference of a tiled layer is not the same as the map spatial reference).</remarks>
		/// <value><c>true</c> if the layer is currently visible; otherwise, <c>false</c>.</value>
		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			internal set
			{
				if (value != _isVisible)
				{
					_isVisible = value;
					OnPropertyChanged("IsVisible");
				}
			}
		}
		#endregion

		#region IsInScaleRange
		private bool _isInScaleRange = true;
		/// <summary>
		/// Gets or sets a value indicating whether this instance is in the scale range to be visible.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is in scale range; otherwise, <c>false</c>.
		/// </value>
		public bool IsInScaleRange
		{
			get
			{
				return _isInScaleRange;
			}
			internal set
			{
				if (value != _isInScaleRange)
				{
					_isInScaleRange = value;
					OnPropertyChanged("IsInScaleRange");
				}
			}
		}
		#endregion

		#region IsInTimeExtent
		private bool _isInTimeExtent = true;
		/// <summary>
		/// Gets or sets a value indicating whether this instance is in the time extent to be visible.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is in time extent; otherwise, <c>false</c>.
		/// </value>
		internal bool IsInTimeExtent // internal for the moment, might be public?
		{
			get
			{
				return _isInTimeExtent;
			}
			private set
			{
				if (value != _isInTimeExtent)
				{
					_isInTimeExtent = value;
					OnPropertyChanged("IsInTimeExtent");
				}
			}
		}
		#endregion

		#region IsBusy/BusyIndicatorVisibility
		private bool _isBusy;
		/// <summary>
		/// Gets a value indicating whether this the layer is currently loading
		/// its legend.
		/// </summary>
		/// <value><c>true</c> if this instance is busy; otherwise, <c>false</c>.</value>
		/// <seealso cref="BusyIndicatorVisibility"/>
		public bool IsBusy
		{
			get
			{
				return _isBusy;
			}
			internal set
			{
				if (value != _isBusy)
				{
					_isBusy = value;
					OnPropertyChanged("IsBusy");
					BusyIndicatorVisibility = (_isBusy ? Visibility.Visible : Visibility.Collapsed);
				}
			}
		}

		Visibility _busyIndicatorVisibility = Visibility.Collapsed;
		/// <summary>
		/// Gets the busy indicator visibility.
		/// The busy indicator is visible while the layer is currently loading its legend.
		/// </summary>
		/// <remarks>This value makes sense for map layer items.</remarks>
		/// <value>The busy indicator visibility.</value>
		/// <seealso cref="IsBusy"/>
		public Visibility BusyIndicatorVisibility
		{
			get
			{
				return _busyIndicatorVisibility;
			}
			internal set
			{
				if (value != _busyIndicatorVisibility && !CompatUtility.IsDesignMode) // Don't show busyindicator at design time
				{
					_busyIndicatorVisibility = value;
					OnPropertyChanged("BusyIndicatorVisibility");
				}
			}
		}

		#endregion

		#region LayerItems
		private ObservableCollection<LayerItemViewModel> _layerItems;
		/// <summary>
		/// The collection of <see cref="LayerItemViewModel"/> under this layer item.
		/// </summary>
		public ObservableCollection<LayerItemViewModel> LayerItems
		{
			get
			{
				return _layerItems;
			}
			set
			{
				if (_layerItems != value)
				{
					if (_layerItems != null)
					{
						Unsubscribe(_layerItems);
						_layerItems.CollectionChanged -= LayerItems_CollectionChanged;
					}
					_layerItems = value;

					if (_layerItems != null)
					{
						Subscribe(_layerItems);
						_layerItems.CollectionChanged += LayerItems_CollectionChanged;
					}
					OnPropertyChanged("LayerItems");
					LayerItemsSourceChanged();
				}
			}
		}

		private void Subscribe(IEnumerable layerItems)
		{
			if (layerItems == null)
				return;
			foreach (LayerItemViewModel layerItem in layerItems)
			{
				layerItem.Attach(LegendTree);
				layerItem.PropertyChanged += LayerItem_PropertyChanged;
			}

		}

		private void Unsubscribe(IEnumerable layerItems)
		{
			if (layerItems == null)
				return;

			foreach (LayerItemViewModel layerItem in layerItems)
			{
				layerItem.PropertyChanged -= LayerItem_PropertyChanged;
				layerItem.Detach();
			}
		}

		// Get called when a property of a child has changed
		private void LayerItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "LayerItemsSource")
			{
				if (!LayerItemsOptions.ReturnGroupLayerItems || !LayerItemsOptions.ReturnMapLayerItems
					|| (sender is LayerItemViewModel && ((LayerItemViewModel)sender).IsTransparent))
					LayerItemsSourceChanged(); // LayerItemsSource impacted if LayerItemsSource of a child changed 
			}
			else if (e.PropertyName == "IsVisible")
			{
				if (LayerItemsOptions.ShowOnlyVisibleLayers)
					LayerItemsSourceChanged(); // LayerItemsSource impacted if isVisible of a child changed
			}
			else if (e.PropertyName == "IsTransparent" || e.PropertyName == "IsHidden")
			{
				LayerItemsSourceChanged();
			}

		}

		private void LayerItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			Unsubscribe(e.OldItems);
			Subscribe(e.NewItems);

			LayerItemsSourceChanged();
		}

		#endregion

		#region LegendItems
		ObservableCollection<LegendItemViewModel> _legendItems;
		/// <summary>
		/// The collection of <see cref="LegendItemViewModel"/> under this layer item.
		/// </summary>
		public ObservableCollection<LegendItemViewModel> LegendItems
		{
			get
			{
				return _legendItems;
			}
			set
			{
				if (_legendItems != value)
				{
					if (_legendItems != null)
					{
						_legendItems.ForEach(item => item.Detach());
						_legendItems.CollectionChanged -= LegendItems_CollectionChanged;
					}

					_legendItems = value;

					if (_legendItems != null)
					{
						_legendItems.ForEach(item => item.Attach(LegendTree));
						_legendItems.CollectionChanged += LegendItems_CollectionChanged;
					}
					OnPropertyChanged("LegendItems");
					LayerItemsSourceChanged();
				}
			}
		}

		private void LegendItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (LegendItemViewModel legendItem in e.OldItems)
				{
					legendItem.Detach();
				}
			}
			if (e.NewItems != null)
			{
				foreach (LegendItemViewModel legendItem in e.NewItems)
				{
					legendItem.Attach(LegendTree);
				}
			}
			LayerItemsSourceChanged();
		}
		
		#endregion

		#region LayerItemsSource

		private IEnumerable<LegendItemViewModel> _layerItemsSource;
		private bool _isDirty = true;
		/// <summary>
		/// The enumeration of all legend items (LegendItemViewModel + LayerItemViewModel) displayed under this layer item.
		/// This enumeration is depending on the <see cref="Legend.ShowOnlyVisibleLayers"/> property.
		/// </summary>
		public override IEnumerable<LegendItemViewModel> LayerItemsSource
		{
			get
			{
				if (_layerItemsSource == null || _isDirty)
				{
					var newLayerItemsSources = GetLayerItemsSource().ToList();
					if (!AreEquals(_layerItemsSource, newLayerItemsSources)) // Avoid changing ItemSources if the list didn't change
						_layerItemsSource = newLayerItemsSources;
					_isDirty = false;
				}
				return _layerItemsSource;
			}
		}

		private static bool AreEquals<T>(IEnumerable<T> enum1, IEnumerable<T> enum2) where T: class
		{
			return enum1 != null && enum2 != null && enum1.Count() == enum2.Count() && enum1.Zip(enum2, (item1, item2) => item1 == item2).All(b => b);
		}

		private IEnumerable<LegendItemViewModel> GetLayerItemsSource()
		{
			// Enumerates LegendItems
			if (_legendItems != null && LayerItemsOptions.ReturnLegendItems)
				foreach (LegendItemViewModel legendItem in _legendItems)
					yield return legendItem;

			// Enumerates non hidden LayerItems
			if (_layerItems != null)
			{
				IEnumerable<LayerItemViewModel> items;
				if (LayerItemsOptions.ReverseLayersOrder && _layerItems.OfType<MapLayerItem>().Any())
					items = _layerItems.Where(item => !item.IsHidden).Reverse();
				else
					items = _layerItems.Where(item => !item.IsHidden);

				foreach (LayerItemViewModel layerItem in items)
				{
					if (!LayerItemsOptions.ShowOnlyVisibleLayers || layerItem.IsVisible)
					{
						bool hasLayerChildren = layerItem.LayerItems != null && layerItem.LayerItems.Any(item => !item.IsTransparent && !item.IsHidden); // if all children are transparent or hidden, the layerItem becomes a leaf 
						bool hasNoChildren = (layerItem.LegendItems == null || !layerItem.LegendItems.Any()) &&
											 (layerItem.LayerItems == null || !layerItem.LayerItems.Any());
						bool isBusy = layerItem is MapLayerItem && layerItem.IsBusy;
						if (((layerItem.IsMapLayer && LayerItemsOptions.ReturnMapLayerItems) ||
							(!hasLayerChildren && (!hasNoChildren || isBusy))  || // Leaves are returned if they are not empty
							LayerItemsOptions.ReturnGroupLayerItems) && !layerItem.IsTransparent)
						{
							yield return layerItem;
						}
						else
						{
							// The legend item is not returned but Recursive call to get items at lower level
							foreach (LegendItemViewModel legendItem in layerItem.GetLayerItemsSource())
								yield return legendItem;
						}
					}
				}
			}
		}

		private void LayerItemsSourceChanged()
		{
			_isDirty = true;
			if (!_deferLayerItemsSourceChanged)
				OnPropertyChanged("LayerItemsSource");
		}

		private bool _deferLayerItemsSourceChanged; // Defers the firing of LayerItemsSource property changed event (to avoid too much events sent to the legend control while propagating the layer items mode)
		internal bool DeferLayerItemsSourceChanged {
			get
			{
				return _deferLayerItemsSourceChanged;
			}
			set
			{
				if (_deferLayerItemsSourceChanged != value)
				{
					_deferLayerItemsSourceChanged = value;
					if (!_deferLayerItemsSourceChanged && _isDirty)
						OnPropertyChanged("LayerItemsSource");
				}
			}
		}

		#endregion

		#region IsGroupLayer
		/// <summary>
		/// Gets a value indicating whether this instance is a group layer.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is group layer; otherwise, <c>false</c>.
		/// </value>
		public bool IsGroupLayer
		{
			get
			{
				return Layer is GroupLayer ||
				       (LayerItems != null && LayerItems.Count > 0 && !IsMapLayer);
			}
		}
		#endregion

		#region IsMapLayer
		/// <summary>
		/// Gets a value indicating whether this instance is a map layer item.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is a map layer item; otherwise, <c>false</c>.
		/// </value>
		public bool IsMapLayer
		{
			get
			{
				return LayerType != null && LayerType.Equals(MapLayerItem.MapLayerType, StringComparison.Ordinal);
			}
		}

		#endregion

		#region HideChildren

		private bool _hideChildren;
		/// <summary>
		/// Gets or sets a value indicating whether the children of this layer item must be hidden in the legend.
		/// </summary>
		/// <remarks>
		/// When a layer item is hidden, it's no more returned in the <see cref="LayerItemsSource"/> enumeration of its parent items, but 
		/// it's still returned in the <see cref="LayerItems"/> enumeration of its parent item.</remarks>
		/// <value>
		/// 	<c>true</c> if the children (layerItem and legendItem) must not be displayed in the legend; otherwise, <c>false</c>.
		/// </value>
		public bool HideChildren
		{
			get
			{
				return _hideChildren;
			}
			set
			{
				if (value != _hideChildren)
				{
					_hideChildren = value;
					OnPropertyChanged("HideChildren");
				}
			}
		}

		#endregion

		#region IsHidden

		private bool _isHidden;
		/// <summary>
		/// Gets or sets a value indicating whether this layer item must be hidden in the legend.
		/// </summary>
		/// <remarks>
		/// When a layer item is hidden, it's no more returned in the <see cref="LayerItemsSource"/> enumeration of its parent items, but 
		/// it's still returned in the <see cref="LayerItems"/> enumeration of its parent item.</remarks>
		/// <value>
		/// 	<c>true</c> if the layer item must not be displayed in the legend; otherwise, <c>false</c>.
		/// </value>
		public bool IsHidden
		{
			get
			{
				return _isHidden;
			}
			set
			{
				if (value != _isHidden)
				{
					_isHidden = value;
					OnPropertyChanged("IsHidden");
				}
			}
		}

		#endregion

		#region internal IsTransparent
		private bool _isTransparent;
		/// <summary>
		/// Gets or sets a value indicating whether this layer item is transparent, i.e. the item is not displayed in the legend
		/// and its children are attached to its parent.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is transparent; otherwise, <c>false</c>.
		/// </value>
		internal bool IsTransparent
		{
			get { return _isTransparent; }
			set
			{
				if (_isTransparent != value)
				{
					_isTransparent = value;
					OnPropertyChanged("IsTransparent");
				}
			}
		}
		#endregion

		#region internal LayerItemsOptions
		private LayerItemsOpts _layerItemsOptions;
		/// <summary>
		/// Gets or sets the layer items options used to get the layer items.
		/// </summary>
		/// <value>The layer items options.</value>
		internal LayerItemsOpts LayerItemsOptions
		{
			get
			{
				return _layerItemsOptions;
			}
			set
			{
				if (!_layerItemsOptions.Equals(value))
				{
				    _layerItemsOptions = value;
				    LayerItemsSourceChanged();
				}
			}
		}

		internal struct LayerItemsOpts
		{
			internal LayerItemsOpts(
				bool returnMapLayerItems, bool returnGroupLayerItems,
				bool returnLegendItems, bool showOnlyVisibleLayers, bool reverseLayersOrder)
			{
				ReturnMapLayerItems = returnMapLayerItems;
				ReturnGroupLayerItems = returnGroupLayerItems;
				ReturnLegendItems = returnLegendItems;
				ShowOnlyVisibleLayers = showOnlyVisibleLayers;
				ReverseLayersOrder = reverseLayersOrder;
			}

			public bool ReturnMapLayerItems;
			public bool ReturnGroupLayerItems;
			public bool ReturnLegendItems;
			public bool ShowOnlyVisibleLayers;
			public bool ReverseLayersOrder;
		};


		#endregion

		#region internal Attach/Detach

		internal override void Attach(LegendTree legendTree)
		{
			if (legendTree == null)
				return;

			base.Attach(legendTree);

			LayerItemsOptions = legendTree.LayerItemsOptions;

			LayerItems.ForEach(item => item.Attach(legendTree));
			LegendItems.ForEach(item => item.Attach(legendTree));
		}

		internal override void Detach() 
		{
			LayerItems.ForEach(item => item.Detach());
			LegendItems.ForEach(item => item.Detach());
			base.Detach();
		}
		#endregion

		#region internal override DataTemplate GetTemplate()
		/// <summary>
		/// Gets the template.
		/// </summary>
		/// <returns>The template</returns>
		internal override DataTemplate GetTemplate()
		{
			if (LegendTree == null)
				return null;

			DataTemplate template = null;

			if (IsMapLayer)
				template = LegendTree.MapLayerTemplate;

			if (template == null)
				template = LegendTree.LayerTemplate;

			return template;
		}
		
		#endregion

		#region internal void UpdateLayerVisibilities(bool isParentVisible, bool isParentInScaleRange)

		/// <summary>
		/// Updates recursively the properties IsEnabled, IsVisible, IsInScaleRange and IsInTimeExtent.
		/// </summary>
		/// <param name="isParentVisible">The visibility of the parent.</param>
		/// <param name="isParentInScaleRange">The scale range status of the parent.</param>
		/// <param name="isParentInTimeExtent">The time extent status of the parent.</param>
		internal void UpdateLayerVisibilities(bool isParentVisible, bool isParentInScaleRange, bool isParentInTimeExtent)
		{
			bool isEnabled;
			if (Layer != null)
				isEnabled = Layer.IsVisible;
			else
				isEnabled = true;

			if (isEnabled != IsEnabled)
			{
				_isEnabled = isEnabled; // Note : don't set directly the property to avoid an useless call to SetLayerVisibility
				OnPropertyChanged("IsEnabled");
			}

			double mapScale = (LegendTree != null) ? LegendTree.Scale : double.NaN;
			
			if (isParentInScaleRange)
			{
				// all ascendants are in scale range ==> Take care of resolution
				if (mapScale > 0.0)
				{
					IsInScaleRange = double.IsNaN(mapScale) || !(MaximumScale > mapScale || MinimumScale < mapScale);
				}
			}
			else
			{
				// Parent not in scale range ==> layer can't be in scale range
				IsInScaleRange = false;
			}

			// A layer is visible if it's checked on, if all its ascendants are visible, if it's in scale range and if it's in time extent
			bool isVisible = _isEnabled && isParentVisible && IsInScaleRange && IsInTimeExtent;

			// Check if there is no initialization failure and if a tiled layer is using the same spatial reference as the map (bug1419)
			if (isVisible && IsMapLayer && Layer != null)
			{
				if (Layer.InitializationException != null)
					isVisible = false;
			}

			IsVisible = isVisible;

			// Update properties recursively
			LayerItems.ForEach(layerItem => layerItem.UpdateLayerVisibilities(IsVisible, IsInScaleRange, IsInTimeExtent));
		}

		#endregion
	}
}
