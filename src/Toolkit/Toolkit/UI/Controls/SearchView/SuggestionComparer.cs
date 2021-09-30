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

using System.Collections.Generic;
using Esri.ArcGISRuntime.Tasks.Geocoding;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Compares two suggestion or geocode results to determine if they are equivalent.
    /// </summary>
    /// <typeparam name="T">GeocodeResult or SuggestResult.</typeparam>
    public class SuggestionComparer<T> : IEqualityComparer<T>
    {
        /// <inheritdoc/>
        public bool Equals(T? x, T? y)
        {
            if (x is SuggestResult r1 && y is SuggestResult r2)
            {
                return r1.IsCollection == r2.IsCollection && r1.Label == r2.Label;
            }
            else if (x is GeocodeResult gr1 && y is GeocodeResult gr2)
            {
                // TODO - is this enough?
                return gr1.DisplayLocation == gr2.DisplayLocation && gr1.Label == gr2.Label;
            }

            return false;
        }

        /// <inheritdoc/>
        public int GetHashCode(T? obj)
        {
            if (obj is SuggestResult sr)
            {
                return sr.Label.GetHashCode() * (sr.IsCollection ? 1 : -1);
            }
            else if (obj is GeocodeResult gr)
            {
                return gr.DisplayLocation?.GetHashCode() ?? 0;
            }

            return obj?.GetHashCode() ?? 0;
        }
    }
}
