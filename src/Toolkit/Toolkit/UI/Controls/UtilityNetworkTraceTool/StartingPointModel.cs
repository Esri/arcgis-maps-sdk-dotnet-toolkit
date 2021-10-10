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
            DelegateCommand zoomToCommand,
            DelegateCommand deleteCommand)
        {
            StartingPoint = startingPoint;
            UpdateCommand = updateCommand;
            ZoomToCommand = zoomToCommand;
            DeleteCommand = deleteCommand;
        }

        internal UtilityElement StartingPoint { get; }

        internal double FractionAlongEdge
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

        internal ICommand UpdateCommand { get; }

        internal ICommand ZoomToCommand { get; }

        internal ICommand DeleteCommand { get; }
    }
}