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
using System.Collections.Generic;
using System.Linq;
using ARKit;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using SceneKit;
using UIKit;

namespace Esri.ArcGISRuntime.ARToolkit
{
    public partial class ARSceneView : SceneView
    {
        private bool _created;
        private ARDelegate _delegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        public ARSceneView() : base()
        {
            InitializeCommon();
       
            _delegate = new ARDelegate(this);

            // Each session has to be configured.
            //  We will use ARWorldTrackingConfiguration to have full access to device orientation,
            // rear camera, device position and to detect real-world flat surfaces:
            _arConfiguration = new ARWorldTrackingConfiguration
            {
                PlaneDetection = ARPlaneDetection.Horizontal,
                WorldAlignment = ARWorldAlignment.GravityAndHeading,
                LightEstimationEnabled = false
            };

            if (DeviceSupportsARKit)
            {
                ARSCNView = new ARSCNView2(_delegate) { TranslatesAutoresizingMaskIntoConstraints = false };
            }

            IsUsingARKit = DeviceSupportsARKit;
            // Tell the SceneView we will be calling `RenderFrame()` manually if we're using ARKit.
            IsManualRendering = IsUsingARKit;
        }

        /// <summary>
        /// Gets a reference to the ARKit SceneView child control
        /// </summary>
        public ARSCNView? ARSCNView { get; private set; }

        /// <summary>
        /// Gets a value indicating whether ARKit should be used for tracking the device movements
        /// </summary>
        public bool IsUsingARKit { get; private set; }

        /// <inheritdoc />
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            if (!_created)
            {
                var sceneviewSurface = Subviews[0];
                sceneviewSurface.BackgroundColor = UIColor.Clear;

                _created = true;
                if (ARSCNView != null)
                {
                    InsertSubview(ARSCNView, 0);
                    ARSCNView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
                    ARSCNView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
                    ARSCNView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
                    ARSCNView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
                }
            }

            if (_isStarted)
            {
                OnStartTracking();
            }
        }

        private bool _tracking;

