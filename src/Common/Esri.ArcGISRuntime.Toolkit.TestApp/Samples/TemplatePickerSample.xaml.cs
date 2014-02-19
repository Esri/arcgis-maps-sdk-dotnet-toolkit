// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#elif SILVERLIGHT
using System.Windows;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.TestApp.Samples
{
	/// <summary>
	/// Interaction logic for TemplatePickerSample.xaml
	/// </summary>
	public partial class TemplatePickerSample
	{
		public TemplatePickerSample()
		{
			InitializeComponent();
			ObjectTracker.Track(this);
			ObjectTracker.Track(MyTemplatePicker);
			ObjectTracker.Track(MyMapView);
#if !WINDOWS_PHONE
			MyMap.InitialExtent = new Envelope(-15000000, 0, -5000000, 10000000, SpatialReferences.WebMercator);
#endif
		}

		private void TemplatePicker_OnTemplatePicked(object sender, Controls.TemplatePicker.TemplatePickedEventArgs e)
		{
			string name = GetLayerName(e.Layer);
			string message = "Event TemplatePicked --> Type='" + (e.FeatureType == null ? "null" : e.FeatureType.Name) + "'  Template='" + e.FeatureTemplate.Name + "'  Layer='" + name + "'";
			LogMessage(message);
		}

		private static string GetLayerName(Layer layer)
		{
			string name = null;
			var featureLayer = layer as FeatureLayer;
			if (featureLayer != null)
			{
				name = featureLayer.FeatureTable.Name;
				if (string.IsNullOrEmpty(name) && featureLayer.FeatureTable is GeodatabaseFeatureServiceTable) // not initialized yet
					name = ((GeodatabaseFeatureServiceTable)featureLayer.FeatureTable).ServiceUri;
			}
			if (string.IsNullOrEmpty(name))
				name = layer.GetType().Name; // fallback value
			return name;
		}

		private void LogMessage(string message)
		{
			StatusText.Text += "\n" + message;
#if SILVERLIGHT
			StatusTextScrollViewer.UpdateLayout();
			StatusTextScrollViewer.ScrollToVerticalOffset(double.MaxValue);
#elif NETFX_CORE
			var grid = (Grid)VisualTreeHelper.GetChild(StatusText, 0);
			for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
			{
				var scrollViewer = VisualTreeHelper.GetChild(grid, i) as ScrollViewer;
				if (scrollViewer != null)
				{
					scrollViewer.ChangeView(null, scrollViewer.ExtentHeight, null);
					break;
				}
			}
#else
			StatusText.ScrollToEnd();
#endif
		}

#if NETFX_CORE
		private void RemoveLayer_OnClick(object sender, RoutedEventArgs e)
#else
		private void RemoveLayer_OnClick(object sender, EventArgs e)
#endif
		{
			if (MyTemplatePicker.Layers.Any())
			{
				var layer = MyTemplatePicker.Layers.First();
				((ObservableCollection<Layer>)MyTemplatePicker.Layers).Remove(layer);
				LogMessage("Removed Layer " + GetLayerName(layer));
			}
			else
			{
				LogMessage("No more layer to remove");
			}
		}

		static int _index;
#if NETFX_CORE
		private async void AddLayer_OnClick(object sender, RoutedEventArgs e)
#else
		private async void AddLayer_OnClick(object sender, EventArgs e)
#endif
		{
			string url = "http://sampleserver6.arcgisonline.com/arcgis/rest/services/Military/FeatureServer/" + (_index + 2);
			_index = ++_index % 5;
			var ft = new GeodatabaseFeatureServiceTable(new Uri(url)) { UseAdvancedSymbology = false}; // todo should work with AdvancedSymbology as well
			var featureLayer = new FeatureLayer(ft);
			((ObservableCollection<Layer>)MyTemplatePicker.Layers).Add(featureLayer);
			await featureLayer.InitializeAsync();
			featureLayer.DisplayName = featureLayer.FeatureTable.ServiceInfo.Name;
			LogMessage("Added Layer " + GetLayerName(featureLayer));
			featureLayer.MinScale = MyMapView.Scale * 4;
			featureLayer.MaxScale = MyMapView.Scale / 4;
			ObjectTracker.Track(featureLayer);
		}

#if NETFX_CORE
		private void AddStreetMapLayer_OnClick(object sender, RoutedEventArgs e)
#else
		private void AddStreetMapLayer_OnClick(object sender, EventArgs e)
#endif
		{
			var layer = new OpenStreetMapLayer { Opacity = 0.5, DisplayName = "OpenStreetMapLayer" };
			((ObservableCollection<Layer>) MyTemplatePicker.Layers).Add(layer);
			LogMessage("Added Layer " + GetLayerName(layer));
		}

#if NETFX_CORE
		private void InitNewLayers_OnClick(object sender, RoutedEventArgs e)
#else
		private void InitNewLayers_OnClick(object sender, EventArgs e)
#endif
		{
			MyTemplatePicker.Layers = new ObservableCollection<Layer>();
			LogMessage("TemplatePicker.Layers initialized with a new collection not displayed in the map");
			MyMapView.Visibility = Visibility.Collapsed;
			MyTemplatePicker.Scale = double.NaN;
		}


#if NETFX_CORE
		private void ClearLayers_OnClick(object sender, RoutedEventArgs e)
#else
		private void ClearLayers_OnClick(object sender, EventArgs e)
#endif
		{
			var coll = MyTemplatePicker.Layers as ObservableCollection<Layer>;
			if (coll != null)
			{
				coll.Clear();
				LogMessage("TemplatePicker.Layers cleared");
			}
		}

#if NETFX_CORE
		private void SwitchLayers_OnClick(object sender, RoutedEventArgs e)
#else
		private void SwitchLayers_OnClick(object sender, EventArgs e)
#endif
		{
			var coll = MyTemplatePicker.Layers as ObservableCollection<Layer>;
			if (coll != null && coll.Count >= 2)
			{
#if SILVERLIGHT
				var layer = coll.First();
				coll.RemoveAt(0);
				coll.Insert(1, layer);
#else
				coll.Move(0, 1);
#endif
				LogMessage("First layer moved after second");
			}
		}


#if NETFX_CORE
		private void TestMemoryLeak(object sender, RoutedEventArgs e)
#else
		private void TestMemoryLeak(object sender, EventArgs e)
#endif
		{
			// Create a template picker that should be released immediatly since no more referenced
			var templatePicker = new Controls.TemplatePicker {Layers = MyTemplatePicker.Layers};
			ObjectTracker.Track(templatePicker);
		}


#if NETFX_CORE
		private void GarbageCollect(object sender, RoutedEventArgs e)
#else
		private void GarbageCollect(object sender, EventArgs e)
#endif
		{
			LogMessage(ObjectTracker.GarbageCollect());
		}
	}
}
