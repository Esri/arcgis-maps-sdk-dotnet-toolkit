using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "ScaleLine", Description = "Demonstrates ScaleLine.")]
    public partial class ScaleLineSample : ContentPage
    {
        public ScaleLineSample()
        {
            InitializeComponent();

            var map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
            map.InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.WebMercator), 50000000);
            MainMapView.Map = map;
        }
    }
}