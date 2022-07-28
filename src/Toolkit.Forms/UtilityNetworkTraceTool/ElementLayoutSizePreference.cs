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
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// Enumeration of size preferences for a UI element. Used by <see cref="UtilityNetworkTraceTool"/> to indicate when it should be shown in a collapsed or expanded state.
    /// </summary>
    public enum ElementLayoutSizePreference
    {
        /// <summary>
        /// Indicates no preference.
        /// </summary>
        NotSet,

        /// <summary>
        /// Indicates a preference for a collapsed presentation style.
        /// </summary>
        Collapsed,

        /// <summary>
        /// Indicates a preference to focus both the element and the map equally.
        /// </summary>
        Half,

        /// <summary>
        /// Indicates a preference to maximize the element.
        /// </summary>
        Maximized,
    }
}
