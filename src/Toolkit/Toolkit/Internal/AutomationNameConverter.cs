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

#if !MAUI
using System;
#if WPF
using System.Windows.Data;
using Culture = System.Globalization.CultureInfo;
#elif WINUI
using Microsoft.UI.Xaml.Data;
using Culture = System.Globalization.CultureInfo;
#endif
namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Combines display title and subtitle into a single automation name.
    /// If subtitle is empty, returns only the title.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#if WPF
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
#elif WINUI
    public sealed partial class AutomationNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return string.Empty;

            var type = value.GetType();

            var titleProp = type.GetProperty("DisplayTitle");
            var subtitleProp = type.GetProperty("DisplaySubtitle");

            string title = titleProp?.GetValue(value)?.ToString();
            string subtitle = subtitleProp?.GetValue(value)?.ToString();

            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(subtitle))
                return $"{title}, {subtitle}";

            return title;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }

    }
#endif
}
#endif