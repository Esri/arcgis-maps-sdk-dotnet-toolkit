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

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that renders a <see cref="Symbology.Symbol"/>.
    /// </summary>
    public partial class SymbolDisplay
    {
        private Symbology.Symbol? _symbol;

        /// <summary>
        /// Gets or sets the symbol to render.
        /// </summary>
        private Symbology.Symbol? SymbolImpl
        {
            get => _symbol;
            set
            {
                if (_symbol != value)
                {
                    var oldValue = _symbol;
                    _symbol = value;
                    OnSymbolChanged(oldValue, _symbol);
                }
            }
        }
    }
}
#endif