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

using System.Reflection;
using Xamarin.Forms;
#if __ANDROID__
using NativeColor = Android.Graphics.Color;
#elif __IOS__
using NativeColor = UIKit.UIColor;
#elif NETFX_CORE
using Windows.UI.Xaml.Media;
using NativeColor = Windows.UI.Color;
#elif NETSTANDARD2_0
using NativeColor = System.Drawing.Color;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal
{
    internal static class ExtensionMethods
    {
        internal static NativeColor ToNativeColor(this Color color)
        {
            var a = (byte)(255 * color.A);
            var r = (byte)(255 * color.R);
            var g = (byte)(255 * color.G);
            var b = (byte)(255 * color.B);
#if NETFX_CORE
            return NativeColor.FromArgb(a, r, g, b);
#elif __ANDROID__
            return NativeColor.Argb(a, r, g, b);
#elif __IOS__
            return NativeColor.FromRGBA(r, g, b, a);
#elif NETSTANDARD2_0
            return NativeColor.FromArgb(a, r, g, b);
#endif
        }

#if !NETSTANDARD2_0
        internal static void SetForeground(this UI.Controls.ScaleLine scaleline, NativeColor color)
        {
#if NETFX_CORE
            scaleline.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(color);
#else
            scaleline.ForegroundColor = color;
#endif
        }

        internal static void SetForeground(this UI.Controls.PopupViewer popupViewer, NativeColor color)
        {
#if NETFX_CORE
            popupViewer.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(color);
#else
            popupViewer.ForegroundColor = color;
#endif
        }

        internal static Esri.ArcGISRuntime.UI.Controls.GeoView? GetNativeGeoView(this Esri.ArcGISRuntime.Xamarin.Forms.GeoView geoView)
        {
            var property = typeof(Esri.ArcGISRuntime.Xamarin.Forms.GeoView).GetProperty("NativeGeoView", BindingFlags.Instance | BindingFlags.NonPublic);
            return property?.GetValue(geoView) as Esri.ArcGISRuntime.UI.Controls.GeoView;
        }
#endif
    }
}
