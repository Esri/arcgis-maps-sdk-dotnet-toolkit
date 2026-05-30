namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SimpleMapPage : Component
{
    public override Element Render()
    {
        var map = UseMemo(() => new Map(BasemapStyle.ArcGISStreets));
        return MapView(map);
    }
}