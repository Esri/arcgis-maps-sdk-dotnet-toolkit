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

#if WPF || WINDOWS_XAML
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Returns visible status for positive boolean, non-null text and opposite state for visibility value.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed partial class NullToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        object IValueConverter.Convert(object? value, Type targetType, object? parameter,
#if WINDOWS_XAML
            string language)
#else
            CultureInfo culture)
#endif
        {
            if (parameter is string preference)
            {
                if (preference.Equals("null", StringComparison.OrdinalIgnoreCase) && value == null)
                {
                    return Visibility.Visible;
                }
                else if (preference.Equals("notnull", StringComparison.OrdinalIgnoreCase) && value != null)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter,
#if WINDOWS_XAML
            string language)
#else
            CultureInfo culture)
#endif
        {
            throw new NotSupportedException();
        }
    }
}
#endif