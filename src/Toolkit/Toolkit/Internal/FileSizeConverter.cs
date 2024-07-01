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

#if WPF || WINDOWS_XAML || MAUI
using System;
using System.Collections;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Converts an integer number to a readable filesize string.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class FileSizeConverter : IValueConverter
    {
        /// <inheritdoc />
        object IValueConverter.Convert(object? value, Type targetType, object? parameter,
#if WINDOWS_XAML
            string language)
#else
            CultureInfo culture)
#endif
        {
            long size = -1;
            if (value is long longsize)
                size = longsize;
            else if (value is int intsize)
                size = intsize;
            else if (value is short shortsize)
                size = shortsize;
            if(size > 1024L*1024*1024*1024)
                return $"{Math.Round(size / 1024d / 1024 / 1024 / 1024, 2)} TB";
            else if (size > 1024*1024*1024)
                return $"{Math.Round(size / 1024d / 1024 / 1024, 2)} GB";
            else if (size > 1024*1024)
                return $"{Math.Round(size / 1024d / 1024, 2)} MB";
            else if (size > 1024)
                return $"{Math.Round(size / 1024d, 2)} kB";
            else if (size > 0)
                return $"{size} B";
            else
                return string.Empty;
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