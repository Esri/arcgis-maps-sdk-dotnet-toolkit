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

        MyMapView.ViewpointChanged += (_, _) => UpdateCoordinateAndScaleTextBoxes();
        MyMapView.Loaded += (_, _) => UpdateCoordinateAndScaleTextBoxes();
    }

    private void UpdateViewpoint_Click(object sender, ClickEventArgs e)
    {
        if (!double.TryParse(ScaleTextBox.Text, out var scale) ||
            !double.TryParse(LongitudeTextBox.Text, out var longitude) ||
            !double.TryParse(LatitudeTextBox.Text, out var latitude))
        {
            return;
        }

        var center = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
        MyMapView.SetViewpoint(new Viewpoint(center, scale));

        UpdateCoordinateAndScaleTextBoxes();
    }

    private void UpdateCoordinateAndScaleTextBoxes()
    {
        var visibleArea = MyMapView.VisibleArea;
        if (visibleArea is null)
        {
            return;
        }

        var center = visibleArea.Extent.GetCenter();
        var wgs84Center = GeometryEngine.Project(center, SpatialReferences.Wgs84) as MapPoint;

        if (wgs84Center is null)
        {
            return;
        }

        LongitudeTextBox.Text = wgs84Center.X.ToString("F6");
        LatitudeTextBox.Text = wgs84Center.Y.ToString("F6");
        ScaleTextBox.Text = MyMapView.MapScale.ToString("F0");

    }
}