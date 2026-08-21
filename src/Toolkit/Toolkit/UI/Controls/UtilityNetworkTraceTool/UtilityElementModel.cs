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
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Models a utility element used as an input to a utility network trace.
    /// </summary>
    internal abstract class UtilityElementModel
    {
        private Action<UtilityElementModel>? _deleteAction;

        internal UtilityElementModel(UtilityElement element, Graphic selectionGraphic, Feature feature, Envelope? zoomToExtent, Action<UtilityElementModel> deleteAction)
        {
            Element = element;
            SelectionGraphic = selectionGraphic;
            ZoomToExtent = zoomToExtent;
            _deleteAction = deleteAction;
            DeleteCommand = new DelegateCommand(() =>
            {
                _deleteAction!.Invoke(this);
                _deleteAction = null;
            });

            // Create popup if possible.
            if (feature is ArcGISFeature arcFeature)
            {
                Popup = new Popup(feature, arcFeature.FeatureTable?.PopupDefinition);
            }

            // Get symbol if possible
            if (feature.FeatureTable?.Layer is FeatureLayer featureLayer && featureLayer.Renderer?.GetSymbol(feature) is Symbol symbol)
            {
                Symbol = symbol;
            }
        }

        public ICommand DeleteCommand { get; set; }

        /// <summary>
        /// Gets the graphic representing the utility element, which may have different geometry from the <see cref="UtilityElement"/>.
        /// </summary>
        /// <remarks>
        /// In the case of line elements, the graphic will be a point on the line. Setting <see cref="FractionAlongEdge"/> will change the graphic's geometry.
        /// </remarks>
        public Graphic SelectionGraphic { get; }

        /// <summary>
        /// Gets the extent used for zooming to the utility element.
        /// </summary>
        public Envelope? ZoomToExtent { get; }

        /// <summary>
        /// Gets the symbol used to visually identify the utility element in a list.
        /// </summary>
        public Symbol? Symbol { get; }

        /// <summary>
        /// Gets the underlying utility element.
        /// </summary>
        public UtilityElement Element { get; }

        /// <summary>
        /// Gets the popup for the utility element.
        /// </summary>
        /// <remarks>
        /// The popup can be used to inspect and differentiate utility elements.
        /// </remarks>
        public Popup? Popup { get; }

        /// <summary>
        /// Gets or sets the utility element's location along a line element. Setting this value will update the geometry of <see cref="SelectionGraphic"/>.
        /// </summary>
        public double FractionAlongEdge
        {
            get => Element.FractionAlongEdge;
            set
            {
                if (Element.FractionAlongEdge != value)
                {
                    Element.FractionAlongEdge = value;
                }

                if (SelectionGraphic != null && SelectionGraphic.Attributes.TryGetValue("Geometry", out var jsonObj) && jsonObj is string jsonString && Geometry.Geometry.FromJson(jsonString) is Polyline originalLine)
                {
                    SelectionGraphic.Geometry = GeometryEngine.CreatePointAlong(originalLine, GeometryEngine.Length(originalLine) * FractionAlongEdge);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether users should be allowed to specify a terminal for the utility element.
        /// </summary>
        public bool TerminalPickerVisible
        {
            get
            {
                if (Element?.AssetType?.TerminalConfiguration is UtilityTerminalConfiguration terminalConfig)
                {
                    return terminalConfig.Terminals.Count > 1;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether users should be allowed to specify a position along a line feature for the utility element.
        /// </summary>
        public bool FractionSliderVisible
        {
            get
            {
                return Element?.NetworkSource?.SourceType == UtilityNetworkSourceType.Edge;
            }
        }
    }
}