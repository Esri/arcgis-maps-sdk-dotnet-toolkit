﻿using Xamarin.Forms;
#if __ANDROID__
using NativeColor = Android.Graphics.Color;
#elif __IOS__
using NativeColor = UIKit.UIColor;
#elif NETFX_CORE
using NativeColor = Windows.UI.Color;
using Windows.UI.Xaml.Media;
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
#endif
        }

        internal static void SetForeground(this UI.Controls.ScaleLine scaleline, NativeColor color)
        {
#if NETFX_CORE
            scaleline.Foreground = new SolidColorBrush(color);
#else
            scaleline.ForegroundColor = color;
#endif
        }
    }
}
