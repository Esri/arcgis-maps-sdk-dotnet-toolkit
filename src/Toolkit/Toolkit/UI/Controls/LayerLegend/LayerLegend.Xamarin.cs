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

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class LayerLegend
    {
        private ILayerContent _layerContent;

        /// <summary>
        /// Gets or sets the layer to display the legend for.
        /// </summary>
        private ILayerContent LayerContentImpl
        {
            get => _layerContent;
            set
            {
                if (_layerContent != value)
                {
                    _layerContent = value;
                    Refresh();
                }
            }
        }

        private bool _includeSublayers = true;

        /// <summary>
        /// Gets or sets a value indicating whether the entire <see cref="ILayerContent"/> tree hierarchy should be rendered.
        /// </summary>
        private bool IncludeSublayersImpl
        {
            get => _includeSublayers;
            set
            {
                if (_includeSublayers != value)
                {
                    _includeSublayers = value;
                    Refresh();
                }
            }
        }
    }
}
#endif