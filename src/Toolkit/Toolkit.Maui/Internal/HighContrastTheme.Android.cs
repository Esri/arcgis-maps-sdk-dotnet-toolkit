#if ANDROID
using Android.Content;
using Android.Views.Accessibility;
using UiModeManager = Android.App.UiModeManager;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed partial class HighContrastTheme
{
    private UiModeManager? _uiModeManager;
    private AccessibilityManager? _accessibilityManager;

    partial void ConnectPlatformSettings()
    {
        var context = Android.App.Application.Context;
        if (context.MainExecutor is not { } executor)
        {
            return;
        }

        // The "Contrast" setting (medium / high) in Settings > Accessibility > Color and motion.
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            _uiModeManager = context.GetSystemService(Context.UiModeService) as UiModeManager;
            _uiModeManager?.AddContrastChangeListener(executor, new ContrastChangeListener(this));
        }

        // "High contrast text" in Settings > Accessibility > Display size and text. Readable on earlier versions only through hidden APIs.
        if (OperatingSystem.IsAndroidVersionAtLeast(36))
        {
            _accessibilityManager = context.GetSystemService(Context.AccessibilityService) as AccessibilityManager;
            _accessibilityManager?.AddHighContrastTextStateChangeListener(executor, new HighContrastTextStateChangeListener(this));
        }
    }

    partial void GetPlatformColors(ref HighContrastColors? colors)
    {
        var contrastIncreased = OperatingSystem.IsAndroidVersionAtLeast(34) && _uiModeManager?.Contrast > 0;
        var highContrastText = OperatingSystem.IsAndroidVersionAtLeast(36) && _accessibilityManager?.IsHighContrastTextEnabled == true;
        if (contrastIncreased || highContrastText)
        {
            colors = HighContrastColors.FromAppTheme();
        }
    }

    private sealed class ContrastChangeListener(HighContrastTheme theme) : Java.Lang.Object, UiModeManager.IContrastChangeListener
    {
        public void OnContrastChanged(float contrast) => theme.Refresh();
    }

    private sealed class HighContrastTextStateChangeListener(HighContrastTheme theme) : Java.Lang.Object, AccessibilityManager.IHighContrastTextStateChangeListener
    {
        public void OnHighContrastTextStateChanged(bool enabled) => theme.Refresh();
    }
}
#endif
