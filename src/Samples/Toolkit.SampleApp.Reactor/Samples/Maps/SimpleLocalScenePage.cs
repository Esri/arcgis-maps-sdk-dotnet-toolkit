namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SimpleLocalScenePage : Component
{
    private Scene scene = new Scene(SceneViewingMode.Local, BasemapStyle.ArcGISImageryStandard).WorldElevation();

    public override Element Render()
    {
        return LocalSceneView(scene);
    }
}
