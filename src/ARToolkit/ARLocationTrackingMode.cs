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

namespace Esri.ArcGISRuntime.ARToolkit
{
    /// <summary>
    /// Controls how the locations generated from the location data source are used during AR tracking.
    /// </summary>
    public enum ARLocationTrackingMode
    {
        /// <summary>
        /// Ignore all location data source locations.
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// Use only the initial location from the location data source and ignore all subsequent locations.
        /// </summary>
        Initial = 1,

        /// <summary>
        /// Use all locations from the location data source.
        /// </summary>
        Continuous = 2,

        /// <summary>
        /// Enable Visual Positioning Systems with continuous location updates.
        /// </summary>
        ContinuousWithVPS = 3
    }
}