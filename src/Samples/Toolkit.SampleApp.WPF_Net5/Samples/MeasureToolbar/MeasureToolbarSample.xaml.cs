using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.MeasureToolbar
{
    /// <summary>
    /// Interaction logic for MeasureToolbarSample.xaml
    /// </summary>
    public partial class MeasureToolbarSample : UserControl
    {
        public MeasureToolbarSample()
        {
            InitializeComponent();

            // Builds a map with different types of layer and graphic that can be identified for measure.

            var map = new Map(Basemap.CreateLightGrayCanvasVector());

            map.OperationalLayers.Add(new FeatureLayer(new Uri("https://services2.arcgis.com/ZQgQTuoyBrtmoGdP/ArcGIS/rest/services/Mobile_Data_Collection_WFL1/FeatureServer/1")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("https://services2.arcgis.com/ZQgQTuoyBrtmoGdP/ArcGIS/rest/services/Mobile_Data_Collection_WFL1/FeatureServer/0")));

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
