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
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Event arguments used when raising the <see cref="TimeSlider.CurrentExtentChanged"/> event.
    /// </summary>
    public sealed class CurrentExtentChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentExtentChangedEventArgs"/> class.
        /// </summary>
        /// <param name="newExtent">The new <see cref="TimeExtent"/> value.</param>
        /// <param name="oldExtent">The old <see cref="TimeExtent"/> value.</param>
        internal CurrentExtentChangedEventArgs(TimeExtent newExtent, TimeExtent oldExtent)
        {
            NewExtent = newExtent;
            OldExtent = oldExtent;
        }
        /// <summary>
        /// Gets the new <see cref="TimeExtent"/> value.
        /// </summary>
        public TimeExtent NewExtent { get; private set; }

        /// <summary>
        /// Gets the old <see cref="TimeExtent"/> value.
        /// </summary>
        public TimeExtent OldExtent { get; private set; }
    }
}
