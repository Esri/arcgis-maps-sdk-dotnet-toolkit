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

using Esri.ArcGISRuntime.Location;

namespace Esri.ArcGISRuntime.ARToolkit.Maui
{
    /// <summary>
    /// The Augmented Reality-enabled SceneView control
    /// </summary>
    public class ARSceneView : Esri.ArcGISRuntime.Maui.SceneView, IARSceneView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        public ARSceneView()
            : base()
        {
            SpaceEffect = UI.SpaceEffect.None;
            AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.None;
            OriginCamera = new Mapping.Camera(0, 0, 15E6, 0, 0, 0);
        }

        /// <summary>
        /// Identifies the <see cref="ClippingDistance"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ClippingDistanceProperty =
            BindableProperty.Create(nameof(ClippingDistance), typeof(double), typeof(ARSceneView), 0.0, BindingMode.OneWay, null);

        /// <summary>
        /// Gets or sets the clipping distance from the origin camera, beyond which data will not be displayed.
        /// Defaults to 0.0. When set to 0.0, there is no clipping distance; all data is displayed.
        /// </summary>
        /// <remarks>
        /// You can use clipping distance to limit the display of data in world-scale AR or clip data for tabletop AR.
        /// </remarks>
        public double ClippingDistance
        {
            get { return (double)GetValue(ClippingDistanceProperty); }
            set { SetValue(ClippingDistanceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TranslationFactor"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TranslationFactorProperty =
            BindableProperty.Create(nameof(TranslationFactor), typeof(double), typeof(ARSceneView), 1d, BindingMode.TwoWay, null);

        /// <summary>
        /// Gets or sets translation factor used to support a table top AR experience.
        /// </summary>
        /// <remarks>A value of 1 means if the device 1 meter in the real world, it'll move 1 m in the AR world. Set this to 1000 to make 1 m meter 1km in the AR world.</remarks>
        public double TranslationFactor
        {
            get { return (double)GetValue(TranslationFactorProperty); }
            set { SetValue(TranslationFactorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OriginCamera"/> bindable property.
        /// </summary>
        public static readonly BindableProperty OriginCameraProperty =
            BindableProperty.Create(nameof(OriginCamera), typeof(Mapping.Camera), typeof(ARSceneView), null, BindingMode.TwoWay, null);

        /// <summary>
        /// Gets or sets the viewpoint camera used to set the initial view of the sceneView instead of the device's GPS location via the location data source.
        /// </summary>
        /// <seealso cref="OriginCameraChanged"/>
        public Mapping.Camera OriginCamera
        {
            get { return (Mapping.Camera)GetValue(OriginCameraProperty); }
            set { SetValue(OriginCameraProperty, value); }
        }

        /// <summary>
        /// Raised when the <see cref="OriginCamera"/> has changed
        /// </summary>
        /// <seealso cref="OriginCamera"/>
        public event EventHandler? OriginCameraChanged;

        void IARSceneView.OnOriginCameraChanged()
        {
#if __IOS__ || __ANDROID__
            if (NativeARSceneView?.OriginCamera is Mapping.Camera newCam && !ARToolkit.ARSceneView.CameraEquals(this.OriginCamera, newCam))
            {
                OriginCamera = newCam;
            }
#endif
            
            OriginCameraChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Identifies the <see cref="RenderVideoFeed"/> bindable property.
        /// </summary>
        public static readonly BindableProperty RenderVideoFeedProperty =
            BindableProperty.Create(nameof(RenderVideoFeed), typeof(bool), typeof(ARSceneView), true, BindingMode.TwoWay, null);

        /// <summary>
        /// Gets or sets a value indicating whether the background of the <see cref="ARSceneView"/> is transparent or not. Enabling transparency allows for the
        /// camera feed to be visible underneath the <see cref="ARSceneView"/>.
        /// </summary>
        public bool RenderVideoFeed
        {
            get { return (bool)GetValue(RenderVideoFeedProperty); }
            set { SetValue(RenderVideoFeedProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="NorthAlign"/> bindable property.
        /// </summary>
        public static readonly BindableProperty NorthAlignProperty =
            BindableProperty.Create(nameof(NorthAlign), typeof(bool), typeof(ARSceneView), true, BindingMode.TwoWay, null);

        /// <summary>
        /// Gets or sets a value indicating whether the scene should attempt to use the device compass to align the scene towards north.
        /// </summary>
        /// <remarks>
        /// Note that the accuracy of the compass can heavily affect the quality of alignment.
        /// </remarks>
        public bool NorthAlign
        {
            get { return (bool)GetValue(NorthAlignProperty); }
            set { SetValue(NorthAlignProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RenderPlanes"/> bindable property.
        /// </summary>
        public static readonly BindableProperty RenderPlanesProperty =
            BindableProperty.Create(nameof(RenderPlanes), typeof(bool), typeof(ARSceneView), false, BindingMode.TwoWay, null);

        /// <summary>
        /// Gets or sets a value indicating whether to render planes that's been detected
        /// </summary>
        public bool RenderPlanes
        {
            get { return (bool)GetValue(RenderPlanesProperty); }
            set { SetValue(RenderPlanesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LocationDataSource"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LocationDataSourceProperty =
            BindableProperty.Create(nameof(LocationDataSource), typeof(LocationDataSource), typeof(ARSceneView), null, BindingMode.TwoWay, null);

        /// <summary>
        /// The data source used to get device location.
        /// Used either in conjuction with device camera tracking data or when device camera tracking is not present or not being used.
        /// </summary>
        public LocationDataSource LocationDataSource
        {
            get { return (LocationDataSource)GetValue(LocationDataSourceProperty); }
            set { SetValue(LocationDataSourceProperty, value); }
        }

        /// <summary>
        /// Starts device tracking.
        /// </summary>
        public System.Threading.Tasks.Task StartTrackingAsync(ARLocationTrackingMode locationTrackingMode = ARLocationTrackingMode.Ignore)
        {
#if !NETSTANDARD2_0
            var nativeView = NativeARSceneView;
            if (nativeView == null)
                throw new InvalidOperationException("Cannot start tracking before the view has appeared");
            return nativeView.StartTrackingAsync(locationTrackingMode);
#else
            throw new PlatformNotSupportedException();
#endif
        }

        /// <summary>
        /// Suspends device tracking.
        /// </summary>
        public System.Threading.Tasks.Task StopTrackingAsync()
        {
#if !NETSTANDARD2_0
            var nativeView = NativeARSceneView;
            if (nativeView is null)
                throw new InvalidOperationException("Cannot stop tracking before the view has a handler attached");
            return nativeView.StopTrackingAsync();
#else
            throw new PlatformNotSupportedException();
#endif
        }

        /// <summary>
        /// Resets the device tracking, using <see cref="OriginCamera"/> if it's not null or the device's GPS location via the location data source.
        /// </summary>
        public void ResetTracking()
        {
            MessagingCenter.Send(this, "ResetTracking");
        }

        /// <summary>
        /// Determines the map point for the given screen point hittesting any surface in the scene.
        /// </summary>
        /// <param name="screenPoint"> The point in screen coordinates.</param>
        /// <returns>The map point corresponding to screenPoint.</returns>
        public Geometry.MapPoint? ARScreenToLocation(Point screenPoint)
        {
#if NETSTANDARD2_0
            throw new PlatformNotSupportedException();
#else
            return NativeARSceneView?.ARScreenToLocation(ToNativePoint(screenPoint));
#endif
        }

        /// <summary>
        /// Sets the initial transformation used to offset the <see cref="OriginCamera"/>.
        /// </summary>
        /// <param name="transformationMatrix">Initial transformation matrix</param>
        /// <seealso cref="SetInitialTransformation(Point)"/>
        public void SetInitialTransformation(Mapping.TransformationMatrix transformationMatrix)
        {
#if !NETSTANDARD2_0
            if (NativeARSceneView is null)
                throw new InvalidOperationException("Cannot set initial transformation before the view has a handler attached");
#endif
            MessagingCenter.Send(this, "SetInitialTransformation", transformationMatrix);
        }

        /// <summary>
        ///  Sets the initial transformation used to offset the <see cref="OriginCamera"/>.
        ///  The initial transformation is based on an AR point determined via existing plane hit detection from <paramref name="screenLocation"/>.
        /// </summary>
        /// <param name="screenLocation">The screen point to determine the <see cref="InitialTransformation"/> from.</param>
        /// <returns>if an AR point cannot be determined, this method will return <c>false</c>.</returns>
        /// <seealso cref="SetInitialTransformation(Mapping.TransformationMatrix)"/>
        public bool SetInitialTransformation(Point screenLocation)
        {
#if NETSTANDARD2_0
            return false;
#else
            return NativeARSceneView?.SetInitialTransformation(ToNativePoint(screenLocation)) ?? false;
#endif
        }

        /// <summary>
        /// Gets the initial transformation used for a table top experience.  Defaults to the Identity Matrix.
        /// </summary>
        /// <seealso cref="SetInitialTransformation(Mapping.TransformationMatrix)"/>
        /// <seealso cref="SetInitialTransformation(Point)"/>
        public Mapping.TransformationMatrix InitialTransformation
        {
            get
            {
#if NETSTANDARD2_0
                return Mapping.TransformationMatrix.Identity;
#else
                return NativeARSceneView?.InitialTransformation ?? Mapping.TransformationMatrix.Identity;
#endif
            }
        }

        /// <summary>
        /// Raises an event indicating whether horizontal planes are currently detected or not
        /// </summary>
        public event EventHandler<bool>? PlanesDetectedChanged;

        void IARSceneView.OnPlanesDetectedChanged(bool planesDetected) => PlanesDetectedChanged?.Invoke(this, planesDetected);

#if !NETSTANDARD
        private Esri.ArcGISRuntime.ARToolkit.ARSceneView? NativeARSceneView => Handler?.PlatformView as ARToolkit.ARSceneView;
#endif

#if WINDOWS
        private Windows.Foundation.Point ToNativePoint(Point screenPoint)
        {
            return new Windows.Foundation.Point(screenPoint.X, screenPoint.Y);
        }
#elif __IOS__
        private CoreGraphics.CGPoint ToNativePoint(Point screenPoint)
        {
            return new CoreGraphics.CGPoint(screenPoint.X, screenPoint.Y);
        }
#elif __ANDROID__
        private Android.Graphics.PointF ToNativePoint(Point screenPoint)
        {
            return new Android.Graphics.PointF((float)screenPoint.X * SystemPixelToDipsFactor, (float)screenPoint.Y * SystemPixelToDipsFactor);
        }

        // Screen coordinates for native Android is in physical pixels, but XF works in DIPs so apply the factor on conversion
        private static Android.Views.IWindowManager? _windowManager;

        internal static float SystemPixelToDipsFactor
        {
            get
            {
                var displayMetrics = new Android.Util.DisplayMetrics();
                if (_windowManager == null)
                {
                    var windowService = Android.App.Application.Context?.GetSystemService(Android.Content.Context.WindowService);
                    if (windowService != null)
                    {
                        _windowManager = Android.Runtime.Extensions.JavaCast<Android.Views.IWindowManager>(windowService);
                    }
                }

                if (_windowManager == null)
                {
                    return 1f;
                }

                _windowManager.DefaultDisplay?.GetMetrics(displayMetrics);
                return displayMetrics?.Density ?? 1f;
            }
        }
#endif
    }
}
