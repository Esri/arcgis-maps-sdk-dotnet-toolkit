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
    private const double WorldScale = 50000000;
    private const double DetailScale = 5000000;

    public ScaleLines()
    {
        InitializeComponent();

        var map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        map.InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.WebMercator), 50000000);
        MainMapView.Map = map;
    }

    private void WorldEquatorViewpoint_Click(object sender, ClickEventArgs e)
    {
        SetViewpoint(WorldScale, 0);
    }

    private void EquatorDetailViewpoint_Click(object sender, ClickEventArgs e)
    {
        SetViewpoint(DetailScale, 0);
    }

    private void HighLatitudeDetailViewpoint_Click(object sender, ClickEventArgs e)
    {
        SetViewpoint(DetailScale, 60);
    }

    private void SetViewpoint(double scale, double latitude)
    {
        var center = new MapPoint(0, latitude, SpatialReferences.Wgs84);
        MainMapView.SetViewpoint(new Viewpoint(center, scale));
    }
}