namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class LoadingMapPage : Component
{
    public override Element Render()
    {
        var (mapUri, setMapUri) = UseState("https://www.arcgis.com/home/webmap/viewer.html?webmap=c50de463235e4161b206d000587af18b");
        var mapResource = UseResource(async ct =>
        {
            await Task.Delay(2000); // Simulate slow loading
            Map map = new Map(new Uri(mapUri));
            ct.Register(map.CancelLoad);
            await map.LoadAsync();
            return map;
        }, deps: [mapUri]); // Reload Map when mapUri changes

        return FlexColumn(
            TextBox(mapUri, (t) => setMapUri(t)).PlaceholderText("Enter Map URI").AutomationName("Map URL"),
            mapResource.Match<Element>(
                loading: () => Heading("Loading map...").Center(),
                data: (map) => MapView(map),
                error: (e) => TextBlock($"Error loading map\n{e.Message}").Foreground(new ThemeRef("SystemFillColorCriticalBrush")),
                reloading: (map) => Heading("Loading map...").Center()

            ).Flex(grow: 1, basis: 0));
    }
}