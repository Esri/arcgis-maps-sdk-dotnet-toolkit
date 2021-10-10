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
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UtilityNetworks;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#elif !XAMARIN_FORMS
using Esri.ArcGISRuntime.UI.Controls;
#endif

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Esri.ArcGISRuntime.Toolkit.Xamarin.Forms")]

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Internal interface enables code sharing for <see cref="UtilityNetworkTraceToolController" /> with Forms- and Windows-specific UtilityNetworkTraceTool implementations.
    /// </summary>
    internal interface IUtilityNetworkTraceTool
    {
        public GeoView? GeoView { get; set; }

        public IList<ArcGISFeature>? StartingPoints { get; set; }

        public bool IsAddingStartingPoints { get; set; }

        public bool AutoZoomToTraceResults { get; set; }

        public Symbol? StartingPointSymbol { get; set; }

        public Symbol? ResultPointSymbol { get; set; }

        public Symbol? ResultLineSymbol { get; set; }

        public Symbol? ResultFillSymbol { get; set; }

        internal void SelectUtilityNetwork(UtilityNetwork? utilityNetwork);

        internal void SelectTraceType(UtilityNamedTraceConfiguration? traceType);

        internal void SelectStartingPoint(StartingPointModel? startingPoint);

        internal void EnableTrace(bool isTraceEnabled);

        internal void SetIsBusy(bool isBusy);

        internal void SetStatus(string status);

        internal void NotifyUtilityNetworkChanged(UtilityNetwork utilityNetwork);

        internal void NotifyUtilityNetworkTraceCompleted(UtilityTraceParameters parameters,
            IEnumerable<UtilityTraceResult>? results, Exception? error);
    }
}