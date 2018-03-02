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

#if !XAMARIN

using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Data;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal static class FeatureHelper
    {
        public static Field GetField(this Feature feature, string fieldName)
        {
            return feature?.FeatureTable?.Fields?.FirstOrDefault(f => f.Name == fieldName);
        }

        public static CodedValueDomain GetCodedValueDomain(this Field field)
        {
            return field?.Domain as CodedValueDomain;
        }

        public static object GetDisplayValue(this Field field, object value)
        {
            var cvd = field?.GetCodedValueDomain();
            if (cvd == null)
            {
                return value;
            }

            var codeValue = cvd.CodedValues?.FirstOrDefault(c => c.Code != null && c.Code.Equals(value));
            return codeValue?.Name ?? value;
        }
    }
}
#endif