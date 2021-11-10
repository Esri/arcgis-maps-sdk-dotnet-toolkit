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
using System.Windows.Input;
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Represents a model that enables removal of a <see cref="UtilityElement"/> item from a collection.
    /// </summary>
    internal class StartingPointModel
    {
        internal StartingPointModel(UtilityElement startingPoint,
            DelegateCommand updateCommand,
            DelegateCommand deleteCommand)
        {
            StartingPoint = startingPoint;
            UpdateCommand = updateCommand;
            DeleteCommand = deleteCommand;
        }

        public UtilityElement StartingPoint { get; }

        public double FractionAlongEdge
        {
            get => StartingPoint.FractionAlongEdge;
            set
            {
                if (StartingPoint.FractionAlongEdge != value)
                {
                    StartingPoint.FractionAlongEdge = value;
                    UpdateCommand?.Execute(StartingPoint);
                }
            }
        }

        public ICommand UpdateCommand { get; }

        public ICommand DeleteCommand { get; }

        public bool TerminalPickerVisible
        {
            get
            {
                if (StartingPoint?.AssetType?.TerminalConfiguration is UtilityTerminalConfiguration terminalConfig)
                {
                    return terminalConfig.Terminals.Count > 1;
                }

                return false;
            }
        }

        public bool FractionSliderVisible
        {
            get
            {
                return StartingPoint?.NetworkSource?.SourceType == UtilityNetworkSourceType.Edge;
            }
        }
    }
}
#endif