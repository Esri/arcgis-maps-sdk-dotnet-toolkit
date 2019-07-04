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
        private ARToolkit.ARSceneView ARControl => Control as ARToolkit.ARSceneView;

        private ARSceneView ARElement => Element as ARSceneView;

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
                MessagingCenter.Unsubscribe<ARSceneView>(this, "StartTracking");
                MessagingCenter.Unsubscribe<ARSceneView>(this, "StopTracking");
                MessagingCenter.Unsubscribe<ARSceneView>(this, "ResetTracking");
                MessagingCenter.Unsubscribe<ARSceneView, Mapping.TransformationMatrix>(this, "SetInitialTransformation");
            }

            if (e.NewElement != null)
            {
                var elm = (ARSceneView)e.NewElement;
                ARControl.TranslationFactor = elm.TranslationFactor;
                if (elm.OriginCamera != null)
                {
                    ARControl.OriginCamera = elm.OriginCamera;
                }

                MessagingCenter.Subscribe<ARSceneView>(this, "StartTracking", (s) => ARControl.StartTracking(), elm);
                MessagingCenter.Subscribe<ARSceneView>(this, "StopTracking", (s) => ARControl.StopTracking(), elm);
                MessagingCenter.Subscribe<ARSceneView>(this, "ResetTracking", (s) => ARControl.ResetTracking(), elm);
                MessagingCenter.Subscribe<ARSceneView, Mapping.TransformationMatrix>(this, "SetInitialTransformation", (s, a) => ARControl.SetInitialTransformation(a), elm);
            }
        }

        /// <inheritdoc />
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == ARSceneView.TranslationFactorProperty.PropertyName)
            {
                ARControl.TranslationFactor = ARElement.TranslationFactor;
            }
            else if (e.PropertyName == ARSceneView.OriginCameraProperty.PropertyName)
            {
                ARControl.OriginCamera = ARElement.OriginCamera;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            ARControl.StopTracking();
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