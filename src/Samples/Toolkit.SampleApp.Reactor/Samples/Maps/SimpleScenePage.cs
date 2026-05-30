namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SimpleScenePage : Component
{
    public override Element Render()
    {
        var scene = UseMemo(() => new Scene(BasemapStyle.ArcGISImageryStandard));
        return SceneView(scene);
    }
}
