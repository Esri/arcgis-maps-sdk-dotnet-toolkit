using System;
using System.Globalization;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;

namespace Toolkit.UITests.App.TestPages;

public partial class BookmarksMap : TestPage
{
    private bool _navigationCompleted;

    public BookmarksMap()
    {
        InitializeComponent();

        var map = new Esri.ArcGISRuntime.Mapping.Map(SpatialReferences.Wgs84)
        {
            InitialViewpoint = new Viewpoint(new MapPoint(0, 0, SpatialReferences.Wgs84), 10000000),
        };
        var backgroundGrid = MainMapView.BackgroundGrid ?? throw new InvalidOperationException("The map view background grid is unavailable.");
        backgroundGrid.Color = System.Drawing.Color.FromArgb(255, 224, 234, 238);
        backgroundGrid.GridLineColor = System.Drawing.Color.FromArgb(255, 150, 170, 178);
        backgroundGrid.GridLineWidth = 1;
        backgroundGrid.GridSize = 48;
        var graphicsOverlay = new GraphicsOverlay();
        graphicsOverlay.Graphics.Add(new Graphic(new MapPoint(10, 10, SpatialReferences.Wgs84), new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 24)));
        graphicsOverlay.Graphics.Add(new Graphic(new MapPoint(30, 20, SpatialReferences.Wgs84), new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Green, 24)));
        graphicsOverlay.Graphics.Add(new Graphic(new MapPoint(-30, -20, SpatialReferences.Wgs84), new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 24)));
        MainMapView.Map = map;
        (MainMapView.GraphicsOverlays ?? throw new InvalidOperationException("The map view graphics overlay collection is unavailable.")).Add(graphicsOverlay);
        BookmarksView.GeoView = MainMapView;
        BookmarksView.BookmarksOverride =
        [
            new Bookmark("Red bookmark", new Viewpoint(10, 10, 1000000)),
            new Bookmark("Green bookmark", new Viewpoint(20, 30, 1000000)),
            new Bookmark("Blue bookmark", new Viewpoint(-20, -30, 1000000)),
        ];
        BookmarksView.BookmarkSelected += BookmarksView_BookmarkSelected;
        MainMapView.NavigationCompleted += MainMapView_NavigationCompleted;
        MainMapView.DrawStatusChanged += MainMapView_DrawStatusChanged;
    }

    private void BookmarksView_BookmarkSelected(object? sender, Bookmark bookmark)
    {
        _navigationCompleted = false;
        SelectedBookmarkText.Text = bookmark.Name;
        MapCenterText.Text = string.Empty;
    }

    private void MainMapView_NavigationCompleted(object? sender, EventArgs e)
    {
        _navigationCompleted = true;
        ReportReady();
    }

    private void MainMapView_DrawStatusChanged(object? sender, DrawStatusChangedEventArgs e)
    {
        if (e.Status == DrawStatus.Completed)
            ReportReady();
    }

    private void ReportReady()
    {
        if (!_navigationCompleted || MainMapView.DrawStatus != DrawStatus.Completed)
            return;

        if (MainMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry is MapPoint center)
        {
            var geographicCenter = (MapPoint)GeometryEngine.Project(center, SpatialReferences.Wgs84);
            MapCenterText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.0},{1:0.0}", geographicCenter.X, geographicCenter.Y);
        }
        else
        {
            MapCenterText.Text = "No viewpoint";
        }
    }
}