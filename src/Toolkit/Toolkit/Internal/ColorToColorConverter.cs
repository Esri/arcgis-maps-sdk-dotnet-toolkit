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

#if WINDOWS

using System;
using System.Collections;
using System.Globalization;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using MediaColor = Windows.UI.Color;
#else
using System.Windows.Data;
using MediaColor = System.Windows.Media.Color;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal class ColorToColorConverter : IValueConverter
    {
        object IValueConverter.Convert(object? value, Type targetType, object? parameter,
#if NETFX_CORE
            string language)
#else
            CultureInfo culture)
#endif
        {
            if (value is MediaColor mediaColor && targetType == typeof(System.Drawing.Color))
            {
                return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
            }
            else if (value is System.Drawing.Color drawingColor && targetType == typeof(MediaColor))
            {
                return MediaColor.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            }

            return value;
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