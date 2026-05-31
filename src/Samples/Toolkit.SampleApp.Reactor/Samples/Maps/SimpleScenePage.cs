namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SimpleScenePage : Component
{
    private Scene scene = new Scene(BasemapStyle.ArcGISImageryStandard).WorldElevation();

    public override Element Render()
    {
        return SceneView(scene);
    }
}
