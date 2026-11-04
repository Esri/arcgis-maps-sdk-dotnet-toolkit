using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Microsoft.UI.Reactor.Hooks;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;
using UtilityNetworkTraceToolControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.UtilityNetworkTraceTool;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class UtilityNetworkTraceToolPage : Component
{
    private const string WebmapUrl = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";

    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var (map, setMap) = UseState<Map?>(null);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .Ref(mapViewRef),

            UtilityNetworkTraceTool()
                .Set((UtilityNetworkTraceToolControl control) => control.GeoView = mapViewRef.Current)
                .Width(360)
                .Margin(20)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Right)
                .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Top),

            (map is null
                ? GalleryControls.ControlPanel(Caption("Loading sample web map and credentials..."))
                : GalleryControls.ControlPanel(Caption("Add starting points from the map and run a named utility trace.")))
                .OnMount(async _ =>
                {
                    if (map is not null)
                    {
                        return;
                    }

                    var credential = await AccessTokenCredential.CreateAsync(
                        new Uri("https://sampleserver7.arcgisonline.com/portal/sharing/rest"),
                        "viewer01",
                        "I68VGU^nMurF");
                    AuthenticationManager.Current.AddCredential(credential);
                    setMap(new Map(new Uri(WebmapUrl)));
                })
        );
    }
}
