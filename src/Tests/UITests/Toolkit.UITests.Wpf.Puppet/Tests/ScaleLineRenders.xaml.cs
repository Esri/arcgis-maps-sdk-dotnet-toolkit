using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System.Windows.Controls;

namespace Toolkit.UITests.Wpf.Puppet.Tests;

/// <summary>
/// Interaction logic for ScaleLineRender.xaml
/// </summary>
public partial class ScaleLineRenders : UserControl
{
    public ScaleLineRenders()
    {
        InitializeComponent();

        var map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        map.InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.WebMercator), 50000000);
        MainMapView.Map = map;
    }

    private void UpdateViewpoint_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var scale = double.Parse(ScaleTextBox.Text);
        var latitude = double.Parse(LatitudeTextBox.Text);

        var center = new MapPoint(0, latitude, SpatialReferences.Wgs84);
        MainMapView.SetViewpoint(new Viewpoint(center, scale));
    }
}