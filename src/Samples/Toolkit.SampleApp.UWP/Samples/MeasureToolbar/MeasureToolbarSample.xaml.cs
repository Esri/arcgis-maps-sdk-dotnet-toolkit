using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.MeasureToolbar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MeasureToolbarSample : Page
    {
        public MeasureToolbarSample()
        {
            this.InitializeComponent();

            // Builds a map with different types of layer and graphic that can be identified for measure.

            var map = new Map(Basemap.CreateLightGrayCanvasVector());

            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/USA/MapServer")));

            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/Wildfire/FeatureServer/2")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/Wildfire/FeatureServer/1")));

            mapView.Map = map;
            AddRandomGraphics();
        }

        private void AddRandomGraphics()
        {
            var random = new Random();
            var overlay = mapView.GraphicsOverlays.FirstOrDefault();
            if (overlay != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    var mp = new MapPoint(random.Next(-180, 180), random.Next(-90, 90), SpatialReferences.Wgs84);
                    var geometry = GeometryEngine.Buffer(mp, random.Next(1, 10));
                    overlay.Graphics.Add(new Graphic(geometry));
                }
            }
        }
    }
}
