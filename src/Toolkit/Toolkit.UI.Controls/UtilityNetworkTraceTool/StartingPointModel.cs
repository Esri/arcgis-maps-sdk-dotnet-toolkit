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
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Models a starting point used for a utility network trace.
    /// </summary>
    internal class StartingPointModel : IEquatable<StartingPointModel>
    {
        private UtilityNetworkTraceToolController _controller;

        internal StartingPointModel(UtilityNetworkTraceToolController controller, UtilityElement element, Graphic selectionGraphic, Feature feature, Envelope? zoomToExtent)
        {
            _controller = controller;
            StartingPoint = element;
            SelectionGraphic = selectionGraphic;
            ZoomToExtent = zoomToExtent;
            DeleteCommand = new DelegateCommand(() =>
            {
                _controller.StartingPoints.Remove(this);
                _controller = null;
            });

            // Create popup if possible.
            if (feature is ArcGISFeature arcFeature && arcFeature.FeatureTable?.PopupDefinition is PopupDefinition featurePopupDef)
            {
                PopupManager = new PopupManager(new Popup(feature, featurePopupDef));
            }

            // Get symbol if possible
            if (feature.FeatureTable?.Layer is FeatureLayer featureLayer && featureLayer.Renderer?.GetSymbol(feature) is Symbol symbol)
            {
                Symbol = symbol;
            }
        }

        public ICommand DeleteCommand { get; set; }

        /// <summary>
        /// Gets the graphic representing the starting point, which may have different geometry from the <see cref="UtilityElement"/>.
        /// </summary>
        /// <remarks>
        /// In the case of line elements, the starting point will be a point on the line. Setting <see cref="FractionAlongEdge"/> will change the graphic's geometry.
        /// </remarks>
        public Graphic SelectionGraphic { get; }

        /// <summary>
        /// Gets the extent used for zooming to the starting point.
        /// </summary>
        public Envelope? ZoomToExtent { get; }

        /// <summary>
        /// Gets the symbol used to visually identify the starting point in a list.
        /// </summary>
        public Symbol? Symbol { get; }

        /// <summary>
        /// Gets the underlying utility element.
        /// </summary>
        public UtilityElement StartingPoint { get; }

        /// <summary>
        /// Gets the popup manager for the starting point.
        /// </summary>
        /// <remarks>
        /// The popup can be used to inspect and differentiate starting points.
        /// </remarks>
        public PopupManager? PopupManager { get; }

        /// <summary>
        /// Gets or sets the starting point's location along a line element. Setting this value will update the geometry of <see cref="SelectionGraphic"/>.
        /// </summary>
        public double FractionAlongEdge
        {
            get => StartingPoint.FractionAlongEdge;
            set
            {
                if (StartingPoint.FractionAlongEdge != value)
                {
                    StartingPoint.FractionAlongEdge = value;
                }

                if (SelectionGraphic != null && SelectionGraphic.Attributes.TryGetValue("Geometry", out var jsonObj) && jsonObj is string jsonString && Geometry.Geometry.FromJson(jsonString) is Polyline originalLine)
                {
                    SelectionGraphic.Geometry = GeometryEngine.CreatePointAlong(originalLine, GeometryEngine.Length(originalLine) * FractionAlongEdge);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether users should be allowed to specify a terminal for the starting point.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether users should be allowed to specify a position along a line feature for the starting point.
        /// </summary>
        public bool FractionSliderVisible
        {
            get
            {
                return StartingPoint?.NetworkSource?.SourceType == UtilityNetworkSourceType.Edge;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// This is used internally to enable appropriate warnings for duplicate trace operations.
        /// </remarks>
        public bool Equals(StartingPointModel? other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.FractionAlongEdge == FractionAlongEdge && other.TerminalPickerVisible == TerminalPickerVisible && other.StartingPoint.ObjectId == StartingPoint.ObjectId)
            {
                return true;
            }

            return false;
        }
    }
}