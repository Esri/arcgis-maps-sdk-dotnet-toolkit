using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System.Diagnostics.Contracts;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    /// <summary>
    /// Sets the GraphicsOverlays collection on the GeoView
    /// </summary>
    /// <param name="element">GeoView</param>
    /// <param name="collection">GraphicsOverlayC</param>
    /// <returns></returns>
    public static T GraphicsOverlays<T>(this T element, GraphicsOverlayCollection collection) where T : GeoViewElement
    {
        return element with
        {
            GraphicsOverlays = collection
        };
    }

    /// <summary>
    /// Replaces the basemap on the <see cref="Map"/> or <see cref="Scene"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="element"></param>
    /// <param name="basemap"></param>
    /// <returns></returns>
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

    public static T Basemap<T>(this T element, BasemapStyle basemap) where T : GeoViewElement => element.Basemap(new Basemap(basemap));

    private const string WorldElevationUri = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";
    
    public static Scene WorldElevation(this Scene scene)
    {
        var surface = scene.BaseSurface ?? new Surface();
        if (surface.ElevationSources.OfType<ArcGISTiledElevationSource>().Any(es => es.Source?.OriginalString == WorldElevationUri))
            return scene;
        surface.ElevationSources.Insert(0, new ArcGISTiledElevationSource(new Uri(WorldElevationUri)));
        scene.BaseSurface = surface;
        return scene;
    }
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
    public static MapViewElement MapView(Map? map = null, Action<GeoViewInputEventArgs>? onTapped = null) => new(map, onTapped);

    public static SceneViewElement SceneView(Scene? scene = null) => new(scene);

    public static LocalSceneViewElement LocalSceneView(Scene? scene = null) => new(scene);


    public static T Set<T>(this T element, Action<GeoView> configure) where T : GeoViewElement =>
        element with { Setters = [.. element.Setters, configure] };


}