namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SimpleLocalScenePage : Component
{
    public override Element Render()
    {
        return LocalSceneView().Basemap(BasemapStyle.ArcGISImageryStandard);
    }
}
