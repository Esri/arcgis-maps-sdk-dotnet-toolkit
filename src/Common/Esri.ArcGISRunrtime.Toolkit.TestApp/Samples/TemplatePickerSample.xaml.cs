// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Data;
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
		}

		private void TemplatePicker_OnTemplateSelected(object sender, Controls.TemplatePicker.TemplateSelectedEventArgs e)
		{
			string name = GetLayerName(e.Layer);
			string message = "Event TemplateSelected--> Type='" + (e.FeatureType == null ? "null" : e.FeatureType.Name) + "'  Template='" + e.FeatureTemplate.Name + "'  Layer='" + name + "'";
			LogMessage(message);
		}

		private static string GetLayerName(Layer layer)
		{
			string name = null;
			var featureLayer = layer as FeatureLayer;
			if (featureLayer != null)
			{
				name = featureLayer.FeatureTable.Name;
				if (string.IsNullOrEmpty(name) && featureLayer.FeatureTable is GdbFeatureServiceTable) // not initialized yet
					name = ((GdbFeatureServiceTable) featureLayer.FeatureTable).ServiceUri;
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
					//scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
					scrollViewer.ChangeView(null, scrollViewer.ExtentHeight, null);
					break;
				}
			}
#else
			StatusText.ScrollToEnd();
#endif
		}

		private void RemoveLayer_OnClick(object sender, EventArgs e)
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
		private async void AddLayer_OnClick(object sender, EventArgs e)
		{
			string url = "http://sampleserver6.arcgisonline.com/arcgis/rest/services/Military/FeatureServer/" + (_index + 2);
			_index = ++_index % 5;
			var ft = new GdbFeatureServiceTable(new Uri(url)) { UseAdvancedSymbology = false}; // todo should work with AdvancedSymbology as well
			var featureLayer = new FeatureLayer(ft);
			((ObservableCollection<Layer>)MyTemplatePicker.Layers).Add(featureLayer);
			LogMessage("Added Layer " + GetLayerName(featureLayer));
			if (MyMapView.Visibility == Visibility.Collapsed)
			{
				// Initialize the GdbFeatureServiceTable else it will never be done since the layer is not in a mapview
				try
				{
					await ft.InitializeAsync();
					featureLayer.DisplayName = featureLayer.FeatureTable.Name;
				}
				catch
				{
				}
			}
			else
				featureLayer.MetadataChanged += InitDisplayName;
		}

		private void InitDisplayName(object sender, EventArgs eventArgs)
		{
			var featureLayer = sender as FeatureLayer;
			if (featureLayer != null)
			{
				featureLayer.MetadataChanged -= InitDisplayName;
				var table = featureLayer.FeatureTable as GdbFeatureServiceTable;
				if (table != null && table.IsInitialized)
					featureLayer.DisplayName = table.Name;
			}
		}

		private void AddStreetMapLayer_OnClick(object sender, EventArgs e)
		{
			var layer = new OpenStreetMapLayer { Opacity = 0.5, DisplayName = "OpenStreetMapLayer" };
			((ObservableCollection<Layer>) MyTemplatePicker.Layers).Add(layer);
			LogMessage("Added Layer " + GetLayerName(layer));
		}

		private void InitNewLayers_OnClick(object sender, EventArgs e)
		{
			MyTemplatePicker.Layers = new ObservableCollection<Layer>();
			LogMessage("TemplatePicker.Layers initialized with a new collection not displayed in the map");
			MyMapView.Visibility = Visibility.Collapsed;
			MyTemplatePicker.Scale = double.NaN;
		}


		private void ClearLayers_OnClick(object sender, EventArgs e)
		{
			var coll = MyTemplatePicker.Layers as ObservableCollection<Layer>;
			if (coll != null)
			{
				coll.Clear();
				LogMessage("TemplatePicker.Layers cleared");
			}
		}

		private void SwitchLayers_OnClick(object sender, EventArgs e)
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
	}
}