        private void OnCameraDidChangeTrackingState(ARSession session, ARCamera camera)
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
            ARSCNViewCameraDidChangeTrackingState?.Invoke(this, new ARSCNViewCameraTrackingStateEventArgs(session, camera));
        }

        private void OnWillRenderScene(ISCNSceneRenderer renderer, SCNScene scene, double timeInSeconds)
        {
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                if (_tracking && ARSCNView != null)
                {
                    var pov = ARSCNView.PointOfView;
                    if (pov != null)
                    {
                        var c = Camera;
                        if (c != null)
                        {
                            var q = pov.WorldOrientation;
                            var t = pov.Transform;
                            if (_controller != null)
                                _controller.TransformationMatrix = InitialTransformation + TransformationMatrix.Create(q.X, q.Y, q.Z, q.W, t.Row3.X, t.Row3.Y, t.Row3.Z);
                            using (var frame = ARSCNView.Session.CurrentFrame)
                            {
                                var camera = frame?.Camera;
                                if (camera != null)
                                {
                                    var intrinsics = camera.Intrinsics;
                                    var imageResolution = camera.ImageResolution;
                                    SetFieldOfView(intrinsics.M11, intrinsics.M22, intrinsics.M13, intrinsics.M23, (float)imageResolution.Width, (float)imageResolution.Height, GetDeviceOrientation());
                                }
                            }
                        }
                    }
                }

                if (IsManualRendering)
                {
                    RenderFrame();
                }
                ARSCNViewWillRenderScene?.Invoke(this, new ARSCNViewRenderSceneEventArgs(renderer, scene, timeInSeconds));
            });
            
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
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                _isStarted = true;
                if (IsUsingARKit)
                {
                    // Once we have our configuration we need to run session with it.
                    // ResetTracking will just reset tracking by session to start it again from scratch:
                    ARSCNView?.Session.Run(ARConfiguration, ARSessionRunOptions.ResetTracking);
                }
            });
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            OnStopTracking();
            base.Dispose(disposing);
        }

        private void OnStopTracking()
        {
            if (IsUsingARKit && ARSCNView != null)
            {
                _delegate.OnStop();
                ARSCNView.Session.Pause();
            }
        }

        private void OnResetTracking()
        {
            if (IsUsingARKit)
            {
                ARSCNView?.Session.Run(ARConfiguration, ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors);
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
                    if (IsTracking && IsUsingARKit)
                    {
                        ARSCNView?.Session.Run(ARConfiguration, ARSessionRunOptions.ResetTracking);
                    }
                }
            }
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

        private TransformationMatrix? HitTest(CoreGraphics.CGPoint screenPoint, ARHitTestResultType type = ARHitTestResultType.EstimatedHorizontalPlane)
        {
            var hit = ARSCNView?.HitTest(screenPoint, type);
            // Get the worldTransform from the first result; if there's no worldTransform, return null.
            var t = hit?.FirstOrDefault()?.WorldTransform;
            if (t != null)
            {
                return TransformationMatrix.Create(0, 0, 0, 1, t.Value.Column3.X, t.Value.Column3.Y, t.Value.Column3.Z);
            }

            return null;
        }

        private static bool DeviceSupportsARKit => ARConfiguration.IsSupported;

        private bool _renderPlanes;

        /// <summary>
        /// Gets or sets a value indicating whether to render the detected horizontal planes using a default simple rendering
        /// </summary>
        /// <remarks>
        /// You can also render your own planes using the <see cref="ARSCNViewDelegate"/> delegate.
        /// </remarks>
        public bool RenderPlanes
        {
            get => _renderPlanes;
            set
            {
                if (_renderPlanes != value)
                {
                    _renderPlanes = value;
                    if(ARSCNView?.Delegate is ARDelegate ard)
                    {
                        ard.TogglePlanes(_renderPlanes);
                    }
                }
            }
        }

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.WillUpdateNode(ISCNSceneRenderer, SCNNode, ARAnchor)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler<ARSCNViewNodeEventArgs>? ARSCNViewWillUpdateNode;

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.DidAddNode(ISCNSceneRenderer, SCNNode, ARAnchor)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler<ARSCNViewNodeEventArgs>? ARSCNViewDidAddNode;

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.DidUpdateNode(ISCNSceneRenderer, SCNNode, ARAnchor)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler<ARSCNViewNodeEventArgs>? ARSCNViewDidUpdateNode;

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.DidRemoveNode(ISCNSceneRenderer, SCNNode, ARAnchor)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler<ARSCNViewNodeEventArgs>? ARSCNViewDidRemoveNode;

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.CameraDidChangeTrackingState(ARSession, ARCamera)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler<ARSCNViewCameraTrackingStateEventArgs>? ARSCNViewCameraDidChangeTrackingState;

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.WillRenderScene(ISCNSceneRenderer, SCNScene, double)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler<ARSCNViewRenderSceneEventArgs>? ARSCNViewWillRenderScene;

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.DidRenderScene(ISCNSceneRenderer, SCNScene, double)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler<ARSCNViewRenderSceneEventArgs>? ARSCNViewDidRenderScene;

        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.WasInterrupted(ARSession)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler? ARSCNViewWasInterrupted;
        
        /// <summary>
        /// Triggered when the <see cref="ARSCNViewDelegate.InterruptionEnded(ARSession)"/> delegate method gets invoked.
        /// </summary>
        public event EventHandler? ARSCNViewInterruptionEnded;

        /// <summary>
        /// Custom version of ARSCNView that prevents setting the delegate that we rely on (use the provided events instead)
        /// </summary>
        private class ARSCNView2 : ARSCNView
        {
            internal ARSCNView2(IARSCNViewDelegate del)
            {
                base.Delegate = del;
            }

            public override IARSCNViewDelegate? Delegate { get => base.Delegate; set => throw new NotSupportedException(); }
        }

        private class ARDelegate : ARSCNViewDelegate
        {
            private ARSceneView _sceneView;
            private Dictionary<NSUuid, Plane> _planes = new Dictionary<NSUuid, Plane>();

            public ARDelegate(ARSceneView sceneView)
            {
                _sceneView = sceneView;
            }

            public override void WillRenderScene(ISCNSceneRenderer renderer, SCNScene scene, double timeInSeconds) =>  _sceneView.OnWillRenderScene(renderer, scene, timeInSeconds);

            public override void DidRenderScene(ISCNSceneRenderer renderer, SCNScene scene, double timeInSeconds) => _sceneView?.ARSCNViewDidRenderScene?.Invoke(this, new ARSCNViewRenderSceneEventArgs(renderer, scene, timeInSeconds));

            public override void DidAddNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
            {
                if (anchor is ARPlaneAnchor planeAnchor)
                {
                    var plane = new Plane(planeAnchor, renderer) { Hidden = !_sceneView.RenderPlanes };
                    _planes[planeAnchor.Identifier] = plane;
                    node.AddChildNode(plane);
                    if (_planes.Count == 1)
                        _sceneView?.RaisePlanesDetectedChanged(true);
                }
                _sceneView?.ARSCNViewDidAddNode?.Invoke(_sceneView, new ARSCNViewNodeEventArgs(renderer, node, anchor));
            }

            public override void WillUpdateNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor) => _sceneView?.ARSCNViewWillUpdateNode?.Invoke(_sceneView, new ARSCNViewNodeEventArgs(renderer, node, anchor));

            public override void DidUpdateNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
            {
                if (_sceneView.RenderPlanes && anchor is ARPlaneAnchor planeAnchor && _planes.ContainsKey(anchor.Identifier))
                {
                    var plane = _planes[anchor.Identifier];
                    plane.Update(planeAnchor, renderer);
                }
                _sceneView?.ARSCNViewDidUpdateNode?.Invoke(_sceneView, new ARSCNViewNodeEventArgs(renderer, node, anchor));
            }

            public override void DidRemoveNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
            {
                if (_sceneView.RenderPlanes && anchor is ARPlaneAnchor planeAnchor && _planes.ContainsKey(anchor.Identifier))
                {
                    _planes.Remove(anchor.Identifier, out Plane? plane);
                    if (_planes.Count == 0)
                        _sceneView?.RaisePlanesDetectedChanged(false);
                }
                _sceneView?.ARSCNViewDidRemoveNode?.Invoke(_sceneView, new ARSCNViewNodeEventArgs(renderer, node, anchor));
            }

            public override void CameraDidChangeTrackingState(ARSession session, ARCamera camera) => _sceneView?.OnCameraDidChangeTrackingState(session, camera);

            public override void WasInterrupted(ARSession session) => _sceneView?.ARSCNViewWasInterrupted?.Invoke(_sceneView, EventArgs.Empty);

            public override void InterruptionEnded(ARSession session) => _sceneView?.ARSCNViewInterruptionEnded?.Invoke(_sceneView, EventArgs.Empty);


            internal void OnStop()
            {
                if (_planes.Count > 0)
                    _sceneView?.RaisePlanesDetectedChanged(false);
                _planes.Clear();
            }

            internal void TogglePlanes(bool renderPlanes)
            {
                foreach (var plane in _planes.Values.ToArray())
                {
                    plane.Hidden = !renderPlanes;
                }
            }

            private class Plane : SCNNode
            {
                private SCNNode? node;
                private SCNMaterial? material;
                private static UIImage img = UIImage.FromResource(typeof(Plane).Assembly, "Esri.ArcGISRuntime.ARToolkit.GridDot.png");

                public Plane(ARPlaneAnchor anchor, ISCNSceneRenderer renderer)
                {
#if NETCOREAPP
                    var device = renderer.Device;
#else
                    var device = renderer.GetDevice();
#endif
                    if (device != null)
                    {
                        var planeGeometry = ARSCNPlaneGeometry.Create(device);
                        if (planeGeometry != null)
                        {
                            planeGeometry.Update(anchor.Geometry);
                            node = SCNNode.FromGeometry(planeGeometry);
                            node.Geometry = planeGeometry;
                            node.Opacity = 1f;
                            material = new SCNMaterial()
                            {
                                DoubleSided = false
                            };
                            material.Diffuse.Contents = img;
                            material.Diffuse.WrapS = SCNWrapMode.Repeat;
                            material.Diffuse.WrapT = SCNWrapMode.Repeat;
                            planeGeometry.Materials = new[] { material };
                            UpdateMaterial(anchor);
                            AddChildNode(node);
                        }
                    }
                }

                public void Update(ARPlaneAnchor anchor, ISCNSceneRenderer renderer)
                {
                    ARPlaneGeometry geometry = anchor.Geometry;
#if NETCOREAPP
                    var device = renderer.Device;
#else
                    var device = renderer.GetDevice();
#endif
                    if (device != null)
                    {

                        ARSCNPlaneGeometry? planeGeometry = ARSCNPlaneGeometry.Create(device);
                        if (planeGeometry != null && material != null && node != null)
                        {
                            planeGeometry.Update(geometry);
                            planeGeometry.Materials = new[] { material };
                            UpdateMaterial(anchor);
                            node.Geometry = planeGeometry;
                        }
                    }
                }

                private void UpdateMaterial(ARPlaneAnchor anchor)
                {
                    // Scale the material to be 10x10cm
                    // Texture used is 1.732x taller than wide
                    if (material != null)
                        material.Diffuse.ContentsTransform = SCNMatrix4.Scale(anchor.Extent.X / .1f, anchor.Extent.Z / .1f / 1.732f, 0);
                }
            }
        }
    }
}
#endif