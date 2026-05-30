using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System.Diagnostics.Contracts;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    public static GeoViewElement GraphicsOverlays(this GeoViewElement element, GraphicsOverlayCollection collection)
    {
        return element with
        {
            GraphicsOverlays = collection
        };
    }

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