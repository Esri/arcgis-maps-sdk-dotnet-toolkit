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

#if XAMARIN
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The base class for <see cref="Legend"/>
    /// and TableOfContents control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="Esri.ArcGISRuntime.UI.Controls.GeoView"/>.
    /// </summary>
    public partial class LayerList
    {
        private GeoView _geoView;

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        private GeoView GeoViewImpl
        {
            get => _geoView;
            set
            {
                if (_geoView != value)
                {
                    var oldView = _geoView;
                    _geoView = value;
                    OnViewChanged(oldView, _geoView);
                }
            }
        }

        private bool _filterByVisibleScaleRange = true;

        /// <summary>
        /// Gets or sets a value indicating whether the scale of <see cref="GeoView"/> and any scale ranges on the <see cref="Layer"/>s
        /// are used to determine when legend for layer is displayed.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layer is displayed only when layer is in visible scale range;
        /// otherwise, <c>false</c>, legend for layer is displayed regardless of its scale range.
        /// </remarks>
        private bool FilterByVisibleScaleRangeImpl
        {
            get => _filterByVisibleScaleRange;
            set
            {
                if (_filterByVisibleScaleRange != value)
                {
                    _filterByVisibleScaleRange = value;
                    UpdateLegendVisiblity();
                }
            }
        }

        private bool _reverseLayerOrder = false;

        /// <summary>
        /// Gets or sets a value indicating whether the order of layers in the <see cref="GeoView"/>, top to bottom, is used.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layers is displayed from top to bottom order;
        /// otherwise, <c>false</c>, legend for layers is displayed from bottom to top order.
        /// </remarks>
        private bool ReverseLayerOrderImpl
        {
            get => _reverseLayerOrder;
            set
            {
                if (_reverseLayerOrder != value)
                {
                    _reverseLayerOrder = value;
                    if (_layerContentList != null)
                    {
                        _layerContentList.ReverseOrder = !value;
                    }
                }
            }
        }
    }
}
#endif