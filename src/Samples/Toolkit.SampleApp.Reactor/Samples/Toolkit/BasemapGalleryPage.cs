using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Reactor.Hooks;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class BasemapGalleryPage : Component
{
    private readonly Map map = new(BasemapStyle.ArcGISStreets);

    public override Element Render()
    {
        var (gridStyle, setGridStyle) = UseState(false);
        var (basemap, setBasemap) = UseState(default(BasemapGalleryItem));
        return Grid(columns: [GridSize.Px(300), GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .Grid(column: 1),
            BasemapGallery(map) with
            {
                SelectedBasemap = basemap,
                 GalleryViewStyle = gridStyle ? BasemapGalleryViewStyle.Grid : BasemapGalleryViewStyle.List,
                 OnBasemapSelected = (b) => 
                 {
                     setBasemap(b);
                 }
            },
             GalleryControls.ControlPanel(
                 ToggleSwitch(gridStyle, (b) => setGridStyle(b), onContent: "Grid of basemaps", offContent: "List of basemaps", header: "View Style"))
                .Grid(column: 1),

             Title("Selected: " + basemap?.Name).Grid(column: 1)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Center)
                .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Bottom)
                .Margin(0,0,0,40)
        );
    }
}
