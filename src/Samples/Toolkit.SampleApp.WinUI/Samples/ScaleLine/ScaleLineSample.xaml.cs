using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.ScaleLine
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScaleLineSample : Page
    {
        public ScaleLineSample()
        {
            this.InitializeComponent();

            var map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
            map.InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.WebMercator), 50000000);
            MainMapView.Map = map;
        }
    }
}
