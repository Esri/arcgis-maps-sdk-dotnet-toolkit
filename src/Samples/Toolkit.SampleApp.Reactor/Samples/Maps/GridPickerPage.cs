using Esri.ArcGISRuntime.UI;
using Microsoft.UI.Reactor.Hooks;
using GeoViewGrid = Esri.ArcGISRuntime.UI.Grid;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class GridPickerPage : Component
{
    private readonly (string Name, Func<GeoViewGrid?> CreateGrid)[] gridOptions =
    [
        ("None", static () => null),
        ("Latitude / Longitude", static () => new LatitudeLongitudeGrid()),
        ("MGRS", static () => new MgrsGrid()),
        ("USNG", static () => new UsngGrid()),
        ("UTM", static () => new UtmGrid()),
    ];

    private readonly Map map = new Map(BasemapStyle.ArcGISImagery);

    public override Element Render()
    {
        var (selectedGridIndex, setSelectedGridIndex) = UseState(0);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map) with
            {
                Grid = gridOptions[selectedGridIndex].CreateGrid(),
            },
            GalleryControls.ControlPanel(
                ComboBox(
                        items: gridOptions.Select(option => option.Name).ToArray(),
                        selectedIndex: selectedGridIndex,
                        onSelectedIndexChanged: index => setSelectedGridIndex(index)
                        )
                    .Header("Select a Grid")
                    .Width(220)));
    }
}
