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

#if __IOS__
using System;
using System.Linq;
using ARKit;
using CoreMedia;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.ARToolkit
{
    public partial class ARSceneView : SceneView
    {
        private bool _created;
        private ARSCNView _arview;
        private ArSessionDel _delegate;
        private UIView _sceneviewSurface;

        private void Initialize()
        {
            _delegate = new ArSessionDel(this);
            IsManualRendering = true;

            // Each session has to be configured.
            //  We will use ARWorldTrackingConfiguration to have full access to device orientation,
            // rear camera, device position and to detect real-world flat surfaces:
            _arConfiguration = new ARWorldTrackingConfiguration
            {
                PlaneDetection = ARPlaneDetection.Horizontal,
                WorldAlignment = ARWorldAlignment.Gravity,
                LightEstimationEnabled = false
            };

            _arview = new ARSCNView() { TranslatesAutoresizingMaskIntoConstraints = false };
            _delegate.FrameUpdated += FrameUpdated;
            _delegate.CameraTrackingStateChanged += CameraTrackingStateChanged;
        }

        /// <summary>
        /// Gets a reference to the ARKit SceneView child control
        /// </summary>
        public ARSCNView ARSCNView
        {
            get => _arview;
        }

        /// <summary>
        /// Gets a value indicating whether ARKit should be used for tracking the device movements
        /// </summary>
        public bool UseARKit { get; private set; }

        /// <inheritdoc />
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            if (!_created)
            {
                _sceneviewSurface = Subviews[0];
                _sceneviewSurface.BackgroundColor = UIColor.Clear;

                _created = true;
                InsertSubview(_arview, 0);
                _arview.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
                _arview.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
                _arview.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
                _arview.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            }

            if (_isStarted)
            {
                OnStartTracking();
            }
        }

        private bool _tracking;

        private void CameraTrackingStateChanged(object sender, ARCamera camera)
        {
            var state = string.Empty;
            var reason = string.Empty;

            switch (camera.TrackingState)
            {
                case ARTrackingState.NotAvailable:
                    _tracking = false;
                    state = "Tracking Not Available";
                    break;
                case ARTrackingState.Normal:
                    _tracking = true;
                    state = "Tracking Normal";
                    break;
                case ARTrackingState.Limited:
                    state = "Tracking Limited";
                    _tracking = true;
                    switch (camera.TrackingStateReason)
                    {
                        case ARTrackingStateReason.ExcessiveMotion:
                            reason = "because of excessive motion";
                            break;
                        case ARTrackingStateReason.Initializing:
                            reason = "because tracking is initializing";
                            break;
                        case ARTrackingStateReason.InsufficientFeatures:
                            reason = "because of insufficient features in the environment";
                            break;
                        case ARTrackingStateReason.None:
                            reason = "because of an unknown reason";
                            break;
                    }

                    break;
            }

            // TODO: Do something with this
            System.Diagnostics.Debug.WriteLine($"CameraTrackingStateChanged: {camera.TrackingState} : {reason}");
        }

        private void FrameUpdated(object sender, ARFrame frame)
        {
            if (_tracking)
            {
                var pov = _arview.PointOfView;
                if (pov != null)
                {
                    var c = Camera;
                    if (c != null)
                    {
                        var q = pov.WorldOrientation;
                        var t = pov.Transform;
                        _controller.TransformationMatrix = InitialTransformation + TransformationMatrix.Create(q.X, q.Y, q.Z, q.W, t.Row3.X, t.Row3.Y, t.Row3.Z);
                        var intrinsics = frame.Camera.Intrinsics;
                        var imageResolution = frame.Camera.ImageResolution;
                        SetFieldOfView(intrinsics.R0C0, intrinsics.R1C1, intrinsics.R0C2, intrinsics.R1C2, (float)imageResolution.Width, (float)imageResolution.Height, GetDeviceOrientation());
                    }
                }
            }

            if (IsManualRendering)
            {
                RenderFrame();
            }
        }

        private DeviceOrientation GetDeviceOrientation()
        {
            switch (UIDevice.CurrentDevice.Orientation)
            {
                case UIDeviceOrientation.Portrait: return DeviceOrientation.Portrait;
                case UIDeviceOrientation.LandscapeLeft: return DeviceOrientation.LandscapeLeft;
                case UIDeviceOrientation.LandscapeRight: return DeviceOrientation.LandscapeRight;
                case UIDeviceOrientation.PortraitUpsideDown: return DeviceOrientation.ReversePortrait;
                default: return DeviceOrientation.Portrait;
            }
        }

        private bool _isStarted;

        private void OnStartTracking()
        {
            _isStarted = true;
            if (UseARKit)
            {
                // Once we have our configuration we need to run session with it.
                // ResetTracking will just reset tracking by session to start it again from scratch:
                _arview.Session.Delegate = _delegate;
                _arview.Session.Run(ARConfiguration, ARSessionRunOptions.ResetTracking);
            }
        }

        private void OnStopTracking()
        {
            if (UseARKit)
            {
                _arview.Session.Pause();
                _arview.Session.Delegate = null;
            }
        }

        private void OnResetTracking()
        {
            if (UseARKit)
            {
                _arview.Session.Run(ARConfiguration, ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors);
            }
        }

        private ARConfiguration _arConfiguration;

        /// <summary>
        /// Gets or sets the world tracking information used by ARKit.
        /// </summary>
        public ARConfiguration ARConfiguration
        {
            get => _arConfiguration;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value != _arConfiguration)
                {
                    _arConfiguration = value;
                    if (IsTracking && UseARKit)
                    {
                        _arview.Session.Run(ARConfiguration, ARSessionRunOptions.ResetTracking);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the opacity the <see cref="Scene" /> is rendered with. 
        /// This is useful for making the scene slightly see-through while placing it.
        /// </summary>
        public nfloat SceneAlpha
        {
            get => _sceneviewSurface.Alpha;
            set => _sceneviewSurface.Alpha = value;
        }

        private bool _northAlign = true;

        /// <summary>
        /// Gets or sets a value indicating whether the scene should attempt to the device compass to align the scene towards north
        /// </summary>
        /// <remarks>
        /// Note that the accuracy of the compass can heavily affect the quality of alignment.
        /// The property updates the WorldAlignment property on the ARWorldTrackingConfiguration to either GravityAndHeading or Gravity.
        /// </remarks>.
        public bool NorthAlign
        {
            get { return _northAlign; }
            set
            {
                if (_northAlign != value)
                {
                    _northAlign = value;
                    if (ARConfiguration is ARWorldTrackingConfiguration w)
                    {
                        w.WorldAlignment = value ? ARWorldAlignment.GravityAndHeading : ARWorldAlignment.Gravity;
                    }
                }
            }
        }

        private TransformationMatrix HitTest(CoreGraphics.CGPoint screenPoint, ARHitTestResultType type = ARHitTestResultType.EstimatedHorizontalPlane)
        {
            var hit = _arview.HitTest(screenPoint, type);
            // Get the worldTransform from the first result; if there's no worldTransform, return null.
            var t = hit?.FirstOrDefault()?.WorldTransform;
            if (t != null)
            {
                return TransformationMatrix.Create(0, 0, 0, 1, t.Value.Column3.X, t.Value.Column3.Y, t.Value.Column3.Z);
            }

            return null;
        }

        private class ArSessionDel : ARSessionDelegate
        {
            private readonly ARSceneView _view;

            public ArSessionDel(ARSceneView view)
            {
                _view = view;
            }

            public override void CameraDidChangeTrackingState(ARSession session, ARCamera camera) => CameraTrackingStateChanged?.Invoke(session, camera);

            public override void DidUpdateFrame(ARSession session, ARFrame frame)
            {   
                FrameUpdated?.Invoke(this, frame);
                frame.Dispose(); // Must dispose frame after this. See https://xamarin.github.io/bugzilla-archives/60/60393/bug.html
            }

            public override void DidAddAnchors(ARSession session, ARAnchor[] anchors)
            {
            }

            public override void DidFail(ARSession session, NSError error)
            {
            }

            public override void DidRemoveAnchors(ARSession session, ARAnchor[] anchors)
            {
            }

            public override void DidUpdateAnchors(ARSession session, ARAnchor[] anchors)
            {
            }

            public override void DidOutputAudioSampleBuffer(ARSession session, CMSampleBuffer audioSampleBuffer)
            {
            }

            public override void WasInterrupted(ARSession session)
            {
            }

            public override void InterruptionEnded(ARSession session)
            {
            }

            public override bool ShouldAttemptRelocalization(ARSession session)
            {
                return false;
            }

            public event EventHandler<ARFrame> FrameUpdated;

            public event EventHandler<ARCamera> CameraTrackingStateChanged;
        }
    }
}
#endif