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

using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal class EmptyStringToBoolConverter : IValueConverter
{
    private EmptyStringToBoolConverter() { }

    /// <summary>
    /// Converts a string to a bool, true if not empty, false otherwise.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringvalue && !string.IsNullOrEmpty(stringvalue))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    internal static EmptyStringToBoolConverter Instance { get; } = new EmptyStringToBoolConverter();
}
