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
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if __ANDROID__
using Point = Android.Graphics.PointF;
#elif __IOS__
using Point = CoreGraphics.CGPoint;
#elif NETFX_CORE || WINUI
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
        private TransformationMatrixCameraController? _controller;  // Only null in design mode
        private bool _initialLocationSet = false; // Denotes whether we've received our initial location from the data source.
        private ARLocationTrackingMode _locationTrackingMode = ARLocationTrackingMode.Ignore; // The tracking mode controlling how the locations generated from the location data source are used during AR tracking.
        private LocationDataSource? _locationDataSource;

#if !__ANDROID__
        private bool IsDesignTime { get; } = false;
#endif

        private void InitializeCommon()
        {
            if (!IsDesignTime)
            {
                SpaceEffect = UI.SpaceEffect.None;
                AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.None;
                _initialTransformation = Mapping.TransformationMatrix.Identity;
                _controller = new TransformationMatrixCameraController() { TranslationFactor = 1 };
                _controller.OriginCameraChanged += Controller_OriginCameraChanged;
                LocationDataSource = new SystemLocationDataSource();
            }
        }

        private void Controller_OriginCameraChanged(object? sender, EventArgs e) => OriginCameraChanged?.Invoke(this, e);
        
        /// <summary>
        /// Starts device tracking.
        /// </summary>
        /// <param name="locationTrackingMode">
        /// </param>
        public async Task StartTrackingAsync(ARLocationTrackingMode locationTrackingMode = ARLocationTrackingMode.Ignore)
        {
            _locationTrackingMode = locationTrackingMode;
            if (locationTrackingMode != ARLocationTrackingMode.Ignore)
            {
                if (LocationDataSource == null)
                    throw new InvalidOperationException("Cannot use location tracking without the LocationDataSource property being initialized");
#if __ANDROID__
                if (!RequestLocationPermission())
                    return;
#endif
                await LocationDataSource.StartAsync();
            }
            CameraController = _controller;
            var currentTrackingValue = IsTracking;
            OnStartTracking();
            IsTracking = true;
            if (!currentTrackingValue)
                IsTrackingStateChanged?.Invoke(this, true);
        }

        /// <summary>
        /// Suspends device tracking.
        /// </summary>
        public async Task StopTrackingAsync()
        {
            if(LocationDataSource != null)
                await LocationDataSource.StopAsync();
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
            _initialTransformation = Mapping.TransformationMatrix.Identity;
            if(_controller != null)
                _controller.TransformationMatrix = Mapping.TransformationMatrix.Identity;
            _initialLocationSet = false;
#if __ANDROID__
            _initialHeading = null;
#endif
            OnResetTracking();
        }

        /// <summary>
        /// The data source used to get device location.
        /// Used either in conjuction with device camera tracking data or when device camera tracking is not present or not being used.
        /// </summary>
        public LocationDataSource? LocationDataSource
        {
            get => _locationDataSource;
            set
            {
                if(_locationDataSource != null)
                {
                    _locationDataSource.LocationChanged -= LocationDataSource_LocationChanged;
                }
                _locationDataSource = value;
                if (_locationDataSource != null)
                {
                    _locationDataSource.LocationChanged += LocationDataSource_LocationChanged;
                }
            }
        }

        private object locationLock = new object();
        private void LocationDataSource_LocationChanged(object? sender, Location.Location e)
        {
            if (_locationTrackingMode == ARLocationTrackingMode.Ignore)
                return;
            if (e.IsLastKnown)
                return;
            if (!IsTracking)
                return;
            lock (locationLock)
            {
                if (_locationTrackingMode == ARLocationTrackingMode.Initial && _initialLocationSet)
                    return; //Can happen when location datasource is rapidly firing
                var locationPoint = e.Position;
                if (_locationTrackingMode == ARLocationTrackingMode.Initial || !_initialLocationSet)
                {
                    double heading = OriginCamera.Heading;
                    // if location has altitude, use that else use a default value
                    var newCamera = new Mapping.Camera(locationPoint.Y, locationPoint.X, locationPoint.HasZ ? locationPoint.Z : 1, heading, 90, 0);
                    OriginCamera = newCamera;
                    _initialLocationSet = true;
                }
                else if (_locationTrackingMode == ARLocationTrackingMode.Continuous)
                {
                    var originCamera = OriginCamera;
                    // if location has altitude, use that else the previous value
                    OriginCamera = new Mapping.Camera(locationPoint.Y, locationPoint.X, locationPoint.HasZ ? locationPoint.Z : originCamera.Location.Z, originCamera.Heading, originCamera.Pitch, originCamera.Roll);
                }
            }
            if (_controller != null)
                _controller.TransformationMatrix = Mapping.TransformationMatrix.Identity;
            if (_locationTrackingMode != ARLocationTrackingMode.Continuous && LocationDataSource != null)
            {
                _ = LocationDataSource.StopAsync();
            }
        }

        /// <summary>
        /// Gets or sets translation factor used to support a table top AR experience.
        /// </summary>
        /// <remarks>A value of 1 means if the device 1 meter in the real world, it'll move 1 m in the AR world. Set this to 1000 to make 1 m meter 1km in the AR world.</remarks>
        public double TranslationFactor
        {
            get => _controller?.TranslationFactor ?? 1d;
            set
            {
                if (_controller == null) throw new InvalidOperationException();
                _controller.TranslationFactor = value;
            }
        }

        /// <summary>
        /// Gets or sets the clipping distance from the origin camera, beyond which data will not be displayed.
        /// Defaults to 0.0. When set to 0.0, there is no clipping distance; all data is displayed.
        /// </summary>
        /// <remarks>
        /// You can use clipping distance to limit the display of data in world-scale AR or clip data for tabletop AR.
        /// </remarks>
        public double ClippingDistance
        {
            get => _controller?.ClippingDistance ?? 0d;
            set
            {
                if (_controller == null) throw new InvalidOperationException();
                _controller.ClippingDistance = value;
            }
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
#if NETFX_CORE|| WINUI
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
#if NETFX_CORE|| WINUI
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
        /// Gets a value indicating whether tracking of location and angles is currently active
        /// </summary>
        /// <seealso cref="StartTrackingAsync"/>
        /// <seealso cref="StopTrackingAsync"/>
        public bool IsTracking { get; private set; }

        /// <summary>
        /// Raised if the tracking state changes
        /// </summary>
        /// <seealso cref="IsTracking"/>
        /// <seealso cref="StartTrackingAsync"/>
        /// <seealso cref="StopTrackingAsync"/>
        public event EventHandler<bool>? IsTrackingStateChanged;

        /// <summary>
        /// Gets or sets the viewpoint camera used to set the initial view of the sceneView instead of the device's GPS location via the location data source.
        /// </summary>
        /// <seealso cref="OriginCameraChanged"/>
        public Mapping.Camera OriginCamera
        {
            get => _controller?.OriginCamera ?? throw new InvalidOperationException();  // Only null in design mode
            set
            {
                if (_controller != null && value != OriginCamera && !CameraEquals(value, OriginCamera))
                {
                    _controller.OriginCamera = value;
                } 
            }
        }

        public static bool CameraEquals(Mapping.Camera camera1, Mapping.Camera camera2)
        {
            if (camera1 is null && camera2 is null) return true;
            if (camera1 is null && camera2 != null || camera1 != null && camera2 is null) return false;
            if (ReferenceEquals(camera1, camera2)) return true;
            return camera1!.Location.X == camera2!.Location.X &&
                camera1.Location.Y == camera2.Location.Y &&
                camera1.Location.Z == camera2.Location.Z &&
                Math.Abs(camera1.Heading - camera2.Heading) < 0.01 &&
                Math.Abs(camera1.Pitch - camera2.Pitch) < 0.01 &&
                Math.Abs(camera1.Roll - camera2.Roll) < 0.01;
        }

        /// <summary>
        /// Raised when the <see cref="OriginCamera"/> has changed
        /// </summary>
        /// <seealso cref="OriginCamera"/>
        public event EventHandler? OriginCameraChanged;

        private Mapping.TransformationMatrix? _initialTransformation; // Only null in design mode

        /// <summary>
        /// Gets the initial transformation used for a table top experience.  Defaults to the Identity Matrix.
        /// </summary>
        /// <seealso cref="SetInitialTransformation(Mapping.TransformationMatrix)"/>
        /// <seealso cref="SetInitialTransformation(Point)"/>
        public Mapping.TransformationMatrix InitialTransformation { get => _initialTransformation ?? throw new InvalidOperationException(); }

        /// <summary>
        /// Determines the map point for the given screen point hittesting any surface in the scene.
        /// </summary>
        /// <param name="screenPoint"> The point in screen coordinates.</param>
        /// <returns>The map point corresponding to screenPoint.</returns>
        public Geometry.MapPoint? ARScreenToLocation(Point screenPoint)
        {
            var matrix = HitTest(screenPoint);
            if (matrix == null)
            {
                return null;
            }
            var translatedMatrix = Mapping.TransformationMatrix.Create(
                matrix.QuaternionX, matrix.QuaternionY, matrix.QuaternionZ, matrix.QuaternionW,
                matrix.TranslationX * TranslationFactor, matrix.TranslationY * TranslationFactor, matrix.TranslationZ * TranslationFactor);
            return new Mapping.Camera(OriginCamera.Transformation + translatedMatrix).Location;
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

            _initialTransformation = transformationMatrix;
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

            _initialTransformation = Mapping.TransformationMatrix.Identity - origin;
            return true;
        }

        /// <summary>
        /// Raises an event indicating whether horizontal planes are currently detected or not
        /// </summary>
        public event EventHandler<bool>? PlanesDetectedChanged;

        internal void RaisePlanesDetectedChanged(bool planesDetected)
        {
            PlanesDetectedChanged?.Invoke(this, planesDetected);
        }
    }

#endif
}
