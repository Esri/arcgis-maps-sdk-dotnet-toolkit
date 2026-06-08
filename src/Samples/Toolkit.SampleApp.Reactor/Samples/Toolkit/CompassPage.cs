namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

using Microsoft.UI.Reactor.Hooks;
using CompassControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;

public sealed class CompassPage : Component
{
    private readonly Map map = new Map(BasemapStyle.ArcGISStreets);

    public override Element Render()
    {
        var (autoHide, setAutoHide) = UseState(false);
        var mapViewRef = this.UseElementRef<Esri.ArcGISRuntime.UI.Controls.GeoView>();
        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map).Ref(mapViewRef),
            Compass(geoView: mapViewRef, autoHide)
                .Margin(20)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Right)
                .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Top),
             GalleryControls.ControlPanel(
                 ToggleSwitch(autoHide, (b) => setAutoHide(b), onContent: "Hidden when north is up", offContent: "Always Visible", header: "Auto Hide Compass"))
        );
    }
}
