using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor.Hooks;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class ScaleLinePage : Component
{
    Map map = new Map(new Uri("https://www.arcgis.com/home/webmap/viewer.html?webmap=c50de463235e4161b206d000587af18b"));
    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapView>();
        var (wide, setWide) = UseState(false);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .Ref(mapViewRef),

            (ScaleLine() with
            {
                TargetWidth = wide ? 240 : 140,
            })
            .Set((ScaleLine control) => control.MapView = mapViewRef.Current)
            .Margin(20)
            .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Left)
            .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Bottom),

            GalleryControls.ControlPanel(
                ToggleSwitch(
                    wide,
                    value => setWide(value),
                    onContent: "Wide scale line",
                    offContent: "Compact scale line",
                    header: "Target width"))
        );
    }
}
