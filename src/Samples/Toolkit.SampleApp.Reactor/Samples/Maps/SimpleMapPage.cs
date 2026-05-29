using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Esri.ArcGISRuntime.Toolkit.Reactor.Factories;
using static Microsoft.UI.Reactor.Factories;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SimpleMapPage : Component
{
    public override Element Render()
    {
        var (map, setMap) = UseState(new Map(BasemapStyle.ArcGISImageryStandard));
        return MapView(map);
    }
}
