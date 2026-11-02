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

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

/// <summary>
/// The colors to use while a system contrast setting is on.
/// </summary>
/// <remarks>
/// On Windows these are the high contrast theme's system colors. iOS ("Increase Contrast") and Android ("Contrast" / "High contrast text")
/// have no system palette, so there they are solid black and white matching the app theme.
/// </remarks>
internal sealed record HighContrastColors(Color Foreground, Color Background, Color Highlight, Color HighlightForeground, Color Disabled)
{
    public Color Get(HighContrastColorRole role) => role switch
    {
        HighContrastColorRole.Background => Background,
        HighContrastColorRole.Highlight => Highlight,
        HighContrastColorRole.HighlightForeground => HighlightForeground,
        HighContrastColorRole.Disabled => Disabled,
        _ => Foreground,
    };

    internal static HighContrastColors FromAppTheme()
    {
        var isDark = Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
        var foreground = isDark ? Colors.White : Colors.Black;
        var background = isDark ? Colors.Black : Colors.White;
        return new HighContrastColors(foreground, background, foreground, background, foreground);
    }
}
