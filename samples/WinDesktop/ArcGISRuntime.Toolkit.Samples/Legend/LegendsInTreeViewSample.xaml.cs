using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;

namespace ArcGISRuntime.Toolkit.Samples.Desktop.Legend
{
	/// <summary>
	/// Demonstrates how to show layers in a treeview and show legend for each layer on the map view.
	/// </summary>
	/// <title>Legends in Treeview</title>
	/// <category>Toolkit</category>
	/// <subcategory>Legend</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public partial class LegendsInTreeViewSample : UserControl
	{
		public LegendsInTreeViewSample()
		{
			InitializeComponent();
		}

		private void MyMapView_OnLayerLoaded(object sender, LayerLoadedEventArgs e)
		{
			// Zoom to water network
			var layer = e.Layer as ArcGISDynamicMapServiceLayer;
			if (layer != null)
			{
				var extent = layer.ServiceInfo.InitialExtent ?? layer.ServiceInfo.InitialExtent;
				if (extent != null)
				{
					// If extent is not in the same spatial reference than map, reproject it
					if (!SpatialReference.Equals(extent.SpatialReference, MyMapView.SpatialReference))
						extent = GeometryEngine.Project(extent, MyMapView.SpatialReference) as Envelope;
					if (extent != null)
					{
						extent = extent.Expand(0.5);
						MyMapView.SetView(extent);
					}
				}
			}
		}	
	}

	public class EnumeratorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Esri.ArcGISRuntime.Layers.Layer && targetType == typeof(IEnumerable<Esri.ArcGISRuntime.Layers.Layer>))
			{
				return new Esri.ArcGISRuntime.Layers.Layer[] { (Esri.ArcGISRuntime.Layers.Layer)value };
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
