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


#if WPF
using System;
using System.Windows.Data;
using Culture = System.Globalization.CultureInfo;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Combines display title and subtitle into a single automation name.
    /// If subtitle is empty, returns only the title.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]

    public sealed class AutomationNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, Culture culture)
        {
            var title = values.Length > 0 ? values[0] as string : null;
            var subtitle = values.Length > 1 ? values[1] as string : null;

            if (string.IsNullOrWhiteSpace(subtitle))
            {
                return title;
            }

            return $"{title}, {subtitle}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, Culture culture)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
