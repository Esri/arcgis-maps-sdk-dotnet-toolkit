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

        private Mapping.Camera _start;

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
                         if (_start == null)
                        {
                            _start = new Esri.ArcGISRuntime.Mapping.Camera(c.Location, c.Heading, 90, 0);
                            _controller.OriginCamera = _start;
                        }

                        var q = pov.WorldOrientation;
                        var t = pov.Transform;
                        _controller.TransformationMatrix = new TransformationMatrix(q.X, q.Y, q.Z, q.W, t.Row3.X, t.Row3.Y, t.Row3.Z);
                    }
                }
            }

            if (IsManualRendering)
            {
                RenderFrame();
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