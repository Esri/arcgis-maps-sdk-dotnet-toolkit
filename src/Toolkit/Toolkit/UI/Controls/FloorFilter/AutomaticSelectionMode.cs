﻿// /*******************************************************************************
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

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    #if !XAMARIN && !NETSTANDARD && !WINDOWS_UWP
    /// <summary>
    /// Defines the selection modes for the <see cref="FloorFilter"/>.
    /// </summary>
    #else
    /// <summary>
    /// Defines the selection modes for the FloorFilter.
    /// </summary>
    #endif
    public enum AutomaticSelectionMode
    {
        /// <summary>
        /// Never update selection from the observed viewpoint
        /// </summary>
        Never,

        /// <summary>
        /// Always update selection from the observed viewpoint, and clear selection if there is nothing in view
        /// </summary>
        Always,

        /// <summary>
        /// Always update selection from the observed viewpoint, but don't clear selection if there is no facility/site/level visible
        /// </summary>
        AlwaysNonClearing,
    }
}
