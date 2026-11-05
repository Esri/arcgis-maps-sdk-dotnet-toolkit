#if MAUI_APP
using ClickEventArgs = System.EventArgs;
#elif WINUI_APP
using Microsoft.UI.Xaml;
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
#elif WPF_APP
using System.Windows;
using ClickEventArgs = System.Windows.RoutedEventArgs;
#endif
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.UITests.App.TestPages;

public partial class BookmarksTemplateMap : TestPage
{
    public BookmarksTemplateMap()
    {
        InitializeComponent();

        MainMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(SpatialReferences.Wgs84)
        {
            InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.Wgs84), 100000),
        };
        BookmarksView.GeoView = MainMapView;
        BookmarksView.BookmarksOverride =
        [
            new Bookmark("Red bookmark", new Viewpoint(10, 10, 1000000)),
            new Bookmark("Green bookmark", new Viewpoint(20, 30, 1000000)),
            new Bookmark("Blue bookmark", new Viewpoint(-20, -30, 1000000)),
        ];
    }

    private void SetRuntimeTemplateButton_Click(object sender, ClickEventArgs e)
    {
        BookmarksView.ItemTemplate = Resources["RuntimeItemTemplate"] as DataTemplate;
#if WPF_APP
        BookmarksView.ItemContainerStyle = Resources["RuntimeItemContainerStyle"] as Style;
#endif
    }
}