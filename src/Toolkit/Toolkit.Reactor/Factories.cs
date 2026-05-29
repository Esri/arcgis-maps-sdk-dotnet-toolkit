using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static class Factories
{
    public static void Register(Reconciler reconciler)
    {
        Action<GeoViewElement, GeoViewElement, GeoView> geoViewUpdate = static (oldElement, newElement, geoView) =>
        {
            if (oldElement.GraphicsOverlays != newElement.GraphicsOverlays)
                geoView.GraphicsOverlays = newElement.GraphicsOverlays;
        };
        ReactorApp.RegisterControlAssembly(new Esri_ArcGISRuntime_WinUI_XamlTypeInfo.XamlMetaDataProvider());
        reconciler.RegisterType<MapViewElement, MapView>(
            mount: (_, element, _) => CreateMapView(element),
            update: (r, oldElement, newElement, mapView, a) =>
            {
                if (oldElement.map != newElement.map)
                    mapView.Map = newElement.map;
                geoViewUpdate(oldElement, newElement, mapView);
                ApplySetters(newElement.Setters, mapView);
                return null;
            },
            unmount: static (r, mapView) => mapView.Map = null
            );
        reconciler.RegisterType<SceneViewElement, SceneView>(
            mount: (_, element, _) => CreateSceneView(element),
            update: (_, oldElement, newElement, sceneView, _) =>
            {
                if (oldElement.scene != newElement.scene)
                    sceneView.Scene = newElement.scene;
                geoViewUpdate(oldElement, newElement, sceneView);

                ApplySetters(newElement.Setters, sceneView);
                return null;
            },
            unmount: static (r, sceneView) => sceneView.Scene = null
            );
        reconciler.RegisterType<LocalSceneViewElement, LocalSceneView>(
            mount: (_, element, _) => CreateLocalSceneView(element),
            update: (_, oldElement, newElement, sceneView, _) =>
            {
                if (oldElement.scene != newElement.scene)
                    sceneView.Scene = newElement.scene;
                geoViewUpdate(oldElement, newElement, sceneView);

                ApplySetters(newElement.Setters, sceneView);
                return null;
            },
            unmount: static (r, sceneView) => sceneView.Scene = null
            );
    }

    public record GeoViewElement() : Element
    {
        internal Action<GeoView>[] Setters { get; init; } = [];
        public GraphicsOverlayCollection? GraphicsOverlays { get; init; }
    }
    public record MapViewElement(Map? map) : GeoViewElement()
    {
        public Map? Map { get; init; }
    }
    public record SceneViewElement(Scene? scene) : GeoViewElement() { }
    public record LocalSceneViewElement(Scene? scene) : GeoViewElement() { }

    public static MapViewElement MapView(Map? map = null) => new(map);
    public static SceneViewElement SceneView(Scene? scene = null) => new(scene);
    public static LocalSceneViewElement LocalSceneView(Scene? scene = null) => new(scene);

    public static GeoViewElement GraphicsOverlays(this GeoViewElement el, GraphicsOverlayCollection collection)
    {
        return el with
        {
            GraphicsOverlays = collection
        };
    }


    public static T Set<T>(this T element, Action<GeoView> configure) where T : GeoViewElement =>
        element with { Setters = [.. element.Setters, configure] };

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
                    sv.Scene = new Scene(basemap);
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

    private static MapView CreateMapView(MapViewElement element)
    {
        var mapView = new MapView { Map = element.map };
        ApplySetters(element.Setters, geoView: mapView);
        return mapView;
    }

    private static SceneView CreateSceneView(SceneViewElement element)
    {
        var sceneView = new SceneView { Scene = element.scene };
        ApplySetters(element.Setters, geoView: sceneView);
        return sceneView;
    }

    private static LocalSceneView CreateLocalSceneView(LocalSceneViewElement element)
    {
        var sceneView = new LocalSceneView { Scene = element.scene };
        ApplySetters(element.Setters, geoView: sceneView);
        return sceneView;
    }

    private static void ApplySetters(Action<GeoView>[] setters, GeoView geoView)
    {
        foreach (var setter in setters)
            setter(geoView);
    }
}