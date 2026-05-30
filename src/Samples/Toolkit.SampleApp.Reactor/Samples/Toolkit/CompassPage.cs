namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class CompassPage : Component
{
    public override Element Render()
    {
        var (autoHide, setAutoHide) = UseState(false);
        var map = UseMemo(() => new Map(BasemapStyle.ArcGISStreets));
        var mapview = MapView(map);
        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            mapview,
            Compass(geoView: mapview, autoHide)
                .Margin(20)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Right)
                .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Top),
             GalleryControls.ControlPanel(
                 ToggleSwitch(autoHide, (b) => setAutoHide(b), onContent: "Hidden when north is up", offContent: "Always Visible", header: "Auto Hide Compass"))
        );
    }
}
