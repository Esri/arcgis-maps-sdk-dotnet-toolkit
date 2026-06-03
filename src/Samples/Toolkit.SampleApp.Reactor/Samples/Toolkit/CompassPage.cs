namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

using Microsoft.UI.Reactor.Hooks;
using CompassControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;

public sealed class CompassPage : Component
{
    public override Element Render()
    {
        var (autoHide, setAutoHide) = UseState(false);
        var map = UseMemo(() => new Map(BasemapStyle.ArcGISStreets));
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var mapview = MapView(map).Ref(mapViewRef);
        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            mapview,
            Compass(geoView: null, autoHide)
                .Set((CompassControl control) => control.GeoView = mapViewRef.Current)
                .Margin(20)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Right)
                .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Top),
             GalleryControls.ControlPanel(
                 ToggleSwitch(autoHide, (b) => setAutoHide(b), onContent: "Hidden when north is up", offContent: "Always Visible", header: "Auto Hide Compass"))
        );
    }
}
