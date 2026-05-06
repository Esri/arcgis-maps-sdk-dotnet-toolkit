#if MAUI_APP
using ClickEventArgs = System.EventArgs;
#elif WINUI_APP
using System;
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
#elif WPF_APP
using ClickEventArgs = System.Windows.RoutedEventArgs;
#endif
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;


namespace Toolkit.UITests.App.TestPages;

public partial class ScaleLines : TestPage
{
    public ScaleLines()
    {
        InitializeComponent();

        var map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        map.InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.WebMercator), 50000000);
        MainMapView.Map = map;
    }

    private void UpdateViewpoint_Click(object sender, ClickEventArgs e)
    {
        var scale = double.Parse(ScaleTextBox.Text);
        var latitude = double.Parse(LatitudeTextBox.Text);

        var center = new MapPoint(0, latitude, SpatialReferences.Wgs84);
        MainMapView.SetViewpoint(new Viewpoint(center, scale));
    }
}