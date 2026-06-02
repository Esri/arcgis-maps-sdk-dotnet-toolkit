using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    /// <summary>
    /// Sets the graphics overlays displayed by a geoview element.
    /// </summary>
    /// <typeparam name="T">The type of geoview element.</typeparam>
    /// <param name="element">The geoview element to configure.</param>
    /// <param name="collection">The graphics overlay collection to display.</param>
    /// <returns>The configured geoview element.</returns>
    public static T GraphicsOverlays<T>(this T element, GraphicsOverlayCollection collection) where T : GeoViewElement
    {
        return element with
        {
            GraphicsOverlays = collection
        };
    }

    /// <summary>
    /// Replaces the basemap on the underlying <see cref="Map"/> or <see cref="Scene"/>.
    /// </summary>
    /// <typeparam name="T">The type of geoview element.</typeparam>
    /// <param name="element">The geoview element to configure.</param>
    /// <param name="basemap">The basemap to apply.</param>
    /// <returns>The configured geoview element.</returns>
    public static T Basemap<T>(this T element, Basemap basemap) where T : GeoViewElement =>
        element.Set((gv) =>
        {
            if (gv is MapView mv)
            {
                if (mv.Map is not null)
                    mv.Map.Basemap = basemap;
                else
                    mv.Map = new Map(basemap);
            }
            else if (gv is SceneView sv)
            {
                if (sv.Scene is not null)
                    sv.Scene.Basemap = basemap;
                else
                    sv.Scene = new Scene(SceneViewingMode.Global, basemap);
            }
            else if (gv is LocalSceneView lsv)
            {
                if (lsv.Scene is not null)
                    lsv.Scene.Basemap = basemap;
                else
                    lsv.Scene = new Scene(SceneViewingMode.Local, basemap);
            }
        });

    /// <summary>
    /// Replaces the basemap on the underlying <see cref="Map"/> or <see cref="Scene"/> using a <see cref="BasemapStyle"/>.
    /// </summary>
    /// <typeparam name="T">The type of geoview element.</typeparam>
    /// <param name="element">The geoview element to configure.</param>
    /// <param name="basemap">The basemap style to apply.</param>
    /// <returns>The configured geoview element.</returns>
    public static T Basemap<T>(this T element, BasemapStyle basemap) where T : GeoViewElement => element.Basemap(new Basemap(basemap));

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
    /// Configures the location display for a map view element.
    /// </summary>
    /// <param name="element">The map view element to configure.</param>
    /// <param name="enabled"><see langword="true"/> to enable the location display; otherwise, <see langword="false"/>.</param>
    /// <param name="autoPanMode">The auto-pan mode to apply to the location display.</param>
    /// <returns>The configured map view element.</returns>
    public static MapViewElement LocationDisplay(this MapViewElement element, bool enabled, LocationDisplayAutoPanMode autoPanMode = LocationDisplayAutoPanMode.Off)
    {
        return element with
        {
            LocationDisplay = new LocationDisplayElement()
            {
                IsEnabled = enabled,
                AutoPanMode = autoPanMode
            }
        };
    }

    /// <summary>
    /// Creates a declarative <see cref="MapViewElement"/>.
    /// </summary>
    /// <param name="map">The map displayed by the view.</param>
    /// <param name="onTapped">The action invoked when the underlying <see cref="MapView"/> is tapped.</param>
    /// <returns>A new <see cref="MapViewElement"/> instance.</returns>
    public static MapViewElement MapView(Map? map = null, Action<GeoViewInputEventArgs>? onTapped = null) => new(map, onTapped);

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


    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="GeoView"/>.
    /// </summary>
    /// <typeparam name="T">The type of geoview element.</typeparam>
    /// <param name="element">The geoview element to configure.</param>
    /// <param name="configure">The action that configures the mounted geoview.</param>
    /// <returns>The configured geoview element.</returns>
    public static T Set<T>(this T element, Action<GeoView> configure) where T : GeoViewElement =>
        element with { Setters = [.. element.Setters, configure] };


}