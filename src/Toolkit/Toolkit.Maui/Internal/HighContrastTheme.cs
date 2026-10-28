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

using System.ComponentModel;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

/// <summary>
/// Tracks the platform's contrast setting and the app theme, and notifies when either changes.
/// </summary>
/// <remarks>
/// Elements bind to <see cref="Colors"/> through <see cref="HighContrastThemeExtensions.SetHighContrastColor(BindableObject, BindableProperty, Color?, Color?, HighContrastColorRole)"/>.
/// Binding sources are held weakly by MAUI, so elements do not need to unsubscribe, and a value set directly on the element still replaces the binding.
/// </remarks>
internal sealed partial class HighContrastTheme : INotifyPropertyChanged
{
    private static HighContrastTheme? _instance;

    public static HighContrastTheme Instance => _instance ??= new HighContrastTheme();

    private HighContrastTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += (s, e) => Refresh();
        }

        ConnectPlatformSettings();
        Colors = GetColors();
    }

    /// <summary>
    /// Gets the colors to use while a contrast setting is on, or <c>null</c> when it is off.
    /// </summary>
    public HighContrastColors? Colors { get; private set; }

    public bool IsEnabled => Colors != null;

    public event PropertyChangedEventHandler? PropertyChanged;

    partial void ConnectPlatformSettings();

    partial void GetPlatformColors(ref HighContrastColors? colors);

    private HighContrastColors? GetColors()
    {
        HighContrastColors? colors = null;
        GetPlatformColors(ref colors);
        return colors;
    }

    /// <summary>
    /// Re-reads the platform setting and notifies bindings. Raised even when the colors are unchanged,
    /// since the light and dark fallback colors also depend on the app theme.
    /// </summary>
    internal void Refresh()
    {
        if (!Microsoft.Maui.ApplicationModel.MainThread.IsMainThread)
        {
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(Refresh);
            return;
        }

        Colors = GetColors();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Colors)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
    }
}
