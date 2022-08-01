using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
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
            Map map = new Map(BasemapStyle.ArcGISLightGray)
            {
                InitialViewpoint = new Viewpoint(new Envelope(-178, 17.8, -65, 71.4, SpatialReference.Create(4269)))
            };
            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://nowcoast.noaa.gov/arcgis/rest/services/nowcoast/radar_meteo_imagery_nexrad_time/MapServer")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("https://services2.arcgis.com/ZQgQTuoyBrtmoGdP/arcgis/rest/services/SF_311_Incidents/FeatureServer/0")));
            return map;
        }

        private void Grid_SizeChanged(object sender, EventArgs e)
        {
            // Place legend on left size when view is wide, otherwise below mapview
            if(((Grid)sender).Width > ((Grid)sender).Height)
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