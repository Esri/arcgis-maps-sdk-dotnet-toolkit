namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SimpleMapPage : Component
{
    private Map map = new Map(BasemapStyle.ArcGISStreets);

    public override Element Render()
    {
        return MapView(map);
    }
}