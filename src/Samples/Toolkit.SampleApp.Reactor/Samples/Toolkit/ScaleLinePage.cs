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
        var (scale, setScale) = UseState(1d);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map).Ref(mapViewRef) with
            {
                OnMapScaleChanged = (e) => setScale(e)
            },
            GalleryControls.ControlPanel(
                VStack(
                     TextBlock("Scale line bound to MapView"),
                     (ScaleLine(mapView: mapViewRef) with
                     {
                         TargetWidth = wide ? 240 : 140,
                     })
                    .Margin(20),
                      TextBlock("Scale line bound to MapView.MapScale"),
                      (ScaleLine(mapView: null) with
                      {
                          TargetWidth = wide ? 240 : 140,
                          MapScale = scale
                      })
                    .Margin(20),
                    ToggleSwitch(
                        wide,
                        value => setWide(value),
                        onContent: "Wide scale line",
                        offContent: "Compact scale line",
                        header: "Target width"))
                )
        );
    }
}
