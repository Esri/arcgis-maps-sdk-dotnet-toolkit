// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if NETFX_CORE
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
	/// <summary>
	/// The Legend is a utility control that displays the symbology of Layers and their description for map 
	/// interpretation.
	/// </summary>
	public class Legend : Control
	{
		#region Constructor
		private bool _isLoaded;
		private readonly LegendTree _legendTree;

		/// <summary>
		/// Initializes a new instance of the <see cref="Legend"/> class.
		/// </summary>
		public Legend()
		{
#if NETFX_CORE
			DefaultStyleKey = typeof(Legend);
#endif
			_legendTree = new LegendTree();
			_legendTree.Refreshed += OnRefreshed;
			_legendTree.PropertyChanged += LegendTree_PropertyChanged;
		}

#if !NETFX_CORE
		static Legend()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Legend),
				new FrameworkPropertyMetadata(typeof(Legend)));
		}
#endif

		void LegendTree_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "LayerItemsSource")
				UpdateLayerItemsSource();
			else if (e.PropertyName == "LayerItems")
				LayerItems = _legendTree.LayerItems;
		}

		private ThrottleTimer _updateTimer;
		private void UpdateLayerItemsSource()
		{
			if (_updateTimer == null)
			{
				_updateTimer = new ThrottleTimer(100) { Action = UpdateLayerItemsSourceImpl };
			}
			_updateTimer.Invoke();
		}

		private void UpdateLayerItemsSourceImpl()
		{
			LayerItemsSource = _legendTree.LayerItemsSource;
		}

		#endregion

		/// <summary>
		/// Gets or sets the scale if filtering layers by their visible scale range.
		/// </summary>
		public double Scale
		{
			get { return (double)GetValue(ScaleProperty); }
			set { SetValue(ScaleProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Scale"/> Dependency property.
		/// </summary>
		public static readonly DependencyProperty ScaleProperty =
			DependencyProperty.Register("Scale", typeof(double), typeof(Legend), new PropertyMetadata(double.NaN, OnScalePropertyChanged));

		private static void OnScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Legend)d).OnScalePropertyChanged((double)e.NewValue);
		}

		private void OnScalePropertyChanged(double scale)
		{
			if (!_isLoaded)
				return; // defer initialization until all parameters are well known

			_legendTree.Scale = scale;
		}

		/// <summary>
		/// Gets or sets the layers to display the legend for.
		/// </summary>
		public IEnumerable<Layer> Layers
		{
			get { return (IEnumerable<Layer>)GetValue(LayersProperty); }
			set { SetValue(LayersProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Layers"/> Dependency property.
		/// </summary>
		public static readonly DependencyProperty LayersProperty =
			DependencyProperty.Register("Layers", typeof(IEnumerable<Layer>), typeof(Legend), new PropertyMetadata(null, OnLayersPropertyChanged));

		private static void OnLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Legend)d).OnLayersPropertyChanged(e.NewValue as IEnumerable<Layer>);
		}

		private void OnLayersPropertyChanged(IEnumerable<Layer> newLayers)
		{
			if (!_isLoaded)
				return; // defer initialization until all parameters are well known

			_legendTree.Layers = newLayers;
		}


		#region ShowOnlyVisibleLayers

		/// <summary>
		/// Gets or sets a value indicating whether only the visible layers are participating to the legend.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if only the visible layers are participating to the legend; otherwise, <c>false</c>.
		/// </value>
		public bool ShowOnlyVisibleLayers
		{
			get { return (bool)GetValue(ShowOnlyVisibleLayersProperty); }
			set { SetValue(ShowOnlyVisibleLayersProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="ShowOnlyVisibleLayers"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ShowOnlyVisibleLayersProperty =
				DependencyProperty.Register("ShowOnlyVisibleLayers", typeof(bool), typeof(Legend), new PropertyMetadata(true, OnShowOnlyVisibleLayersPropertyChanged));

		private static void OnShowOnlyVisibleLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Legend)d).OnShowOnlyVisibleLayersPropertyChanged((bool)e.NewValue);
		}

		private void OnShowOnlyVisibleLayersPropertyChanged(bool newValue)
		{
			_legendTree.ShowOnlyVisibleLayers = newValue;
		}

		#endregion

		#region LayerItems
		/// <summary>
		/// Gets the LayerItems for all layers that the legend control is working with.
		/// </summary>
		/// <value>The LayerItems.</value>
		public ObservableCollection<LayerItemViewModel> LayerItems
		{
			get { return (ObservableCollection<LayerItemViewModel>)GetValue(LayerItemsProperty); }
			internal set { SetValue(LayerItemsProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="LayerItems"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LayerItemsProperty =
			DependencyProperty.Register("LayerItems", typeof(ObservableCollection<LayerItemViewModel>), typeof(Legend), null);
		#endregion

		#region LayerItemsSource
		/// <summary>
		/// The enumeration of the legend items displayed at the first level of the legend control.
		/// </summary>
		/// <value>The layer items source.</value>
		public IEnumerable<LegendItemViewModel> LayerItemsSource
		{
			get { return (IEnumerable<LegendItemViewModel>)GetValue(LayerItemsSourceProperty); }
			internal set { SetValue(LayerItemsSourceProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="LayerItemsSource"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LayerItemsSourceProperty =
			DependencyProperty.Register("LayerItemsSource", typeof(IEnumerable<LegendItemViewModel>), typeof(Legend), null);
		#endregion

		#region LegendItemTemplate

		/// <summary>
		/// Gets or sets the legend item template
		/// </summary>
		public DataTemplate LegendItemTemplate
		{
			get { return (DataTemplate)GetValue(LegendItemTemplateProperty); }
			set { SetValue(LegendItemTemplateProperty, value); }
		}

		/// /// <summary>
		/// Identifies the <see cref="LegendItemTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LegendItemTemplateProperty =
			DependencyProperty.Register("LegendItemTemplate", typeof(DataTemplate), typeof(Legend), new PropertyMetadata(null, OnLegendItemTemplateChanged));

		private static void OnLegendItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Legend)d).OnLegendItemTemplateChanged(e.NewValue as DataTemplate);
		}

		private void OnLegendItemTemplateChanged(DataTemplate newDataTemplate)
		{
			_legendTree.LegendItemTemplate = newDataTemplate;
		}

		#endregion

		#region LayerTemplate
		/// <summary>
		/// Gets or sets the layer template
		/// </summary>
		public DataTemplate LayerTemplate
		{
			get { return (DataTemplate)GetValue(LayerTemplateProperty); }
			set { SetValue(LayerTemplateProperty, value); }
		}

		/// /// <summary>
		/// Identifies the <see cref="LayerTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LayerTemplateProperty =
			DependencyProperty.Register("LayerTemplate", typeof(DataTemplate), typeof(Legend), new PropertyMetadata(null, OnLayerTemplateChanged));

		private static void OnLayerTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Legend)d).OnLayerTemplateChanged(e.NewValue as DataTemplate);
		}

		private void OnLayerTemplateChanged(DataTemplate newDataTemplate)
		{
			_legendTree.LayerTemplate = newDataTemplate;

		}
		#endregion

		#region ReverseLayersOrder

		/// <summary>
		/// Gets or sets a property specifying whether the order the layers are 
		/// processed in should be reversed.
		/// </summary>
		public bool ReverseLayersOrder
		{
			get { return (bool)GetValue(ReverseLayersOrderProperty); }
			set { SetValue(ReverseLayersOrderProperty, value); }
		}

		/// /// <summary>
		/// Identifies the <see cref="ReverseLayersOrder"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ReverseLayersOrderProperty =
			DependencyProperty.Register("ReverseLayersOrder", typeof(bool), typeof(Legend), new PropertyMetadata(false, OnReverseLayersOrderChanged));

		private static void OnReverseLayersOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Legend)d).OnReverseLayersOrderChanged((bool)e.NewValue);
		}

		private void OnReverseLayersOrderChanged(bool newReverseLayersOrder)
		{
			_legendTree.ReverseLayersOrder = newReverseLayersOrder;

		}
		#endregion

		#region MapLayerTemplate

		/// <summary>
		/// Gets or sets the map layer template
		/// </summary>
		public DataTemplate MapLayerTemplate
		{
			get { return (DataTemplate)GetValue(MapLayerTemplateProperty); }
			set { SetValue(MapLayerTemplateProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="LegendItemTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MapLayerTemplateProperty =
			DependencyProperty.Register("MapLayerTemplate", typeof(DataTemplate), typeof(Legend), new PropertyMetadata(null, OnMapLayerTemplateChanged));

		private static void OnMapLayerTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Legend)d).OnMapLayerTemplateChanged(e.NewValue as DataTemplate);
		}

		private void OnMapLayerTemplateChanged(DataTemplate newDataTemplate)
		{
			_legendTree.MapLayerTemplate = newDataTemplate;
		}
		#endregion

		#region Refresh
		/// <summary>
		/// Refreshes the legend control.
		/// </summary>
		/// <remarks>Note : In most cases, the control is always up to date without calling the refresh method.</remarks>
		public void Refresh()
		{
			_legendTree.Refresh();
		}
		#endregion

		#region public override void OnApplyTemplate()
		/// <summary>
		/// Invoked whenever application code or internal processes (such as a 
		/// rebuilding layout pass) call ApplyTemplate. In simplest terms, this
		/// means the method is called just before a UI element displays in your
		/// app. Override this method to influence the default post-template 
		/// logic of a class.
		/// </summary>
