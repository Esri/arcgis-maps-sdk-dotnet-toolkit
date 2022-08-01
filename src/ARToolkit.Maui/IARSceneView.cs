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

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.ARToolkit.Maui
{
    public interface IARSceneView : IView
    {
        double ClippingDistance { get; set; }
        TransformationMatrix InitialTransformation { get; }
        LocationDataSource LocationDataSource { get; set; }
        bool NorthAlign { get; set; }
        Camera OriginCamera { get; set; }
        bool RenderPlanes { get; set; }
        bool RenderVideoFeed { get; set; }
        double TranslationFactor { get; set; }

        void OnOriginCameraChanged();
        void OnPlanesDetectedChanged(bool planesDetected);

        //MapPoint? ARScreenToLocation(Point screenPoint);
        //void ResetTracking();
        //bool SetInitialTransformation(Point screenLocation);
        //void SetInitialTransformation(TransformationMatrix transformationMatrix);
        //Task StartTrackingAsync(ARLocationTrackingMode locationTrackingMode = ARLocationTrackingMode.Ignore);
        //Task StopTrackingAsync();
    }
}