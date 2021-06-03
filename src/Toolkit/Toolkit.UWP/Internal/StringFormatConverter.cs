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

using System;
using System.ComponentModel;
using Windows.UI.Xaml.Data;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Allows converting an object to a formatted string representation.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StringFormatConverter : IValueConverter
    {
        /// <inheritdoc />
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter is string formatString)
            {
                try
                {
                    return string.Format(formatString, value);
                }
                catch
                {
                    // If the specified string format is invalid, just return the bound value
                    return value;
                }
            }
            else
            {
                return value;
            }
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
