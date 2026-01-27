using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Toolkit.UITests.WinUI.Puppet.TestPages;

public sealed partial class ScaleLines : UserControl
{
    public ScaleLines()
    {
        InitializeComponent();

        var map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        map.InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.WebMercator), 50000000);
        MainMapView.Map = map;
    }

    private void UpdateViewpoint_Click(object sender, RoutedEventArgs e)
    {
        var scale = double.Parse(ScaleTextBox.Text);
        var latitude = double.Parse(LatitudeTextBox.Text);

        var center = new MapPoint(0, latitude, SpatialReferences.Wgs84);
        MainMapView.SetViewpoint(new Viewpoint(center, scale));
    }
}
