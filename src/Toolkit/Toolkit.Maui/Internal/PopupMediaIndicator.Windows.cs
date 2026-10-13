// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *   http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if WINDOWS
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal static class PopupMediaIndicator
{
    internal static void Configure(IndicatorView indicator)
    {
        indicator.HandlerChanged += (_, _) =>
        {
            if (indicator.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement platformView)
                platformView.IsTabStop = false;
        };
        indicator.IndicatorTemplate ??= new DataTemplate(() => CreateButton(indicator));
    }

    private static Button CreateButton(IndicatorView indicator)
    {
        var button = new Button
        {
            Padding = 0,
            BorderWidth = 0,
            MinimumWidthRequest = 0,
            MinimumHeightRequest = 0,
            Margin = new Thickness(4),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        void UpdateButton(object? sender, EventArgs e)
        {
            int index = (button.Parent as Layout)?.Children.IndexOf(button) ?? -1;
            if (index < 0)
                return;

            double size = indicator.IndicatorSize > 0 ? indicator.IndicatorSize : 10;
            button.WidthRequest = button.HeightRequest = size;
            button.CornerRadius = indicator.IndicatorsShape == IndicatorShape.Circle ? (int)Math.Ceiling(size / 2) : 0;
            button.IsVisible = index < indicator.Count && index < indicator.MaximumVisible && (indicator.Count > 1 || !indicator.HideSingle);
            button.IsEnabled = indicator.Count > 1;
            string key = index == indicator.Position ? "PopupViewerSelectedMediaPage" : "PopupViewerMediaPage";
            SemanticProperties.SetDescription(button, string.Format(CultureInfo.CurrentCulture, Properties.Resources.GetString(key)!, index + 1, indicator.Count));
        }

        button.Clicked += (_, _) =>
        {
            int index = (button.Parent as Layout)?.Children.IndexOf(button) ?? -1;
            if (index >= 0 && index < indicator.Count)
                indicator.Position = index;
        };
        button.BindingContextChanged += UpdateButton;
        button.Loaded += (_, _) =>
        {
            indicator.PropertyChanged -= UpdateButton;
            indicator.PropertyChanged += UpdateButton;
            UpdateButton(button, EventArgs.Empty);
        };
        button.Unloaded += (_, _) => indicator.PropertyChanged -= UpdateButton;

        return button;
    }
}
#endif
