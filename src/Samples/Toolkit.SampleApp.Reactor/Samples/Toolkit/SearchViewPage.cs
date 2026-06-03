using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Reactor.Hooks;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;
using SearchViewControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.SearchView;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class SearchViewPage : Component
{
    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var (showResultList, setShowResultList) = UseState(true);
        var (showRepeatSearch, setShowRepeatSearch) = UseState(true);
        var map = UseMemo(() => new Map(BasemapStyle.ArcGISImagery));

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .Ref(mapViewRef),

            (SearchView() with
            {
                EnableResultListView = showResultList,
                EnableRepeatSearchHereButton = showRepeatSearch,
            })
            .Set((SearchViewControl control) => control.GeoView = mapViewRef.Current)
            .Width(320)
            .Margin(20)
            .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Right)
            .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Top),

            GalleryControls.ControlPanel(
                VStack(8,
                    ToggleSwitch(showResultList, value => setShowResultList(value), onContent: "Show result list", offContent: "Hide result list", header: "Result list"),
                    ToggleSwitch(showRepeatSearch, value => setShowRepeatSearch(value), onContent: "Show repeat search", offContent: "Hide repeat search", header: "Repeat search button")))
        );
    }
}
