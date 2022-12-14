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
using System;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Returns visible status for positive boolean, non-null text and opposite state for visibility value.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class VisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        object IValueConverter.Convert(object? value, Type targetType, object? parameter,
#if WINDOWS_XAML
            string language)
#else
            CultureInfo culture)
#endif
        {
            bool isVisible = value != null;

            if (value is bool)
            {
                isVisible = (bool)value;
            }
            else if (value is string)
            {
                isVisible = !string.IsNullOrWhiteSpace((string)value);
            }

            isVisible = parameter?.ToString() == "Reverse" ? !isVisible : isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter,
#if WINDOWS_XAML
            string language)
#else
            CultureInfo culture)
#endif
        {
            if (value is Visibility visibility)
            {
                if (visibility == Visibility.Visible)
                {
                    return true;
                }
                else if (visibility == Visibility.Collapsed)
                {
                    return false;
                }
            }
            throw new NotSupportedException();
        }
    }
}
#endif