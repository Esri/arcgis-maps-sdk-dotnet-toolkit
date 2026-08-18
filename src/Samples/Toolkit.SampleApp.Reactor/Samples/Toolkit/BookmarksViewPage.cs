using System;
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Reactor.Hooks;
using BookmarksViewControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.BookmarksView;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class BookmarksViewPage : Component
{
    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<Esri.ArcGISRuntime.UI.Controls.GeoView>();
        var (useOverride, setUseOverride) = UseState(false);
        var (selectedBookmark, setSelectedBookmark) = UseState("None");

        var map = UseMemo(() =>
        {
            var result = new Map(BasemapStyle.ArcGISImageryStandard);
            result.Bookmarks.Add(new Bookmark("Seattle", new Viewpoint(47.6062, -122.3321, 120000)));
            result.Bookmarks.Add(new Bookmark("New York", new Viewpoint(40.7128, -74.0060, 120000)));
            result.Bookmarks.Add(new Bookmark("London", new Viewpoint(51.5074, -0.1278, 120000)));
            return result;
        });

        var overrideBookmarks = UseMemo(() => new ObservableCollection<Bookmark>
        {
            new("Override One", new Viewpoint(34.0522, -118.2437, 90000)),
            new("Override Two", new Viewpoint(35.6764, 139.6500, 90000)),
            new("Override Three", new Viewpoint(-33.8688, 151.2093, 90000)),
        });

        return Grid(columns: [GridSize.Px(280), GridSize.Star()], rows: [GridSize.Star()],

            MapView(map)
                .Ref(mapViewRef)
                .Grid(column: 1),

            (VStack(12,
                (BookmarksView(mapViewRef) with
                {
                    BookmarksOverride = useOverride ? overrideBookmarks : null,
                    OnBookmarkSelected = bookmark => setSelectedBookmark(bookmark.Name),
                })
                .Width(260)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Stretch),

                Caption($"Selected: {selectedBookmark}")
                    .Margin(16, 0, 16, 0)
            )
            .Margin(12))
            .Grid(column: 0),

            GalleryControls.ControlPanel(
                ToggleSwitch(
                    useOverride,
                    value => setUseOverride(value),
                    onContent: "Using override bookmarks",
                    offContent: "Using map bookmarks",
                    header: "Bookmarks source"))
            .Grid(column: 1)
        );
    }
}
