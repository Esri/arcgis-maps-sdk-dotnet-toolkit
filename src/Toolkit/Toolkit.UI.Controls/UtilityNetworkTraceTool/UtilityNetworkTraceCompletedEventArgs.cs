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
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Event argument used by <see cref="UtilityNetworkTraceTool.UtilityNetworkTraceCompleted"/> event.
    /// </summary>
    public sealed class UtilityNetworkTraceCompletedEventArgs : EventArgs
    {
        internal UtilityNetworkTraceCompletedEventArgs(UtilityTraceParameters parameters, IEnumerable<UtilityTraceResult> results)
        {
            Parameters = parameters;
            Results = results;
        }

        internal UtilityNetworkTraceCompletedEventArgs(UtilityTraceParameters parameters, Exception error)
        {
            Parameters = parameters;
            Error = error;
        }

        /// <summary>
        /// Gets the <see cref="UtilityTraceParameters"/> used by trace.
        /// </summary>
        /// <value>The <see cref="UtilityTraceParameters"/> used by trace.</value>
        public UtilityTraceParameters Parameters { get; }

        /// <summary>
        /// Gets the <see cref="IEnumerable{UtilityTraceResult}"/> returned by trace.
        /// </summary>
        /// <value>The <see cref="IEnumerable{UtilityTraceResult}"/> returned by trace.</value>
        public IEnumerable<UtilityTraceResult>? Results { get; }

        /// <summary>
        /// Gets the <see cref="Exception"/> raised by trace.
        /// </summary>
        /// <value>The <see cref="Exception"/> raised by trace.</value>
        public Exception? Error { get; }
    }
}