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

using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal
{
    internal class EmptyToFalseConverter : IValueConverter
    {
        public static EmptyToFalseConverter Instance { get; } = new EmptyToFalseConverter();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return false;
            if (value is string str) return !string.IsNullOrWhiteSpace(str);
            if (value is System.Collections.ICollection coll) return coll.Count > 0;
            if (value is Array arr) return arr.Length > 0;
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
