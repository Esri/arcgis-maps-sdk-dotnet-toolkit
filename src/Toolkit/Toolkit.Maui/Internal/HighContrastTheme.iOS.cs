#if __IOS__ || MACCATALYST
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed partial class HighContrastTheme
{
    private NSObject? _darkerSystemColorsObserver;

    partial void ConnectPlatformSettings()
    {
        // "Increase Contrast" in Settings > Accessibility > Display & Text Size.
        _darkerSystemColorsObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIView.DarkerSystemColorsStatusDidChangeNotification, _ => Refresh());
    }

    partial void GetPlatformColors(ref HighContrastColors? colors)
    {
        if (UIAccessibility.DarkerSystemColorsEnabled)
        {
            colors = HighContrastColors.FromAppTheme();
        }
    }
}
#endif
