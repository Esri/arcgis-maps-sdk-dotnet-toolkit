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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Models a starting point used for a utility network trace.
    /// </summary>
    internal class StartingPointModel : UtilityElementModel
    {
        internal StartingPointModel(UtilityNetworkTraceToolController controller, UtilityElement element, Graphic selectionGraphic, Feature feature, Envelope? zoomToExtent)
            : base(element, selectionGraphic, feature, zoomToExtent, model => controller.StartingPoints.Remove((StartingPointModel)model))
        {
        }

        /// <summary>
        /// Gets the underlying utility element.
        /// </summary>
        public UtilityElement StartingPoint => Element;

    }
}