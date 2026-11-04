using Esri.ArcGISRuntime.Geometry;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class BasemapPickerPage : Component
{
    public override Element Render()
    {
        var (selectedBasemap, setSelectedBasemap) = UseState(BasemapStyle.ArcGISStreets);
        var map = UseMemo(() => new Map(SpatialReferences.WebMercator) { Basemap = new Basemap(selectedBasemap) });

        var basemaps = Enum.GetValues<BasemapStyle>().ToList();

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map).Basemap(selectedBasemap),
            GalleryControls.ControlPanel(
                ComboBox(
                        items: basemaps.Select(b=>b.ToString()).ToArray(),
                        selectedIndex: basemaps.IndexOf(selectedBasemap),
                        onSelectedIndexChanged: (i) => setSelectedBasemap(basemaps[i]))
                    .Header("Select a Basemap")
                    .Width(200)));
    }
}
