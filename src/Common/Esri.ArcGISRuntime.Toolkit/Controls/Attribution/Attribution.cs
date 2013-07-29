// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows.Controls;
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
	/// <summary>
	/// The Attribution Control displays Copyright information for Layers that have the IAttribution 
	/// Interface implemented.
	/// </summary>
	public class Attribution : Control
	{
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribution"/> class.
		/// </summary>
		public Attribution()
		{
#if NETFX_CORE || WINDOWS_PHONE
			DefaultStyleKey = typeof(Attribution);
#endif
		}

#if !NETFX_CORE && !WINDOWS_PHONE
		static Attribution()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Attribution),
				new FrameworkPropertyMetadata(typeof(Attribution)));
		}
#endif
		#endregion

		#region DependencyProperty Layers

		/// <summary>
		/// Gets or sets the layers to display attribution for.
		/// </summary>
		public IEnumerable<Layer> Layers
		{
			get { return (IEnumerable<Layer>)GetValue(LayersProperty); }
			set { SetValue(LayersProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Layers"/> Dependency Property.
		/// </summary>
		public static readonly DependencyProperty LayersProperty =
			DependencyProperty.Register("Layers", typeof(IEnumerable<Layer>), typeof(Attribution), new PropertyMetadata(null, OnLayersPropertyChanged));

		private static void OnLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Attribution)
				(d as Attribution).OnLayersPropertyChanged(e.OldValue as IEnumerable<Layer>, e.NewValue as IEnumerable<Layer>);
		}

		private void OnLayersPropertyChanged(IEnumerable<Layer> oldLayers, IEnumerable<Layer> newLayers)
		{
			if (oldLayers != null)
				DetachLayersHandler(oldLayers);
			if (newLayers != null)
				AttachLayersHandler(newLayers);
			UpdateAttributionItems();
		}

		#endregion

		#region Items Dependency Property

		/// <summary>
		/// Gets the items to display in attribution control.
		/// </summary>
		/// <value>
		/// The items.
		/// </value>
		public IEnumerable<string> Items
		{
			get { return (IEnumerable<string>)GetValue(ItemsProperty); }
			internal set { SetValue(ItemsProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Items"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(IEnumerable<string>), typeof(Attribution), new PropertyMetadata(null));
		
		#endregion

		#region Layer Event Handlers

		private void DetachLayersHandler(IEnumerable<Layer> layers)
		{
			if (layers != null)
			{
				if(layers is INotifyCollectionChanged)
					(layers as INotifyCollectionChanged).CollectionChanged -= Layers_CollectionChanged;
				foreach (Layer layer in layers)
					DetachLayerHandler(layer);
			}
		}

		private void AttachLayersHandler(IEnumerable<Layer> layers)
		{
			if (layers != null)
			{
				if (layers is INotifyCollectionChanged)
					(layers as INotifyCollectionChanged).CollectionChanged += Layers_CollectionChanged;
				foreach (Layer layer in layers)
					AttachLayerHandler(layer);
			}
		}

		private void AttachLayerHandler(Layer layer)
		{
			layer.PropertyChanged += Layer_PropertyChanged;
		}

		private void DetachLayerHandler(Layer layer)
		{
			layer.PropertyChanged -= Layer_PropertyChanged;
		}

		private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CopyrightText" || e.PropertyName == "Visibility")
				UpdateAttributionItems();
		}


		private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			var oldItems = e.OldItems;
			System.Collections.IEnumerable newItems = e.NewItems;
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) //This only handles a Clear(), and not any other reset
				newItems = Layers;
			if (oldItems != null)
				foreach (var item in oldItems)
					DetachLayerHandler(item as Layer);
			if (newItems != null)
				foreach (var item in newItems)
					AttachLayerHandler(item as Layer);
			UpdateAttributionItems();
		}

		#endregion

		#region Private Methods

		private void UpdateAttributionItems()
		{
			if (Layers == null)
				Items = null;
			else
			{
				var visibleCopyrights = Layers.Where(layer => layer.Visibility == Visibility.Visible).OfType<ICopyright>();
				Items = visibleCopyrights.Select(cpr => cpr.CopyrightText).Where(cpr => !string.IsNullOrEmpty(cpr))
					.Select(cpr => cpr.Trim()).Distinct().ToList();
			}
		}

		#endregion
	}
}
