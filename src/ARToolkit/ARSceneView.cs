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
using System;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if __ANDROID__
using Point = Android.Graphics.PointF;
#elif __IOS__
using Point = CoreGraphics.CGPoint;
#elif NETFX_CORE
using Point = Windows.Foundation.Point;
#endif
#endif

namespace Esri.ArcGISRuntime.ARToolkit
{
#if !NETSTANDARD2_0
    /// <summary>
    /// The Augmented Reality-enabled SceneView control
    /// </summary>
    public partial class ARSceneView : SceneView
    {
        private TransformationMatrixCameraController _controller;

#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        public ARSceneView() : base()
        {
            InitializeCommon();
        }

        private bool IsDesignTime { get; } = false;
#endif

        private void InitializeCommon()
        {
            if (!IsDesignTime)
                {
                    SpaceEffect = UI.SpaceEffect.None;
                    AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.None;
                    _controller = new TransformationMatrixCameraController() { TranslationFactor = 1 }; ;
            }
            
                    Initialize();
                }

        /// <summary>
        /// Starts device tracking.
        /// </summary>
        public Task StartTrackingAsync()
        {
            CameraController = _controller;
            var currentTrackingValue = IsTracking;
            OnStartTracking();
            IsTracking = true;
            if(!currentTrackingValue)
                IsTrackingStateChanged?.Invoke(this, true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Suspends device tracking.
        /// </summary>
        public void StopTracking()
        {
            OnStopTracking();
            if (IsTracking)
            {
                IsTracking = false;
                IsTrackingStateChanged?.Invoke(this, false);
            }
        }

        /// <summary>
        /// Resets the device tracking, using <see cref="OriginCamera"/> if it's not null or the device's GPS location via the location data source.
        /// </summary>
        public void ResetTracking()
        {
            _ = StartTrackingAsync();
        }

        /// <summary>
        /// Gets or sets translation factor used to support a table top AR experience.
        /// </summary>
        /// <remarks>A value of 1 means if the device 1 meter in the real world, it'll move 1 m in the AR world. Set this to 1000 to make 1 m meter 1km in the AR world.</remarks>
        public double TranslationFactor
        {
            get => _controller.TranslationFactor;
            set => _controller.TranslationFactor = value;
        }

        private bool _renderVideoFeed = true;

        /// <summary>
        /// Gets or sets a value indicating whether the background of the <see cref="ARSceneView"/> is transparent or not. Enabling transparency allows for the
        /// camera feed to be visible underneath the <see cref="ARSceneView"/>.
        /// </summary>
        public bool RenderVideoFeed
        {
            get => _renderVideoFeed;
            set
            {
                _renderVideoFeed = value;
                if (_renderVideoFeed)
                {
                    SpaceEffect = UI.SpaceEffect.None;
                    AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.None;
#if NETFX_CORE
                    if (_cameraView != null)
                    {
                        _cameraView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                    StartCapturing();
#endif
                }
                else
                {
                    SpaceEffect = UI.SpaceEffect.Stars;
                    AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.HorizonOnly;
#if NETFX_CORE
                    if(_cameraView != null)
                    {
                        _cameraView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }
                    StopCapturing();
#endif
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether tracking is currently active
        /// </summary>
        /// <seealso cref="StartTrackingAsync"/>
        /// <seealso cref="StopTracking"/>
        public bool IsTracking { get; private set; }

        /// <summary>
        /// Raised if the tracking state changes
        /// </summary>
        /// <seealso cref="IsTracking"/>
        /// <seealso cref="StartTrackingAsync"/>
        /// <seealso cref="StopTracking"/>
        public event EventHandler<bool> IsTrackingStateChanged;

        /// <summary>
        /// Gets or sets the viewpoint camera used to set the initial view of the sceneView instead of the device's GPS location via the location data source.
        /// </summary>
        /// <seealso cref="OriginCameraChanged"/>
        public Mapping.Camera OriginCamera
        {
            get => _controller.OriginCamera;
            set
            {
                _controller.OriginCamera = value;
                if (IsTracking)
                {
                    ResetTracking();
                }

                OriginCameraChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raised when the <see cref="OriginCamera"/> has changed
        /// </summary>
        /// <seealso cref="OriginCamera"/>
        public event EventHandler OriginCameraChanged;

        /// <summary>
        /// Gets the initial transformation used for a table top experience.  Defaults to the Identity Matrix.
        /// </summary>
        /// <seealso cref="SetInitialTransformation(Mapping.TransformationMatrix)"/>
        /// <seealso cref="SetInitialTransformation(Point)"/>
        public Mapping.TransformationMatrix InitialTransformation { get; private set; } = Mapping.TransformationMatrix.Identity;

        /// <summary>
        /// Determines the map point for the given screen point hittesting any surface in the scene.
        /// </summary>
        /// <param name="screenPoint"> The point in screen coordinates.</param>
        /// <returns>The map point corresponding to screenPoint.</returns>
        public Geometry.MapPoint ARScreenToLocation(Point screenPoint)
        {
            var matrix = HitTest(screenPoint);
            if (matrix == null)
            {
                return null;
            }

            return new Mapping.Camera(Camera.Transformation + matrix).Location;
        }

        /// <summary>
        /// Sets the initial transformation used to offset the <see cref="OriginCamera"/>.
        /// </summary>
        /// <param name="transformationMatrix">Initial transformation matrix</param>
        /// <seealso cref="SetInitialTransformation(Point)"/>
        public void SetInitialTransformation(Mapping.TransformationMatrix transformationMatrix)
        {
            if (transformationMatrix == null)
            {
                throw new ArgumentNullException(nameof(transformationMatrix));
            }

            InitialTransformation = transformationMatrix;
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
            var origin = HitTest(screenLocation);
            if (origin == null)
            {
                return false;
            }

            InitialTransformation = Mapping.TransformationMatrix.Identity - origin;
            return true;
        }

        public static DeviceSupport SupportLevel
        {
            get
            {
#if __ANDROID__
                var availability = Google.AR.Core.ArCoreApk.Instance.CheckAvailability(global::Android.App.Application.Context);
                if (availability == Google.AR.Core.ArCoreApk.Availability.SupportedApkTooOld ||
                    availability == Google.AR.Core.ArCoreApk.Availability.SupportedInstalled ||
                    availability == Google.AR.Core.ArCoreApk.Availability.SupportedNotInstalled)
                {
                    return DeviceSupport.SixDegreesOfFreedom;
                }
                if (CompassOrientationHelper.IsSupported(global::Android.App.Application.Context))
                    return DeviceSupport.ThreeDegreesOfFreedom;
#elif __IOS__
                if (ARKit.ARConfiguration.IsSupported)
                    return DeviceSupport.SixDegreesOfFreedom;
                else
                    return DeviceSupport.ThreeDegreesOfFreedom;
#elif NETFX_CORE
                if (Windows.Devices.Sensors.OrientationSensor.GetDefault() != null)
                    return DeviceSupport.ThreeDegreesOfFreedom;
#endif
                return DeviceSupport.NotSupported;
            }
        }
    }

#endif
    public enum DeviceSupport
    {
        NotSupported,
        ThreeDegreesOfFreedom,
        SixDegreesOfFreedom
    }
}
