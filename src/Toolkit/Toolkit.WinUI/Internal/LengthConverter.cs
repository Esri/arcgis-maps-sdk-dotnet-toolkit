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

using System;
using System.ComponentModel;
using Windows.UI.Xaml.Data;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Converts the length to a string.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial class LengthFormatConverter : IValueConverter
    {
        /// <inheritdoc />
#pragma warning disable CA1033 // Interface methods should be callable by child types: Solution is to seal the class, but this would be a binary breaking change, so disabling warning instead
        object? IValueConverter.Convert(object? value, Type targetType, object? parameter, string language)
#pragma warning restore CA1033 // Interface methods should be callable by child types
        {
            if (value is null)
                return 0;
            if (value is string str)
                return str.Length;
            if (value is Array array)
                return array.Length;
            if (value is System.Collections.ICollection coll)
                return coll.Count;

            return value;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
