using Xamarin.Forms;
#if __ANDROID__
using NativePoint = Android.Graphics.PointF;
using NativeColor = Android.Graphics.Color;
#elif __IOS__
using NativePoint = CoreGraphics.CGPoint;
#elif NETFX_CORE
using NativePoint = Windows.Foundation.Point;
#endif
#if NETFX_CORE
using NativeColor = Windows.UI.Color;
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
#endif
        }

        internal static Color ToXamarinFormsColor(this NativeColor color)
        {
            return Color.FromRgba(color.R, color.G, color.B, color.A);
        }
    }
}
