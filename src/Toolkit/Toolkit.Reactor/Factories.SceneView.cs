using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    private const string WorldElevationUri = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

    /// <summary>
    /// Adds the ArcGIS Online world elevation source to a scene's base surface when it is not already present.
    /// </summary>
    /// <param name="scene">The scene to configure.</param>
    /// <returns>The configured scene.</returns>
    public static Scene WorldElevation(this Scene scene)
    {
        var surface = scene.BaseSurface ?? new Surface();
        if (surface.ElevationSources.OfType<ArcGISTiledElevationSource>().Any(es => es.Source?.OriginalString == WorldElevationUri))
            return scene;
        surface.ElevationSources.Insert(0, new ArcGISTiledElevationSource(new Uri(WorldElevationUri)));
        scene.BaseSurface = surface;
        return scene;
    }

    /// <summary>
    /// Creates a declarative <see cref="SceneViewElement"/>.
    /// </summary>
    /// <param name="scene">The scene displayed by the view.</param>
    /// <returns>A new <see cref="SceneViewElement"/> instance.</returns>
    public static SceneViewElement SceneView(Scene? scene = null) => new(scene);

    /// <summary>
    /// Creates a declarative <see cref="LocalSceneViewElement"/>.
    /// </summary>
    /// <param name="scene">The local scene displayed by the view.</param>
    /// <returns>A new <see cref="LocalSceneViewElement"/> instance.</returns>
    public static LocalSceneViewElement LocalSceneView(Scene? scene = null) => new(scene);
}