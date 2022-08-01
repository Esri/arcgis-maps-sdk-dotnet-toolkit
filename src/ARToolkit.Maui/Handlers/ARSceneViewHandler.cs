using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Maui.Handlers;
using Microsoft.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.ARToolkit.Maui.Handlers
{
    public class ARSceneViewHandler //: Esri.ArcGISRuntime.Maui.Handlers.SceneViewHandler
 #if WINDOWS || __IOS__ || __ANDROID__
         : ViewHandler<IARSceneView, Esri.ArcGISRuntime.ARToolkit.ARSceneView>
 #else
         : ViewHandler<IARSceneView, object>
 #endif
    {
        public static PropertyMapper<IARSceneView, ARSceneViewHandler> ARSceneViewMapper = new PropertyMapper<IARSceneView, ARSceneViewHandler>(ArcGISRuntime.Maui.Handlers.SceneViewHandler.ViewMapper)
        {
            [nameof(IARSceneView.ClippingDistance)] = MapClippingDistance,
            [nameof(IARSceneView.LocationDataSource)] = MapLocationDataSource,
            [nameof(IARSceneView.NorthAlign)] = MapNorthAlign,
            [nameof(IARSceneView.OriginCamera)] = MapOriginCamera,
            [nameof(IARSceneView.RenderPlanes)] = MapRenderPlanes,
            [nameof(IARSceneView.RenderVideoFeed)] = MapRenderVideoFeed,
            [nameof(IARSceneView.TranslationFactor)] = MapTranslationFactor,
        };

        public ARSceneViewHandler() : this(ARSceneViewMapper)
        {
        }

        public ARSceneViewHandler(PropertyMapper mapper) : base(mapper ?? ARSceneViewMapper)
        {
        }

#if !NETSTANDARD
        protected override void ConnectHandler(ARToolkit.ARSceneView platformView)
        {
            base.ConnectHandler(platformView);
            platformView.OriginCameraChanged += OriginCameraChanged;
            platformView.PlanesDetectedChanged += PlatformView_PlanesDetectedChanged;
        }

        protected override void DisconnectHandler(ARToolkit.ARSceneView platformView)
        {
            platformView.OriginCameraChanged -= OriginCameraChanged;
            platformView.PlanesDetectedChanged -= PlatformView_PlanesDetectedChanged;
            base.DisconnectHandler(platformView);
        }

        private void PlatformView_PlanesDetectedChanged(object? sender, bool planesDetected) => VirtualView.OnPlanesDetectedChanged(planesDetected);

        private void OriginCameraChanged(object? sender, EventArgs e) => VirtualView.OnOriginCameraChanged();
        
#endif

        public static void MapClippingDistance(ARSceneViewHandler handler, IARSceneView mapView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.ClippingDistance = mapView.ClippingDistance;
#endif
        }

        public static void MapLocationDataSource(ARSceneViewHandler handler, IARSceneView mapView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.LocationDataSource = mapView.LocationDataSource;
#endif
        }

        public static void MapNorthAlign(ARSceneViewHandler handler, IARSceneView mapView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.NorthAlign = mapView.NorthAlign;
#endif
        }

        public static void MapOriginCamera(ARSceneViewHandler handler, IARSceneView mapView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.OriginCamera = mapView.OriginCamera;
#endif
        }

        public static void MapRenderPlanes(ARSceneViewHandler handler, IARSceneView mapView)
        {
#if __ANDROID__
            if (handler.PlatformView.ArSceneView != null)
            {
                handler.PlatformView.ArSceneView.PlaneRenderer.Enabled = mapView.RenderPlanes;
                handler.PlatformView.ArSceneView.PlaneRenderer.Visible = mapView.RenderPlanes;
            }
#elif __IOS__
            handler.PlatformView.RenderPlanes = mapView.RenderPlanes;
#endif
        }

        public static void MapRenderVideoFeed(ARSceneViewHandler handler, IARSceneView mapView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.RenderVideoFeed = mapView.RenderVideoFeed;
#endif
        }

        public static void MapTranslationFactor(ARSceneViewHandler handler, IARSceneView mapView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.TranslationFactor = mapView.TranslationFactor;
#endif
        }

#if !NETSTANDARD
        /// <inheritdoc/>
        protected override Esri.ArcGISRuntime.ARToolkit.ARSceneView CreatePlatformView()
        {
#if __ANDROID__
            return new Esri.ArcGISRuntime.ARToolkit.ARSceneView(Context);
#else
            return new Esri.ArcGISRuntime.ARToolkit.ARSceneView();
#endif
        }
#else
        /// <inheritdoc/>
        protected override object CreatePlatformView() => throw new System.NotImplementedException();
#endif
    }
}