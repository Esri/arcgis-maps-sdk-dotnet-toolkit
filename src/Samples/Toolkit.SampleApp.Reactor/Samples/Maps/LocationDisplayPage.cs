using Esri.ArcGISRuntime.UI;
using Windows.UI.ApplicationSettings;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class LocationDisplayPage : Component
{
    private Map map = new Map(BasemapStyle.ArcGISNavigation);

    public override Element Render()
    {
        var (enableLocation, setEnableLocation) = UseState(false);
        var (autopanmode, setAutopanmode) = UseState(LocationDisplayAutoPanMode.Off);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
         MapView(map: map)
            .LocationDisplay(enableLocation, autopanmode),
         GalleryControls.ControlPanel(
             VStack(
                ToggleSwitch(enableLocation, (e) => setEnableLocation(e), header: "Enable location"),
                Caption("Auto Pan Mode:"),
                ComboBox(new [] { "Off", "Recenter", "Navigation", "Compass" }, (int)autopanmode, (i) => setAutopanmode((LocationDisplayAutoPanMode)i))
             )));
    }
}