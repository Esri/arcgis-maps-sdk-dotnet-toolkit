using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ArcGISRuntime.Toolkit.Samples.Desktop.TemplatePicker
{
	/// <summary>
	/// Demonstrates how to show feature templates from selected layer.
	/// </summary>
	/// <title>TemplatePicker by layer</title>
	/// <category>Toolkit</category>
	/// <subcategory>TemplatePicker</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public partial class TemplatePickerWithLayerSelectionSample : UserControl
	{
		public TemplatePickerWithLayerSelectionSample()
		{
			InitializeComponent();
		}

		private async void MyTemplatePicker_TemplatePicked(object sender, Esri.ArcGISRuntime.Toolkit.Controls.TemplatePicker.TemplatePickedEventArgs e)
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

				Symbol symbol = null;

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

				Selection.Visibility = Visibility.Collapsed;
				SelectedInfo.Visibility = Visibility.Visible;

				// Request location for the new feature
				var addedGeometry = await MyMapView.Editor.RequestShapeAsync(requestedShape, symbol);

				// Add new feature to the MapView. Note that this doesn't commit changes automatically to the service
				var id = await gdbFeatureTable.AddAsync(e.FeatureTemplate.Prototype.Attributes, addedGeometry);
			}
			catch (TaskCanceledException) { } // Editing canceled
			catch (Exception exception)
			{
				MessageBox.Show(string.Format("Error occured : {0}", exception.ToString()), "Sample error");
			}

			Selection.Visibility = Visibility.Visible;
			SelectedInfo.Visibility = Visibility.Collapsed;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (MyMapView.Editor.IsActive && MyMapView.Editor.Cancel.CanExecute(null))
			{
				MyMapView.Editor.Cancel.Execute(null);
				Selection.Visibility = Visibility.Visible;
				SelectedInfo.Visibility = Visibility.Collapsed;
			}
		}
	}

	public class LayerToLayersCollectionConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return null;
			var layer = value as Layer;
			var layersCollection = new LayerCollection();
			layersCollection.Add(layer);

			return layersCollection;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class LayerCollectionFeatureLayersConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return null;

			var layers = value as LayerCollection;
			var layersCollection = new LayerCollection();

			foreach (var layer in layers.OfType<FeatureLayer>())
				layersCollection.Add(layer);

			return layersCollection;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
