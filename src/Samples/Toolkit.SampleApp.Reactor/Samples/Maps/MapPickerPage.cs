namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class MapPickerPage : Component
{
    private Map[] maps = new Map[]
        {
            new Map(BasemapStyle.ArcGISImagery),
            new Map(BasemapStyle.ArcGISStreets),
            new Map(BasemapStyle.ArcGISNavigationNight)
        };

    public override Element Render()
    {
        var (selectedMap, setSelectedMap) = UseState(0);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(maps[selectedMap]),
            GalleryControls.ControlPanel(
                ComboBox(new[] { "Imagery", "Streets", "Night" },
                        selectedIndex: selectedMap,
                        onSelectedIndexChanged: (i) => { if(i >= 0) setSelectedMap(i); })
                    .Header("Select a Map")
                    .Width(200)));
    }
}
