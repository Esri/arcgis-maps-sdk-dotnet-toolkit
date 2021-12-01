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
using System;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Culture = System.String;
#else
using System.Windows;
using System.Windows.Data;
using Culture = System.Globalization.CultureInfo;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Converts a boolean value to a visibility. Specify 'Inverse' as parameter to invert result.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    #if NETFX_CORE
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:Use built-in type alias", Justification = "Alias used to support UWP/WPF differences.")]
    #endif
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, Culture culture)
        {
            if (value is bool inputValue)
            {
                if ("Inverse".Equals(parameter))
                {
                    return inputValue ? Visibility.Collapsed : Visibility.Visible;
                }

                return inputValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, Culture culture)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
