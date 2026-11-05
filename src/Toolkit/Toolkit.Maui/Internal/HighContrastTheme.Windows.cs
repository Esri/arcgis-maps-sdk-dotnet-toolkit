#if WINDOWS
using Windows.UI.ViewManagement;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed partial class HighContrastTheme
{
    private AccessibilitySettings? _accessibilitySettings;
    private UISettings? _uiSettings;

    partial void ConnectPlatformSettings()
    {
        _accessibilitySettings = new AccessibilitySettings();
        // ColorValuesChanged is raised (on a background thread) when high contrast is toggled or its theme changes.
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += (s, e) => Refresh();
    }

    partial void GetPlatformColors(ref HighContrastColors? colors)
    {
        if (_accessibilitySettings?.HighContrast != true || _uiSettings == null)
        {
            return;
        }

        colors = new HighContrastColors(
            GetColor(UIElementType.WindowText),
            GetColor(UIElementType.Window),
            GetColor(UIElementType.Highlight),
            GetColor(UIElementType.HighlightText),
            GetColor(UIElementType.GrayText));
    }

    private Color GetColor(UIElementType type)
    {
        var color = _uiSettings!.UIElementColor(type);
        return Color.FromRgba(color.R, color.G, color.B, color.A);
    }
}
#endif
