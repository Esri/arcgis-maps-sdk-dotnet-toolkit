#if MAUI_APP
using ClickEventArgs = System.EventArgs;
#elif WINUI_APP
using System;
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
#elif WPF_APP
using System.Globalization;
using ClickEventArgs = System.Windows.RoutedEventArgs;
#endif
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime;

namespace Toolkit.UITests.App.TestPages;

public partial class SearchViewControlMap : TestPage
{
    public SearchViewControlMap()
    {
        InitializeComponent();

        var map = new Esri.ArcGISRuntime.Mapping.Map(BasemapStyle.ArcGISImagery);
        MyMapView.Map = map;
    }

    private void UpdateViewpointExtentToUSA_Click(object sender, ClickEventArgs e)
    {
        UpdateViewpoint_Click(50000000, -115, 37);
    }

    private void UpdateViewpointExtentToOntario_Click(object sender, ClickEventArgs e)
    {
        UpdateViewpoint_Click(60000, -117.602000, 34.055845);
    }

    private void UpdateViewpointExtentToColorado_Click(object sender, ClickEventArgs e)
    {
        UpdateViewpoint_Click(3000000, -105.143243, 38.888975);
    }

    private void UpdateViewpoint_Click(double scale, double longitude, double latitude)
    {
        var center = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
        MyMapView.SetViewpoint(new Viewpoint(center, scale));
    }
}