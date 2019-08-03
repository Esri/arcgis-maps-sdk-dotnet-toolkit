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

namespace Esri.ArcGISRuntime.ARToolkit
{
    /// <summary>
    /// The Augmented Reality-enabled SceneView control
    /// </summary>
    public partial class ARSceneView : SceneView
    {
        private TransformationMatrixCameraController _controller = new TransformationMatrixCameraController() { TranslationFactor = 100 };

#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        public ARSceneView()
        {
            SpaceEffect = UI.SpaceEffect.None;

            // IsManualRendering = true;
            AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.None;
            Initialize();
        }
#endif

        /// <summary>
        /// Starts device tracking.
        /// </summary>
        public void StartTracking()
        {
            if (IsTracking)
            {
                return;
            }

            CameraController = _controller;
            OnStartTracking();
            IsTracking = true;
            _ = _locationDataSource?.StartAsync();
        }

        /// <summary>
        /// Suspends device tracking.
        /// </summary>
        public void StopTracking()
        {
            IsTracking = false;
            _ = _locationDataSource?.StopAsync();
            OnStopTracking();
            CameraController = new GlobeCameraController();
        }

        /// <summary>
        /// Resets the device tracking, using <see cref="OriginCamera"/> if it's not null or the device's GPS location via the location data source.
        /// </summary>
        public void ResetTracking()
        {
            var vc = Camera;
            if (vc != null)
            {
                _controller.OriginCamera = vc;
            }

            _initialLocation = null;
            StopTracking();
            StartTracking();
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
                    _cameraView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    StartCapturing();
#endif
                }
                else
                {
                    SpaceEffect = UI.SpaceEffect.Stars;
                    AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.HorizonOnly;
#if NETFX_CORE
                    _cameraView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    StopCapturing();
#endif
                }
            }
        }

        private Location.LocationDataSource _locationDataSource;

        /// <summary>
        /// Gets or sets the data source used to get device location.
        /// </summary>
        public Location.LocationDataSource LocationDataSource
        {
            get => _locationDataSource;
            set
            {
                if (_locationDataSource != value)
                {
                    if (_locationDataSource != null)
                    {
                        _locationDataSource.LocationChanged -= LocationDataSource_LocationChanged;
                    }

                    _locationDataSource = value;
                    if (_locationDataSource != null)
                    {
                        // TODO: Should be a weak listener
                        _locationDataSource.LocationChanged += LocationDataSource_LocationChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Initial location from location data source.
        /// </summary>
        private MapPoint _initialLocation;

        private void LocationDataSource_LocationChanged(object sender, Location.Location e)
        {
            var locationPoint = e.Position;
            if (locationPoint != null)
            {
                if (_initialLocation == null)
                {
                    _initialLocation = locationPoint;

                    // Create a new camera based on our location and set it on the cameraController.
                    OriginCamera = new Mapping.Camera(locationPoint, heading: 0.0, pitch: 0.0, roll: 0.0);
                }
                else
                {
                    var camera = Camera.MoveTo(locationPoint);
                    SetViewpointCamera(camera);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether tracking is currently active
        /// </summary>
        /// <seealso cref="StartTracking"/>
        /// <seealso cref="StopTracking"/>
        public bool IsTracking { get; private set; }

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
    }
}
#endif