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
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;

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
            CameraController = _controller;
            OnStartTracking();
        }

        public void StopTracking()
        {
            OnStopTracking();
            CameraController = new GlobeCameraController();
        }

        public double TranslationFactor
        {
            get => _controller.TranslationFactor;
            set => _controller.TranslationFactor = value;
        }
    }
}
#endif