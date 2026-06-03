using System;
using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Reactor.Hooks;
using LegendControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.Legend;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class LegendPage : Component
{
    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var (filterHidden, setFilterHidden) = UseState(true);
        var (filterScale, setFilterScale) = UseState(true);
        var (reverseOrder, setReverseOrder) = UseState(true);
        var map = UseMemo(() => new Map(new Uri("https://www.arcgis.com/home/webmap/viewer.html?webmap=df8bcc10430f48878b01c96e907a1fc3")));

        return Grid(columns: [GridSize.Px(320), GridSize.Star()], rows: [GridSize.Star()],
            (Legend() with
            {
                FilterHiddenLayers = filterHidden,
                FilterByVisibleScaleRange = filterScale,
                ReverseLayerOrder = reverseOrder,
            })
            .Set((LegendControl control) => control.GeoView = mapViewRef.Current)
            .Margin(12)
            .Grid(column: 0),

            MapView(map)
                .Ref(mapViewRef)
                .Grid(column: 1),

            GalleryControls.ControlPanel(
                VStack(8,
                    ToggleSwitch(filterHidden, value => setFilterHidden(value), onContent: "Hide hidden layers", offContent: "Show hidden layers", header: "Layer visibility"),
                    ToggleSwitch(filterScale, value => setFilterScale(value), onContent: "Filter by scale", offContent: "Ignore scale", header: "Visible scale range"),
                    ToggleSwitch(reverseOrder, value => setReverseOrder(value), onContent: "Reverse layer order", offContent: "Map layer order", header: "Layer order")))
            .Grid(column: 1)
        );
    }
}
