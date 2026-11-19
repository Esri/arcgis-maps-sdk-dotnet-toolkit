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