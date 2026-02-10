using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.UITests.Maui.App.TestPages;

public partial class ScaleLines : ContentView
{
    public ScaleLines()
    {
        InitializeComponent();

        var map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        map.InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.WebMercator), 50000000);
        MainMapView.Map = map;
    }

    private void UpdateViewpoint_Clicked(object sender, EventArgs e)
    {
        var scale = double.Parse(ScaleTextBox.Text);
        var latitude = double.Parse(LatitudeTextBox.Text);

        var center = new MapPoint(0, latitude, SpatialReferences.Wgs84);
        MainMapView.SetViewpoint(new Viewpoint(center, scale));
    }
}