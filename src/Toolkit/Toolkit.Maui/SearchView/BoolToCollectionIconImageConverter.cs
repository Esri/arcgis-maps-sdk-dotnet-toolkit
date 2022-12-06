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
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal class BoolToCollectionIconImageConverter : IValueConverter
{
    /// <summary>
    /// Converts a bool to an icon representing either a search (true) or an individual result (false).
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolvalue && boolvalue)
        {
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                return ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Maui.Assets.search-small.png", Assembly.GetAssembly(typeof(BoolToCollectionIconImageConverter)));
            }

            return ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Maui.Assets.search.png", Assembly.GetAssembly(typeof(BoolToCollectionIconImageConverter)));
        }

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            return ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Maui.Assets.pin-tear-small.png", Assembly.GetAssembly(typeof(BoolToCollectionIconImageConverter)));
        }

        return ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Maui.Assets.pin-tear.png", Assembly.GetAssembly(typeof(BoolToCollectionIconImageConverter)));
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
