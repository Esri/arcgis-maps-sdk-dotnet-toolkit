using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static class Interop
{
    private static void UnmountGeoview(GeoView geoView)
    {
        geoView.GraphicsOverlays = null;
    }

    private static void UpdateGeoview(GeoViewElement oldElement, GeoViewElement newElement, GeoView geoView)
    {
        if (oldElement.GraphicsOverlays != newElement.GraphicsOverlays)
            geoView.GraphicsOverlays = newElement.GraphicsOverlays;
    }

    public static void Register(Microsoft.UI.Reactor.Core.Reconciler reconciler)
    {
        ReactorApp.RegisterControlAssembly(new Esri_ArcGISRuntime_WinUI_XamlTypeInfo.XamlMetaDataProvider());
        ReactorApp.RegisterControlAssembly(new Esri_ArcGISRuntime_Toolkit_WinUI_XamlTypeInfo.XamlMetaDataProvider());

        reconciler.RegisterType<MapViewElement, MapView>(
            mount: static (_, element, _) => Reconciler.CreateMapView(element),
            update: static (r, oldElement, newElement, mapView, a) =>
            {
                if (oldElement.Map != newElement.Map)
                    mapView.Map = newElement.Map;
                if(oldElement.LocationDisplay.IsEnabled != newElement.LocationDisplay.IsEnabled)
                    mapView.LocationDisplay.IsEnabled = newElement.LocationDisplay.IsEnabled;
                if(oldElement.LocationDisplay.AutoPanMode != newElement.LocationDisplay.AutoPanMode)
                    mapView.LocationDisplay.AutoPanMode = newElement.LocationDisplay.AutoPanMode;
                UpdateGeoview(oldElement, newElement, mapView);
                Reconciler.ApplySetters(newElement.Setters, mapView);
                return null;
            },
            unmount: static (r, mapView) => {
                mapView.Map = null;
                UnmountGeoview(mapView);
            }
            );
        reconciler.RegisterType<SceneViewElement, SceneView>(
            mount: static (_, element, _) => Reconciler.CreateSceneView(element),
            update: static (_, oldElement, newElement, sceneView, _) =>
            {
                if (oldElement.Scene != newElement.Scene)
                    sceneView.Scene = newElement.Scene;
                UpdateGeoview(oldElement, newElement, sceneView);

                Reconciler.ApplySetters(newElement.Setters, sceneView);
                return null;
            },
            unmount: static (r, sceneView) => {
                sceneView.Scene = null;
                UnmountGeoview(sceneView);
            }
            );
        reconciler.RegisterType<LocalSceneViewElement, LocalSceneView>(
            mount: static (_, element, _) => Reconciler.CreateLocalSceneView(element),
            update: static (_, oldElement, newElement, sceneView, _) =>
            {
                if (oldElement.Scene != newElement.Scene)
                    sceneView.Scene = newElement.Scene;
                UpdateGeoview(oldElement, newElement, sceneView);

                Reconciler.ApplySetters(newElement.Setters, sceneView);
                return null;
            },
            unmount: static (r, sceneView) => {
                sceneView.Scene = null;
                UnmountGeoview(sceneView);
            }
            );

        // Toolkit
        reconciler.RegisterType<BasemapGalleryElement, BasemapGallery>(
         mount: static (_, element, _) => Reconciler.CreateBasemapGallery(element),
         update: static (_, oldElement, newElement, visual, _) =>
         {
             BasemapGalleryElement.Update(oldElement, newElement, visual);
             Reconciler.ApplySetters(newElement.Setters, visual);
             return null;
         },
         unmount: static (r, visual) => BasemapGalleryElement.Unmount(visual)
         );

        reconciler.RegisterType<CompassElement, Compass>(
           mount: static (_, element, _) => Reconciler.CreateCompass(element),
           update: static (_, oldElement, newElement, visual, _) =>
           {
               CompassElement.Update(oldElement, newElement, visual);
               Reconciler.ApplySetters(newElement.Setters, visual);
               return null;
           },
           unmount: static (r, visual) => CompassElement.Unmount(visual)
           );
    }
}