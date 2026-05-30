namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class BasemapGalleryPage : Component
{
    public override Element Render()
    {
        var (gridStyle, setGridStyle) = UseState(false);
        var map = UseMemo(() => new Map(BasemapStyle.ArcGISStreets));
        return Grid(columns: [GridSize.Px(300), GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .Grid(column: 1),
            BasemapGallery(map)
                .ViewStyle(gridStyle ? BasemapGalleryViewStyle.Grid : BasemapGalleryViewStyle.List),
             GalleryControls.ControlPanel(
                 ToggleSwitch(gridStyle, (b) => setGridStyle(b), onContent: "Grid of basemaps", offContent: "List of basemaps", header: "View Style"))
                .Grid(column: 1)
        );
    }
}
