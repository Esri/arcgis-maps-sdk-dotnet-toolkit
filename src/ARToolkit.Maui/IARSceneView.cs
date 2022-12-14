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
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.ARToolkit.Maui
{
    /// <summary>
    /// A set of <see cref="ARSceneView"/> members used with the <see cref="Handlers.ARSceneViewHandler"/> class.
    /// </summary>
    /// <remarks>
    /// This API might expand in the future, and implementing it isn't recommended.
    /// </remarks>
    public interface IARSceneView : ArcGISRuntime.Maui.ISceneView
    {
        /// <summary>
        /// Gets or sets the clipping distance from the origin camera, beyond which data will not be displayed.
        /// Defaults to 0.0. When set to 0.0, there is no clipping distance; all data is displayed.
        /// </summary>
        /// <remarks>
        /// You can use clipping distance to limit the display of data in world-scale AR or clip data for tabletop AR.
        /// </remarks>
        double ClippingDistance { get; set; }

        /// <summary>
        /// Gets the initial transformation used for a table top experience.  Defaults to the Identity Matrix.
        /// </summary>
        /// <seealso cref="ARSceneView.SetInitialTransformation(Mapping.TransformationMatrix)"/>
        /// <seealso cref="ARSceneView.SetInitialTransformation(Point)"/>
        TransformationMatrix InitialTransformation { get; }

        /// <summary>
        /// The data source used to get device location.
        /// Used either in conjuction with device camera tracking data or when device camera tracking is not present or not being used.
        /// </summary>
        LocationDataSource LocationDataSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the scene should attempt to use the device compass to align the scene towards north.
        /// </summary>
        /// <remarks>
        /// Note that the accuracy of the compass can heavily affect the quality of alignment.
        /// </remarks>
        bool NorthAlign { get; set; }
        
        /// <summary>
        /// Gets or sets the viewpoint camera used to set the initial view of the sceneView instead of the device's GPS location via the location data source.
        /// </summary>
        Camera OriginCamera { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to render planes that's been detected
        /// </summary>
        bool RenderPlanes { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the background of the <see cref="ARSceneView"/> is transparent or not. Enabling transparency allows for the
        /// camera feed to be visible underneath the <see cref="ARSceneView"/>.
        /// </summary>
        bool RenderVideoFeed { get; set; }
        
        /// <summary>
        /// Gets or sets translation factor used to support a table top AR experience.
        /// </summary>
        /// <remarks>A value of 1 means if the device 1 meter in the real world, it'll move 1 m in the AR world. Set this to 1000 to make 1 m meter 1km in the AR world.</remarks>
        double TranslationFactor { get; set; }

        /// <summary>
        /// Raises the <see cref="ARSceneView.OriginCameraChanged"/> event on the <see cref="ARSceneView"/>.
        /// </summary>
        void OnOriginCameraChanged();

        /// <summary>
        /// Raises the <see cref="ARSceneView.PlanesDetectedChanged"/> event on the <see cref="ARSceneView"/>.
        /// </summary>
        /// <param name="planesDetected">true if planes are detected, false otherwise.</param>
        void OnPlanesDetectedChanged(bool planesDetected);
    }
}