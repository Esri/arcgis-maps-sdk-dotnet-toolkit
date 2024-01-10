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

using Microsoft.Maui.ApplicationModel;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal class ByteArrayToImageSourceConverter : IValueConverter
{
    /// <summary>
    /// Converts a byte array to an image source for display in Microsoft .NET MAUI.
    /// </summary>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] rawBuffer)
        {
            return ImageSource.FromStream(() => new MemoryStream(rawBuffer));
        }

        AppTheme currentTheme = Application.Current.RequestedTheme;

        if (currentTheme == AppTheme.Dark)
        {
            return ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Maui.Assets.basemapdark.png", typeof(BasemapGallery).Assembly);
        }

        // Return the placeholder image directly rather than null to work around bugs/limitations in MAUI
        return ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Maui.Assets.basemap.png", typeof(BasemapGallery).Assembly);
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
