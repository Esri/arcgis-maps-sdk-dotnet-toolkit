using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

internal static partial class Reconciler
{

    internal static MapView CreateMapView(MapViewElement element)
    {
        var mapView = new MapView { Map = element.Map }.MountGeoView(element);
        ApplySetters(element.Setters, mapView);
        return mapView;
    }

    internal static SceneView CreateSceneView(SceneViewElement element)
    {
        var sceneView = new SceneView { Scene = element.Scene }.MountGeoView(element); ;
        ApplySetters(element.Setters, sceneView);
        return sceneView;
    }

    internal static LocalSceneView CreateLocalSceneView(LocalSceneViewElement element)
    {
        var sceneView = new LocalSceneView { Scene = element.Scene }.MountGeoView(element); ;
        ApplySetters(element.Setters, sceneView);
        return sceneView;
    }

    internal static T MountGeoView<T>(this T geoview, GeoViewElement element) where T : GeoView
    {
        geoview.GraphicsOverlays = element.GraphicsOverlays;
        return geoview;
    }

    internal static void ApplySetters<T>(Action<T>[] setters, T view)
    {
        foreach (var setter in setters)
            setter(view);
    }
}
