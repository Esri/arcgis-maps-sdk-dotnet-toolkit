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
        public ARSceneView()
        {
            SpaceEffect = UI.SpaceEffect.None;

            // IsManualRendering = true;
            AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.None;
            Initialize();
        }
#endif

        public void StartTracking()
        {
            if (IsTracking)
            {
                return;
            }
            CameraController = _controller;
            OnStartTracking();
            IsTracking = true;
        }

        public void StopTracking()
        {
            IsTracking = false;
            OnStopTracking();
            CameraController = new GlobeCameraController();
        }

        public double TranslationFactor
        {
            get => _controller.TranslationFactor;
            set => _controller.TranslationFactor = value;
        }

        public bool IsTracking { get; private set; }

        private Mapping.Camera _originCamera;

        public Mapping.Camera OriginCamera
        {
            get => _controller.OriginCamera;
            set
            {
                _controller.OriginCamera = value;
                //ResetTracking();
                OriginCameraChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OriginCameraChanged;

        public Mapping.TransformationMatrix InitialTransformation { get; private set; } = IdentityMatrix;

        public Geometry.MapPoint ARScreenToLocation(Point screenPoint)
        {
            var matrix = HitTest(screenPoint);
            if (matrix == null)
            {
                return null;
            }

            return new Mapping.Camera(Camera.Transformation + matrix).Location;
        }

        public void ResetTracking()
        {
            var vc = Camera;
            if (vc != null)
            {
                _controller.OriginCamera = vc;
            }
            StopTracking();
            StartTracking();
        }

        public void SetInitialTransformation(Mapping.TransformationMatrix transformationMatrix)
        {
            if (transformationMatrix == null)
                throw new ArgumentNullException(nameof(transformationMatrix));
            InitialTransformation = transformationMatrix;
        }

        public bool SetInitialTransformation(Point screenLocation)
        {
            var origin = HitTest(screenLocation);
            if (origin == null)
            {
                return false;
            }

            InitialTransformation = IdentityMatrix - origin;
            return true;
        }

        private static readonly Mapping.TransformationMatrix IdentityMatrix = new Mapping.TransformationMatrix(0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0);
    }
}
#endif