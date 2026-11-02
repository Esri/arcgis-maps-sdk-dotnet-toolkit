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

using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal static class HighContrastThemeExtensions
{
    /// <summary>
    /// Binds a color (or brush) property to <paramref name="role"/> while a contrast setting is on, and to the light or dark color otherwise.
    /// </summary>
    /// <remarks>A <c>null</c> light or dark color leaves the property unset outside high contrast.</remarks>
    public static void SetHighContrastColor(this BindableObject target, BindableProperty property, Color? light, Color? dark, HighContrastColorRole role = HighContrastColorRole.Foreground)
        => target.SetBinding(property, CreateBinding(light, dark, role));

    /// <inheritdoc cref="SetHighContrastColor(BindableObject, BindableProperty, Color?, Color?, HighContrastColorRole)"/>
    public static void SetHighContrastColor(this BindableObject target, BindableProperty property, Color? color, HighContrastColorRole role = HighContrastColorRole.Foreground)
        => target.SetBinding(property, CreateBinding(color, color, role));

    /// <summary>
    /// Creates a style setter with the same behavior as <see cref="SetHighContrastColor(BindableObject, BindableProperty, Color?, HighContrastColorRole)"/>.
    /// </summary>
    public static Setter CreateHighContrastSetter(BindableProperty property, Color? color, HighContrastColorRole role = HighContrastColorRole.Foreground)
        => new Setter() { Property = property, Value = CreateBinding(color, color, role) };

    private static Binding CreateBinding(Color? light, Color? dark, HighContrastColorRole role)
        => new Binding(nameof(HighContrastTheme.Colors), source: HighContrastTheme.Instance, converter: new HighContrastColorConverter(light, dark, role));

    private sealed class HighContrastColorConverter(Color? light, Color? dark, HighContrastColorRole role) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var color = value is HighContrastColors colors
                ? colors.Get(role)
                : Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark ? dark : light;
            if (color is null)
            {
                return null;
            }

            return typeof(Brush).IsAssignableFrom(targetType) ? new SolidColorBrush(color) : color;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
