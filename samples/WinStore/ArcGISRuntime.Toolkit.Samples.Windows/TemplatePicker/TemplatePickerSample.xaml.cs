using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace ArcGISRuntime.Toolkit.Samples.Windows.TemplatePicker
{
	/// <summary>
	/// Demonstrates how to use TemplatePicker control.
	/// </summary>
	/// <title>TemplatePicker</title>
	/// <category>Toolkit</category>
	/// <subcategory>TemplatePicker</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class TemplatePickerSample : Page
	{
		public TemplatePickerSample()
		{
			InitializeComponent();
		}

		private async void MyTemplatePicker_TemplatePicked(object sender,  Esri.ArcGISRuntime.Toolkit.Controls.TemplatePicker.TemplatePickedEventArgs e)
		{
			try
			{
				TemplateName.Text = e.FeatureTemplate.Name;
				TargetedLayer.Text = e.Layer.DisplayName;
				TemplateDescription.Text = e.FeatureTemplate.Description;

				GeometryType geometryType = GeometryType.Unknown;
				var gdbFeatureTable = e.Layer.FeatureTable as ServiceFeatureTable;
				if (gdbFeatureTable != null && gdbFeatureTable.ServiceInfo != null)
					geometryType = gdbFeatureTable.ServiceInfo.GeometryType;

				Esri.ArcGISRuntime.Symbology.Symbol symbol = null;

				// Get symbol from the renderer if that is defined
				if (e.Layer.Renderer != null)
				{
					var g = new Graphic(e.FeatureTemplate.Prototype.Attributes ??
						Enumerable.Empty<KeyValuePair<string, object>>());

					symbol = e.Layer.Renderer.GetSymbol(g);
					SelectedSymbol.Source = await symbol.CreateSwatchAsync(32, 32, 96, Colors.Transparent, geometryType);
				}

				// Define what we shape we request from the editor
				DrawShape requestedShape = DrawShape.Point;
				if (e.FeatureTemplate.DrawingTool == FeatureEditTool.Polygon)
					requestedShape = DrawShape.Polygon;
				if (e.FeatureTemplate.DrawingTool == FeatureEditTool.Line)
					requestedShape = DrawShape.Polyline;

				// Request location for the new feature
				var addedGeometry = await MyMapView.Editor.RequestShapeAsync(requestedShape, symbol);

				// Add new feature to the MapView. Note that this doesn't commit changes automatically to the service
				var id = await gdbFeatureTable.AddAsync(e.FeatureTemplate.Prototype.Attributes, addedGeometry);
			}
			catch (TaskCanceledException) { } // Editing canceled
			catch (Exception exception)
			{
				var _x = new MessageDialog(string.Format("Error occured : {0}", exception.ToString()), "Sample error").ShowAsync();
			}
		}

		private void MyMapView_LayerLoaded(object sender, LayerLoadedEventArgs e)
		{
			if (e.LoadError == null)
				return;

			Debug.WriteLine(string.Format("Error while loading layer : {0} - {1}", e.Layer.ID, e.LoadError.Message));
		}
	}
}
