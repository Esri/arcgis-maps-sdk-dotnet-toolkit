namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class MapPickerPage : Component
{
    public override Element Render()
    {
        var (selectedMap, setSelectedMap) = UseState(0);
        var maps = UseMemo(() => new Map[] 
        {
            new Map(BasemapStyle.ArcGISImagery),
            new Map(BasemapStyle.ArcGISStreets),
            new Map(BasemapStyle.ArcGISNavigationNight)
        });
        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(maps[selectedMap]),
            GalleryControls.ControlPanel(
                ComboBox(new[] { "Imagery", "Streets", "Night" }, selectedMap, (i) => { if(i >= 0) setSelectedMap(i); })
                    .Header("Select a Map")
                    .Width(200)));
    }
}
