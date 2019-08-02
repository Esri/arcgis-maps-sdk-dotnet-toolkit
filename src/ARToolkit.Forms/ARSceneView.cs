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

using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.ARToolkit.Forms
{
    /// <summary>
    /// The Augmented Reality-enabled SceneView control
    /// </summary>
    public class ARSceneView : Esri.ArcGISRuntime.Xamarin.Forms.SceneView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        public ARSceneView()
            : base()
        {
            SpaceEffect = UI.SpaceEffect.None;
            AtmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.None;
            OriginCamera = new Mapping.Camera(0, 0, 15E6, 0, 0, 0);
        }

        /// <summary>
        /// Identifies the <see cref="TranslationFactor"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TranslationFactorProperty =
            BindableProperty.Create(nameof(TranslationFactor), typeof(double), typeof(ARSceneView), 1d, BindingMode.TwoWay, null);

        public double TranslationFactor
        {
            get { return (double)GetValue(TranslationFactorProperty); }
            set { SetValue(TranslationFactorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OriginCamera"/> bindable property.
        /// </summary>
        public static readonly BindableProperty OriginCameraProperty =
            BindableProperty.Create(nameof(OriginCamera), typeof(Mapping.Camera), typeof(ARSceneView), null, BindingMode.TwoWay, null);

        public Mapping.Camera OriginCamera
        {
            get { return (Mapping.Camera)GetValue(OriginCameraProperty); }
            set { SetValue(OriginCameraProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RenderVideoFeed"/> bindable property.
        /// </summary>
        public static readonly BindableProperty RenderVideoFeedProperty =
            BindableProperty.Create(nameof(RenderVideoFeed), typeof(bool), typeof(ARSceneView), true, BindingMode.TwoWay, null);

        public bool RenderVideoFeed
        {
            get { return (bool)GetValue(RenderVideoFeedProperty); }
            set { SetValue(RenderVideoFeedProperty, value); }
        }

        public void StartTracking()
        {
            MessagingCenter.Send(this, "StartTracking");
        }

        public void StopTracking()
        {
            MessagingCenter.Send(this, "StopTracking");
        }

        public void ResetTracking()
        {
            MessagingCenter.Send(this, "StopTracking");
        }

        public Geometry.MapPoint ARScreenToLocation(Point screenPoint)
        {
            throw new NotImplementedException("TODO");
        }

        public void SetInitialTransformation(Mapping.TransformationMatrix transformationMatrix)
        {
            MessagingCenter.Send(this, "SetInitialTransformation", transformationMatrix);
        }

        public bool SetInitialTransformation(Point screenLocation)
        {
            throw new NotImplementedException("TODO");
        }
    }
}
