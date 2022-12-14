using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "Legend", Description = "Legend with MapView sample")]
    public partial class LegendSample : ContentPage
	{
		public LegendSample ()
		{
			InitializeComponent ();
            mapView.Map = CreateMap();
        }

        private Map CreateMap()
        {
            Map map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"))
            {
                InitialViewpoint = new Viewpoint(new Envelope(-178, 17.8, -65, 71.4, SpatialReference.Create(4269)))
            };
            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Census/MapServer")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/SF311/FeatureServer/0")));
            return map;
        }

        private void Grid_SizeChanged(object? sender, EventArgs e)
        {
            // Place legend on left size when view is wide, otherwise below mapview
            if(sender is Grid sg && sg.Width > sg.Height)
            {
                Grid.SetColumnSpan(mapView, 1);
                Grid.SetColumnSpan(legend, 1);
                Grid.SetColumn(mapView, 1);
                Grid.SetRow(legend, 0);
                Grid.SetRowSpan(mapView, 2);
                Grid.SetRowSpan(legend, 2);
                if (((Grid)sender).Width > 600)
                    legend.WidthRequest = 300;
                else
                    legend.ClearValue(WidthRequestProperty);
            }
            else
            {
                legend.ClearValue(WidthRequestProperty);
                Grid.SetColumnSpan(mapView, 2);
                Grid.SetColumnSpan(legend, 2);
                Grid.SetColumn(mapView, 0);
                Grid.SetRow(legend, 1);
                Grid.SetRowSpan(mapView, 1);
                Grid.SetRowSpan(legend, 1);
            }
        }
    }
}