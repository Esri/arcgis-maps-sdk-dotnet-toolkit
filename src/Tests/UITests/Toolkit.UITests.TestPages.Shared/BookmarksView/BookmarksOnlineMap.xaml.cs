using System;
using System.Globalization;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Toolkit.UITests.App.TestPages;

public partial class BookmarksOnlineMap : TestPage
{
    private static readonly Uri MapUri = new("https://arcgisruntime.maps.arcgis.com/home/item.html?id=e50fafe008ac4ce4ad2236de7fd149c3");

    public BookmarksOnlineMap()
    {
        InitializeComponent();
        MainMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(MapUri);
        BookmarksView.GeoView = MainMapView;
        BookmarksView.BookmarkSelected += BookmarksView_BookmarkSelected;
        MainMapView.NavigationCompleted += MainMapView_NavigationCompleted;
    }

    private void BookmarksView_BookmarkSelected(object? sender, Bookmark bookmark)
    {
        SelectedBookmarkText.Text = bookmark.Name;
    }

    private void MainMapView_NavigationCompleted(object? sender, EventArgs e)
    {
        if (MainMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry is MapPoint center)
        {
            MapCenterText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.0},{1:0.0}", center.X, center.Y);
        }
    }
}