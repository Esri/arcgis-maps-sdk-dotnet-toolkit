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

#if !__IOS__ && !__ANDROID__
using System;
using Esri.ArcGISRuntime.UtilityNetworks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Esri.ArcGISRuntime.Toolkit.Xamarin.Forms")]

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Event argument used by <see cref="UtilityNetworkTraceTool.UtilityNetworkChanged"/> event.
    /// </summary>
    public sealed class UtilityNetworkChangedEventArgs : EventArgs
    {
        internal UtilityNetworkChangedEventArgs(UtilityNetwork utilityNetwork)
        {
            UtilityNetwork = utilityNetwork;
        }

        /// <summary>
        /// Gets the new value for <see cref="UtilityNetwork"/>.
        /// </summary>
        /// <value>The new value for <see cref="UtilityNetwork"/>.</value>
        public UtilityNetwork UtilityNetwork { get; }
    }
}
#endif