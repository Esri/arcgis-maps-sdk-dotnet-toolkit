﻿// /*******************************************************************************
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
using System;
using System.Collections;
using System.Globalization;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Returns visible status for positive boolean, non-null text and opposite state for visibility value.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class ListSizeVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        object IValueConverter.Convert(object? value, Type targetType, object? parameter,
#if NETFX_CORE
            string language)
#else
            CultureInfo culture)
#endif
        {
            if (parameter is string count)
            {
                int comparisonValue;
                if (value is int rawInt)
                {
                    comparisonValue = rawInt;
                }
                else if (value is IList list && list.Count is int countInt)
                {
                    comparisonValue = countInt;
                }
                else
                {
                    return Visibility.Collapsed;
                }

                if (int.TryParse(count, out var result) && comparisonValue == result)
                {
                    return Visibility.Visible;
                }
                else if (parameter?.ToString() == "any" && comparisonValue > 0)
                {
                    return Visibility.Visible;
                }
                else if (parameter?.ToString() == "multiple" && comparisonValue > 1)
                {
                    return Visibility.Visible;
                }
                else if (parameter?.ToString() == "none" && comparisonValue == 0)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter,
#if NETFX_CORE
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