// /*******************************************************************************
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

#if !NETSTANDARD2_0
using System.ComponentModel;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;

#if NETFX_CORE
using Esri.ArcGISRuntime.Xamarin.Forms.Platform.UWP;
using Xamarin.Forms.Platform.UWP;
#elif __IOS__
using Esri.ArcGISRuntime.Xamarin.Forms.Platform.iOS;
using Xamarin.Forms.Platform.iOS;
#elif __ANDROID__
using Esri.ArcGISRuntime.Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android;
#endif

#if NETFX_CORE
[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.ARToolkit.Forms.ARSceneView), typeof(Esri.ArcGISRuntime.ARToolkit.Forms.Platform.UWP.ARSceneViewRenderer))]
namespace Esri.ArcGISRuntime.ARToolkit.Forms.Platform.UWP
#elif __IOS__
[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.ARToolkit.Forms.ARSceneView), typeof(Esri.ArcGISRuntime.ARToolkit.Forms.Platform.iOS.ARSceneViewRenderer))]
namespace Esri.ArcGISRuntime.ARToolkit.Forms.Platform.iOS
#elif __ANDROID__
[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.ARToolkit.Forms.ARSceneView), typeof(Esri.ArcGISRuntime.ARToolkit.Forms.Platform.Android.ARSceneViewRenderer))]
namespace Esri.ArcGISRuntime.ARToolkit.Forms.Platform.Android
#endif
{
    /// <summary>
    /// Platform Renderer for <see cref="ARSceneView"/>
    /// </summary>
    public class ARSceneViewRenderer : SceneViewRenderer
    {
        private ARToolkit.ARSceneView? ARControl => Control as ARToolkit.ARSceneView;

        private ARSceneView? ARElement => Element as ARSceneView;

#if __ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneViewRenderer"/> class;
        /// </summary>
        /// <param name="context">Application context</param>
        public ARSceneViewRenderer(global::Android.Content.Context context)
            : base(context)
        {
        }
#else

        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneViewRenderer"/> class;
        /// </summary>
        public ARSceneViewRenderer()
        {
        }
#endif

        /// <inheritdoc />
        protected override void OnElementChanged(ElementChangedEventArgs<SceneView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement != null)
            {
                if (ARControl != null) //This is null during dispose clean-up, but we already unsubscribed during dispose
                {
                    ARControl.OriginCameraChanged -= ARControl_OriginCameraChanged;
                    ARControl.PlanesDetectedChanged -= ARControl_PlanesDetectedChanged;
                }
                MessagingCenter.Unsubscribe<ARSceneView>(this, "ResetTracking");
                MessagingCenter.Unsubscribe<ARSceneView, Mapping.TransformationMatrix>(this, "SetInitialTransformation");
            }

            if (e.NewElement != null)
            {
                var elm = (ARSceneView)e.NewElement;
                if (ARControl != null)
                {
                    ARControl.TranslationFactor = elm.TranslationFactor;
                    ARControl.RenderVideoFeed = elm.RenderVideoFeed;
                    ARControl.NorthAlign = elm.NorthAlign;
                    ARControl.LocationDataSource = elm.LocationDataSource;
                    SetPlaneRendering(elm.RenderPlanes);
                    if (elm.OriginCamera != null)
                    {
                        ARControl.OriginCamera = elm.OriginCamera;
                    }
                    elm.CameraController = ARControl.CameraController; //Ensure we use the native view's camera controller
                    ARControl.OriginCameraChanged += ARControl_OriginCameraChanged;
                    ARControl.PlanesDetectedChanged += ARControl_PlanesDetectedChanged;
                }
                MessagingCenter.Subscribe<ARSceneView>(this, "ResetTracking", (s) => ARControl?.ResetTracking(), elm);
                MessagingCenter.Subscribe<ARSceneView, Mapping.TransformationMatrix>(this, "SetInitialTransformation", (s, a) => ARControl?.SetInitialTransformation(a), elm);
            }
        }

        private void ARControl_PlanesDetectedChanged(object sender, bool planesDetected)
        {
            ARElement?.RaisePlanesDetectedChanged(planesDetected);
        }

        private void ARControl_OriginCameraChanged(object sender, System.EventArgs e)
        {
            if (ARControl != null && ARElement != null)
            {
                if (!ReferenceEquals(ARElement.OriginCamera, ARControl.OriginCamera))
                    ARElement.OriginCamera = ARControl.OriginCamera;
                ARElement.RaiseOriginCameraChanged();
            }
        }

        /// <inheritdoc />
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (ARControl == null || ARElement == null)
                return;
            if (e.PropertyName == ARSceneView.TranslationFactorProperty.PropertyName)
            {
                ARControl.TranslationFactor = ARElement.TranslationFactor;
            }
            else if (e.PropertyName == ARSceneView.OriginCameraProperty.PropertyName)
            {
                if (!ReferenceEquals(ARElement.OriginCamera, ARControl.OriginCamera))
                    ARControl.OriginCamera = ARElement.OriginCamera;
            }
            else if (e.PropertyName == ARSceneView.RenderVideoFeedProperty.PropertyName)
            {
                ARControl.RenderVideoFeed = ARElement.RenderVideoFeed;
            }
            else if (e.PropertyName == ARSceneView.NorthAlignProperty.PropertyName)
            {
                ARControl.NorthAlign = ARElement.NorthAlign;
            }
            else if (e.PropertyName == ARSceneView.RenderPlanesProperty.PropertyName)
            {
                SetPlaneRendering(ARElement.RenderPlanes);
            }
            else if (e.PropertyName == ARSceneView.LocationDataSourceProperty.PropertyName)
            {
                ARControl.LocationDataSource = ARElement.LocationDataSource;
            }
        }

        private void SetPlaneRendering(bool on)
        {
            if (ARControl != null)
            {
#if __ANDROID__
                if (ARControl.ArSceneView != null)
                {
                    ARControl.ArSceneView.PlaneRenderer.Enabled = on;
                    ARControl.ArSceneView.PlaneRenderer.Visible = on;
                }
#elif __IOS__
                ARControl.RenderPlanes = on;
#elif NETFX_CORE
                //Not supported on UWP
#endif
            }
        }


        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (ARControl != null)
            {
                ARControl.OriginCameraChanged -= ARControl_OriginCameraChanged;
                ARControl.PlanesDetectedChanged -= ARControl_PlanesDetectedChanged;
                ARControl.StopTrackingAsync();
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc />
#if __ANDROID__
        protected override UI.Controls.SceneView CreateNativeElement(global::Android.Content.Context context) => new ARToolkit.ARSceneView(context);
#else
        protected override UI.Controls.SceneView CreateNativeElement() => new ARToolkit.ARSceneView();
#endif
    }
}
#endif