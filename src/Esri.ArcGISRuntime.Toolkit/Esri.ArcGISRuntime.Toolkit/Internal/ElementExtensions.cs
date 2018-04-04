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

#if !XAMARIN
using System;
using System.Collections.Generic;
using System.Text;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Helper class for providing common cross-platform names for UI component manipulation
    /// </summary>
    internal static class ElementExtensions
    {
        public static void SetMargin(this FrameworkElement element, double left, double top, double right, double bottom)
            => element.Margin = new Thickness(left, top, right, bottom);

        public static double GetActualWidth(this FrameworkElement element) => element.ActualWidth;

        public static void SetWidth(this FrameworkElement element, double width) => element.Width = width;

        public static void SetHeight(this FrameworkElement element, double height) => element.Height = height;

        public static void SetIsVisible(this UIElement element, bool isVisible) => element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

        public static bool GetIsVisible(this UIElement element) => element.Visibility == Visibility.Visible ? true : false;

        public static void SetOpacity(this UIElement element, double opacity) => element.Opacity = opacity;

        public static double GetOpacity(this UIElement element) => element.Opacity;

        public static bool GetIsEnabled(this Control control) => control.IsEnabled;

        public static void SetIsEnabled(this Control control, bool enabled) => control.IsEnabled = enabled;

        public static void RemoveChild(this Panel parent, UIElement child) => parent.Children.Remove(child);
    }
}
#endif