#if NETFX_CORE
		protected
#else
		public
#endif
		override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (!_isLoaded)
			{
				_isLoaded = true;

				if (CompatUtility.IsDesignMode && (Layers == null || !Layers.Any()))
				{
					if (_legendTree.LayerItems == null)
					{
						// Create a basic hierarchy for design :  Map Layer -> SubLayer -> LegendItemViewModel
						var legendItem1 = new LegendItemViewModel
						{
							Label = "LegendItem1",
							Symbol = new SimpleMarkerSymbol { Style = SimpleMarkerStyle.Circle, Color = Colors.Red }
						};
						var legendItem2 = new LegendItemViewModel
						{
							Label = "LegendItem2",
							Symbol = new SimpleMarkerSymbol { Style = SimpleMarkerStyle.Diamond, Color = Colors.Green }
						};

						var layerItem = new LayerItemViewModel
						{
							Label = "LayerItem",
							LegendItems = new ObservableCollection<LegendItemViewModel> { legendItem1, legendItem2 }
						};

						var mapLayerItem = new LayerItemViewModel
						{
							Label = "MapLayerItem",
							LayerType = MapLayerItem.MapLayerType,
							LayerItems = new ObservableCollection<LayerItemViewModel> { layerItem },
						};

						_legendTree.LayerItems = new ObservableCollection<LayerItemViewModel> { mapLayerItem };
					}

				}
				else
				{
					// Initialize the Map now that all parameters are well known
					_legendTree.Layers = Layers;
				}
			}
		}

		#endregion

		#region Event Refreshed
		/// <summary>
		/// Occurs when the legend is refreshed. 
		/// Give the opportunity for an application to add or remove legend items.
		/// </summary>
		public event EventHandler<RefreshedEventArgs> Refreshed;

		private void OnRefreshed(object sender, RefreshedEventArgs args)
		{
			EventHandler<RefreshedEventArgs> refreshed = Refreshed;

			if (refreshed != null)
			{
				refreshed(this, args);
			}
		}
		#endregion

		#region class RefreshedEventArgs
		/// <summary>
		/// Legend Event Arguments used when the legend is refreshed.
		/// </summary>
		public sealed class RefreshedEventArgs : EventArgs
		{
			internal RefreshedEventArgs(LayerItemViewModel layerItem, Exception ex)
			{
				LayerItem = layerItem;
				Error = ex;
			}

			/// <summary>
			/// Gets the layer item being refreshed.
			/// </summary>
			/// <value>The layer item.</value>
			public LayerItemViewModel LayerItem { get; internal set; }

			/// <summary>
			/// Gets a value that indicates which error occurred during the legend refresh.
			/// </summary>
			/// <value>An System.Exception instance, if an error occurred during the refresh; otherwise null.</value>
			public Exception Error { get; internal set; }
		}
		#endregion
	}
}