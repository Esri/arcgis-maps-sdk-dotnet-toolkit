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
        private ARSCNView _arview;
        private ArSessionDel _delegate;

        private void Initialize()
        {
            _delegate = new ArSessionDel(this);
            BackgroundColor = UIColor.Clear;
            IsManualRendering = true;

            // _arview = new ARSCNView();
            _delegate.FrameUpdated += FrameUpdated;
            _delegate.CameraTrackingStateChanged += CameraTrackingStateChanged;
        }

        public ARSCNView ARSCNView
        {
            get => _arview;
            set
            {
                if (_arview != value)
                {
                    // if (_arview?.Session != null)
                    //     _arview.Session.Delegate = null;
                    _arview = value;

                    // if (_arview?.Session != null)
                    //     _arview.Session.Delegate = _delegate;
                }
            }
        }

        /// <inheritdoc />
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            // _arview = new ARSCNView();
            // InsertSubview(_arview, 0);
            // AddSubview(_arview);
            _arview.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _arview.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
            _arview.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            _arview.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            if (_isStarted)
            {
                StartTracking();
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
                         if (_controller.OriginCamera == null)
                        {
                            _controller.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(c.Location, c.Heading, 90, 0);
                            OriginCameraChanged?.Invoke(this, EventArgs.Empty);
                        }

                        var q = pov.WorldOrientation;
                        var t = pov.Transform;
                        _controller.TransformationMatrix = InitialTransformation + new TransformationMatrix(q.X, q.Y, q.Z, q.W, t.Row3.X, t.Row3.Y, t.Row3.Z);
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

        private ARSession _arsession;
        private bool _isStarted;

        private void OnStartTracking()
        {
            _isStarted = true;
            if (_arview == null)
            {
                return;
            }

            // Each session has to be configured.
            //  We will use ARWorldTrackingConfiguration to have full access to device orientation,
            // rear camera, device position and to detect real-world flat surfaces:
            var configuration = new ARWorldTrackingConfiguration
            {
                PlaneDetection = ARPlaneDetection.Horizontal,
                LightEstimationEnabled = false
            };

            // Once we have our configuration we need to run session with it.
            // ResetTracking will just reset tracking by session to start it again from scratch:
            _arview.Session.Delegate = _delegate;
            _arview.Session.Run(configuration, ARSessionRunOptions.ResetTracking);
        }

        private void OnStopTracking()
        {
            _arview.Session.Pause();
            _arview.Session.Delegate = null;
        }

        private TransformationMatrix HitTest(CoreGraphics.CGPoint screenPoint, ARHitTestResultType type = ARHitTestResultType.EstimatedHorizontalPlane)
        {
            var hit = _arview.Session.CurrentFrame.HitTest(screenPoint, type);
            if (hit.Length > 0)
            {
                var l = hit[0].LocalTransform;
                var t = hit[0].WorldTransform;
                var a = hit[0].Anchor;
                var d = hit[0].Distance;
                var rtype = hit[0].Type;
                var qw = Math.Sqrt(1 + t.Column0.X + t.Column1.Y + t.Column2.Z) / 2.0;
                var qx = (t.Column2.Y - t.Column1.Z) / (qw * 4);
                var qy = (t.Column0.Z - t.Column2.X) / (qw * 4);
                var qz = (t.Column1.X - t.Column0.Y) / (qw * 4);
                return new TransformationMatrix(qx, qy, qz, qw, t.M14, -t.M24, t.M34);
            }
            return null;
        }

        private class ARViewDelegate : ARSCNViewDelegate
        {
        }

        private class ARSessionObserver : IARSessionObserver
        {
            public IntPtr Handle => throw new NotImplementedException();

            public void Dispose()
            {
                throw new NotImplementedException();
            }
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

        /// <summary>
        /// Gets a value indicating whether <c>ARKit</c> is supported on this device.
        /// </summary>
        public bool IsSupported => ARWorldTrackingConfiguration.IsSupported;
    }
}
#endif