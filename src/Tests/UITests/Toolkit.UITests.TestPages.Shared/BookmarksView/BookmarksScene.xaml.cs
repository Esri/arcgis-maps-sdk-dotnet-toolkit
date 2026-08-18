using System;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;

namespace Toolkit.UITests.App.TestPages;

public partial class BookmarksScene : TestPage
{
    private bool _navigationCompleted;
    private string? _selectedBookmarkName;

    public BookmarksScene()
    {
        InitializeComponent();

        var initialPoint = new MapPoint(0, 0, 0, SpatialReferences.Wgs84);
        MainSceneView.Scene = new Scene
        {
            InitialViewpoint = CreateCameraViewpoint(initialPoint),
        };

        var graphicsOverlay = new GraphicsOverlay();
        graphicsOverlay.Graphics.Add(CreateSceneGraphic(10, 10, System.Drawing.Color.Red));
        graphicsOverlay.Graphics.Add(CreateSceneGraphic(30, 20, System.Drawing.Color.Green));
        graphicsOverlay.Graphics.Add(CreateSceneGraphic(-30, -20, System.Drawing.Color.Blue));
        (MainSceneView.GraphicsOverlays ?? throw new InvalidOperationException("The scene view graphics overlay collection is unavailable.")).Add(graphicsOverlay);

        BookmarksView.GeoView = MainSceneView;
        BookmarksView.BookmarksOverride =
        [
            new Bookmark("Red scene bookmark", CreateCameraViewpoint(new MapPoint(10, 10, 0, SpatialReferences.Wgs84))),
            new Bookmark("Green scene bookmark", CreateCameraViewpoint(new MapPoint(30, 20, 0, SpatialReferences.Wgs84))),
            new Bookmark("Blue scene bookmark", CreateCameraViewpoint(new MapPoint(-30, -20, 0, SpatialReferences.Wgs84))),
        ];
        BookmarksView.BookmarkSelected += BookmarksView_BookmarkSelected;
        MainSceneView.NavigationCompleted += MainSceneView_NavigationCompleted;
        MainSceneView.DrawStatusChanged += MainSceneView_DrawStatusChanged;
    }

    private void BookmarksView_BookmarkSelected(object? sender, Bookmark bookmark)
    {
        _navigationCompleted = false;
        _selectedBookmarkName = bookmark.Name;
        SelectedBookmarkText.Text = _selectedBookmarkName;
        SceneNavigationCompletedText.Text = string.Empty;
    }

    private void MainSceneView_NavigationCompleted(object? sender, EventArgs e)
    {
        _navigationCompleted = true;
        ReportReady();
    }

    private void MainSceneView_DrawStatusChanged(object? sender, DrawStatusChangedEventArgs e)
    {
        if (e.Status == DrawStatus.Completed)
            ReportReady();
    }

    private void ReportReady()
    {
        if (_navigationCompleted && MainSceneView.DrawStatus == DrawStatus.Completed && _selectedBookmarkName is not null)
            SceneNavigationCompletedText.Text = $"Ready: {_selectedBookmarkName}";
    }

    private static Viewpoint CreateCameraViewpoint(MapPoint point) => new(point, new Camera(point, 1000000, 0, 45, 0));

    private static Graphic CreateSceneGraphic(double longitude, double latitude, System.Drawing.Color color)
    {
        var point = new MapPoint(longitude, latitude, 0, SpatialReferences.Wgs84);
        return new Graphic(point, SimpleMarkerSceneSymbol.CreateSphere(color, 100000));
    }
}