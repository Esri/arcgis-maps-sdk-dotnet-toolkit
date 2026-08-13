#if MAUI_APP
using ClickEventArgs = System.EventArgs;
using MapView = Esri.ArcGISRuntime.Maui.MapView;
using SceneView = Esri.ArcGISRuntime.Maui.SceneView;
#elif WINUI_APP
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Esri.ArcGISRuntime.UI.Controls;
#elif WPF_APP
using ClickEventArgs = System.Windows.RoutedEventArgs;
using Esri.ArcGISRuntime.UI.Controls;
#endif
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.UITests.App.TestPages;

public partial class BookmarksUpdates : TestPage
{
    private readonly Esri.ArcGISRuntime.Mapping.Map _mapB;
    private readonly ObservableCollection<Bookmark> _overrides;

    public BookmarksUpdates()
    {
        InitializeComponent();

        var mapA = CreateMap("Map A bookmark");
        _mapB = CreateMap("Map B bookmark");
        _overrides =
        [
            new Bookmark("Override bookmark", new Viewpoint(10, 10, 1000000)),
        ];

        MainMapView.Map = mapA;
        MainSceneView.Scene = CreateScene("Scene bookmark");
        BookmarksView.GeoView = MainMapView;
        BookmarksView.BookmarkSelected += BookmarksView_BookmarkSelected;
    }

    private void AddDocumentBookmarkButton_Click(object sender, ClickEventArgs e)
    {
        if (BookmarksView.GeoView is MapView mapView && mapView.Map is { } map)
        {
            map.Bookmarks.Add(new Bookmark("Added map bookmark", new Viewpoint(20, 20, 1000000)));
        }
    }

    private void BookmarksView_BookmarkSelected(object? sender, Bookmark bookmark)
    {
        if (bookmark.Name == "Added map bookmark")
        {
            bookmark.Name = "Renamed map bookmark";
        }
    }

    private void UseOverrideButton_Click(object sender, ClickEventArgs e)
    {
        BookmarksView.BookmarksOverride = _overrides;
    }

    private void AddOverrideButton_Click(object sender, ClickEventArgs e)
    {
        _overrides.Add(new Bookmark("Added override bookmark", new Viewpoint(-20, -20, 1000000)));
    }

    private void ClearOverrideButton_Click(object sender, ClickEventArgs e)
    {
        BookmarksView.BookmarksOverride = null;
    }

    private void UseMapBButton_Click(object sender, ClickEventArgs e)
    {
        MainMapView.Map = _mapB;
    }

    private void UseSceneButton_Click(object sender, ClickEventArgs e)
    {
        BookmarksView.GeoView = MainSceneView;
    }

    private static Esri.ArcGISRuntime.Mapping.Map CreateMap(string bookmarkName)
    {
        var map = new Esri.ArcGISRuntime.Mapping.Map();
        map.Bookmarks.Add(new Bookmark(bookmarkName, new Viewpoint(0, 0, 1000000)));
        return map;
    }

    private static Scene CreateScene(string bookmarkName)
    {
        var scene = new Scene();
        scene.Bookmarks.Add(new Bookmark(bookmarkName, new Viewpoint(0, 0, 1000000)));
        return scene;
    }
}