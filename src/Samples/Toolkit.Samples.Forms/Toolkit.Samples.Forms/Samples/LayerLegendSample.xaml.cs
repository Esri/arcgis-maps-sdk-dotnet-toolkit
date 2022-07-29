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
    [Obsolete]
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "LayerLegend", Description = "[Obsolete - see Legend]")]
    public partial class LayerLegendSample : ContentPage
	{
		public LayerLegendSample()
		{
			InitializeComponent ();
            mapView.Map = CreateMap();
            layerLegend.LayerContent = mapView.Map.OperationalLayers[0];
        }

        private Map CreateMap()
        {
            Map map = new Map(Basemap.CreateLightGrayCanvasVector())
            {
                InitialViewpoint = new Viewpoint(new Envelope(569614.225, 6847121.683, 570198.333, 6846604.317, SpatialReferences.WebMercator))
            };
            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://basisregistraties.arcgisonline.nl/arcgis/rest/services/DKK/DKKv4/MapServer")));
            return map;
        }

        private void Grid_SizeChanged(object sender, EventArgs e)
        {
            // Place legend on left size when view is wide, otherwise below mapview
            if(((Grid)sender).Width > ((Grid)sender).Height)
            {
                Grid.SetColumnSpan(mapView, 1);
                Grid.SetColumnSpan(layerLegend, 1);
                Grid.SetColumn(mapView, 1);
                Grid.SetRow(layerLegend, 0);
                Grid.SetRowSpan(mapView, 2);
                Grid.SetRowSpan(layerLegend, 2);
                if (((Grid)sender).Width > 600)
                    layerLegend.WidthRequest = 300;
                else
                    layerLegend.ClearValue(WidthRequestProperty);
            }
            else
            {
                layerLegend.ClearValue(WidthRequestProperty);
                Grid.SetColumnSpan(mapView, 2);
                Grid.SetColumnSpan(layerLegend, 2);
                Grid.SetColumn(mapView, 0);
                Grid.SetRow(layerLegend, 1);
                Grid.SetRowSpan(mapView, 1);
                Grid.SetRowSpan(layerLegend, 1);
            }
        }

        private void FilterByScaleRange_Toggled(object sender, ToggledEventArgs e)
        {

        }
    }
}