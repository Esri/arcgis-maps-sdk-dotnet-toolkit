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
#if NETFX_CORE
            return NativeColor.FromArgb((byte)color.A, (byte)color.R, (byte)color.G, (byte)color.B);
#elif __ANDROID__
            return NativeColor.Argb((int)color.A, (int)color.R, (int)color.G, (int)color.B);
#endif
        }

        internal static Color ToXamarinFormsColor(this NativeColor color)
        {
            return Color.FromRgba(color.R, color.G, color.B, color.A);
        }
    }
}
