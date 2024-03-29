﻿// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using Microsoft.Maui.Handlers;

namespace Esri.ArcGISRuntime.ARToolkit.Maui.Handlers
{
    public class ARSceneViewHandler
 #if WINDOWS || __IOS__ || __ANDROID__
         : ViewHandler<IARSceneView, Esri.ArcGISRuntime.ARToolkit.ARSceneView>
 #else
         : ViewHandler<IARSceneView, object>
 #endif
    {
        /// <summary>
        /// Property mapper for the <see cref="ARSceneView"/> control.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneViewHandler"/> class.
        /// </summary>
        public ARSceneViewHandler() : this(ARSceneViewMapper)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneViewHandler"/> class.
        /// </summary>
        /// <param name="mapper">The property view mapper.</param>
        public ARSceneViewHandler(PropertyMapper mapper) : base(mapper ?? ARSceneViewMapper)
        {
        }

#if !NETSTANDARD
        /// <inheritdoc />
        protected override void ConnectHandler(ARToolkit.ARSceneView platformView)
        {
            base.ConnectHandler(platformView);
            platformView.OriginCameraChanged += OriginCameraChanged;
            platformView.PlanesDetectedChanged += PlatformView_PlanesDetectedChanged;
        }

        /// <inheritdoc />
        protected override void DisconnectHandler(ARToolkit.ARSceneView platformView)
        {
            platformView.OriginCameraChanged -= OriginCameraChanged;
            platformView.PlanesDetectedChanged -= PlatformView_PlanesDetectedChanged;
            base.DisconnectHandler(platformView);
        }

        private void PlatformView_PlanesDetectedChanged(object? sender, bool planesDetected) => VirtualView.OnPlanesDetectedChanged(planesDetected);

        private void OriginCameraChanged(object? sender, EventArgs e) => VirtualView.OnOriginCameraChanged();

#endif

        /// <summary>
        /// Maps the <see cref="ARSceneView.ClippingDistance"/> property to the native ARSceneView control.
        /// </summary>
        /// <param name="handler">View handler</param>
        /// <param name="arSceneView">ARSceneView instance</param>
        public static void MapClippingDistance(ARSceneViewHandler handler, IARSceneView arSceneView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.ClippingDistance = arSceneView.ClippingDistance;
#endif
        }

        /// <summary>
        /// Maps the <see cref="ARSceneView.LocationDataSource"/> property to the native ARSceneView control.
        /// </summary>
        /// <param name="handler">View handler</param>
        /// <param name="arSceneView">ARSceneView instance</param>
        public static void MapLocationDataSource(ARSceneViewHandler handler, IARSceneView arSceneView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.LocationDataSource = arSceneView.LocationDataSource;
#endif
        }

        /// <summary>
        /// Maps the <see cref="ARSceneView.NorthAlign"/> property to the native ARSceneView control.
        /// </summary>
        /// <param name="handler">View handler</param>
        /// <param name="arSceneView">ARSceneView instance</param>
        public static void MapNorthAlign(ARSceneViewHandler handler, IARSceneView arSceneView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.NorthAlign = arSceneView.NorthAlign;
#endif
        }

        /// <summary>
        /// Maps the <see cref="ARSceneView.OriginCamera"/> property to the native ARSceneView control.
        /// </summary>
        /// <param name="handler">View handler</param>
        /// <param name="arSceneView">ARSceneView instance</param>
        public static void MapOriginCamera(ARSceneViewHandler handler, IARSceneView arSceneView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.OriginCamera = arSceneView.OriginCamera;
#endif
        }

        /// <summary>
        /// Maps the <see cref="ARSceneView.RenderPlanes"/> property to the native ARSceneView control.
        /// </summary>
        /// <param name="handler">View handler</param>
        /// <param name="arSceneView">ARSceneView instance</param>
        public static void MapRenderPlanes(ARSceneViewHandler handler, IARSceneView arSceneView)
        {
#if __ANDROID__
            if (handler.PlatformView.ArSceneView != null)
            {
                handler.PlatformView.ArSceneView.PlaneRenderer.Enabled = arSceneView.RenderPlanes;
                handler.PlatformView.ArSceneView.PlaneRenderer.Visible = arSceneView.RenderPlanes;
            }
#elif __IOS__
            handler.PlatformView.RenderPlanes = arSceneView.RenderPlanes;
#endif
        }

        /// <summary>
        /// Maps the <see cref="ARSceneView.RenderVideoFeed"/> property to the native ARSceneView control.
        /// </summary>
        /// <param name="handler">View handler</param>
        /// <param name="arSceneView">ARSceneView instance</param>
        public static void MapRenderVideoFeed(ARSceneViewHandler handler, IARSceneView arSceneView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.RenderVideoFeed = arSceneView.RenderVideoFeed;
#endif
        }

        /// <summary>
        /// Maps the <see cref="ARSceneView.TranslationFactor"/> property to the native ARSceneView control.
        /// </summary>
        /// <param name="handler">View handler</param>
        /// <param name="arSceneView">ARSceneView instance</param>
        public static void MapTranslationFactor(ARSceneViewHandler handler, IARSceneView arSceneView)
        {
#if !NETSTANDARD
            if (handler.PlatformView != null)
                handler.PlatformView.TranslationFactor = arSceneView.TranslationFactor;
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
        protected override object CreatePlatformView() => throw new System.PlatformNotSupportedException();
#endif
    }
}