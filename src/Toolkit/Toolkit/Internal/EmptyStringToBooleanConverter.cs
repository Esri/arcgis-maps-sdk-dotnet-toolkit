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
#if WINDOWS_XAML
using Culture = System.String;
#else
using System.Windows;
using System.Windows.Data;
using Culture = System.Globalization.CultureInfo;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Converts strings to boolean. Returns true if the string is not empty and false if the string is empty.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#if WINDOWS_XAML
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:Use built-in type alias", Justification = "Alias used to support UWP/WPF differences.")]
#endif
    public partial class EmptyStringToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether the conversion should be inverted. If true, the conversion will return true if is empty and false if not empty. If false, the conversion will return true if not empty and false if empty.
        /// </summary>
        public bool Invert { get; set; }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, Culture culture)
        {
            if (value is string valueString)
            {
                bool result = !string.IsNullOrEmpty(valueString);
                return Invert ? !result : result;
            }

            return false;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, Culture culture)
        {
            throw new NotImplementedException();
        }
    }
}
#endif