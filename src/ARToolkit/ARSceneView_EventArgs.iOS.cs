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
using SceneKit;

namespace Esri.ArcGISRuntime.ARToolkit
{
    /// <summary>
    /// Events args for the <see cref="ARSCNViewDelegate"/> rendering events
    /// </summary>
    /// <seealso cref="ARSceneView.ARSCNViewWillRenderScene"/>
    /// <seealso cref="ARSceneView.ARSCNViewDidRenderScene"/>
    public class ARSCNViewRenderSceneEventArgs : EventArgs
    {
        internal ARSCNViewRenderSceneEventArgs(ISCNSceneRenderer renderer, SCNScene scene, double timeInSeconds)
        {
            Renderer = renderer;
            Scene = scene;
            Time = TimeSpan.FromSeconds(timeInSeconds);
        }

        /// <summary>
        /// Gets the scene renderer
        /// </summary>
        public ISCNSceneRenderer Renderer { get; }

        /// <summary>
        /// Gets the scene
        /// </summary>
        public SCNScene Scene { get; }

        /// <summary>
        /// Gets the time stamp
        /// </summary>
        public TimeSpan Time { get; }
    }

    /// <summary>
    /// Event argument for the <see cref="ARSessionDelegate.DidUpdateFrame(ARSession, ARFrame)"/> delegate method.
    /// </summary>
    /// <seealso cref="ARSceneView.ARSCNViewWillRenderScene"/>
    public class ARSessionFrameEventArgs : EventArgs
    {
        internal ARSessionFrameEventArgs(ARSession session, ARFrame frame)
        {
            Session = session;
            Frame = frame;
        }

        /// <summary>
        /// The AR Session
        /// </summary>
        public ARSession Session { get; }

        /// <summary>
        /// The AR Frame
        /// </summary>
        public ARFrame Frame { get; }
    }

    /// <summary>
    /// Event arguments for the node events of the <see cref="ARSCNViewDelegate"/> node events.
    /// </summary>
    /// <seealso cref="ARSceneView.ARSCNViewDidAddNode"/>
    /// <seealso cref="ARSceneView.ARSCNViewDidRemoveNode"/>
    /// <seealso cref="ARSceneView.ARSCNViewDidUpdateNode"/>
    /// <seealso cref="ARSceneView.ARSCNViewWillUpdateNode"/>
    public class ARSCNViewNodeEventArgs : EventArgs
    {
        internal ARSCNViewNodeEventArgs(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            Renderer = renderer;
            Node = node;
            Anchor = anchor;
        }

        /// <summary>
        /// Gets the scene renderer
        /// </summary>
        public ISCNSceneRenderer Renderer { get; }

        /// <summary>
        /// Gets the node
        /// </summary>
        public SCNNode Node { get; }

        /// <summary>
        /// Gets the anchor
        /// </summary>
        public ARAnchor Anchor { get; }
    }

    /// <summary>
    /// Event arguments for the <see cref="ARSCNViewDelegate.CameraDidChangeTrackingState(ARSession, ARCamera)"/> delegate method.
    /// </summary>
    public class ARSCNViewCameraTrackingStateChangedEventArgs : EventArgs
    {
        internal ARSCNViewCameraTrackingStateChangedEventArgs(ARSession session, ARCamera camera)
        {
            Session = session;
            Camera = camera;
        }

        /// <summary>
        /// Gets the AR Session
        /// </summary>
        public ARSession Session { get; }

        /// <summary>
        /// Gets the AR Camera
        /// </summary>
        public ARCamera Camera { get; }
    }
}
#endif