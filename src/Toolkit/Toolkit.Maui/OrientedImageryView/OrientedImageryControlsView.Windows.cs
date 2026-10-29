#if WINDOWS
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using NativeGrid = Microsoft.UI.Xaml.Controls.Grid;
using NativeThickness = Microsoft.UI.Xaml.Thickness;
using NativeView = Microsoft.UI.Xaml.FrameworkElement;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

// Hosts one toolbar item, or image pagination when ToolbarItem is unset.
internal sealed class OrientedImageryControlsView : Microsoft.Maui.Controls.View
{
    private OrientedImageryView? _owner;

    public OrientedImageryView? Owner
    {
        get => _owner;
        set
        {
            if (_owner == value) return;
            (Handler as OrientedImageryControlsViewHandler)?.Reset();
            _owner = value;
            Refresh();
        }
    }

    public OrientedImageryToolbarItemBase? ToolbarItem { get; init; }
    internal OrientedImageryToolbarItemView? Appearance { get; init; }

    public void Refresh() => (Handler as OrientedImageryControlsViewHandler)?.Refresh();
}

internal sealed partial class OrientedImageryControlsViewHandler : ViewHandler<OrientedImageryControlsView, NativeGrid>
{
    #region Handler state and lifecycle

    private OrientedImageryViewModel? _model;
    private readonly List<Action> _updates = [];
    private readonly List<Action> _cleanup = [];
    private FontFamily? _iconFont;
    private bool _rendering;
    private int _generation;
    private readonly Windows.UI.ViewManagement.UISettings _uiSettings = new();

    public OrientedImageryControlsViewHandler()
        : base(new PropertyMapper<OrientedImageryControlsView, OrientedImageryControlsViewHandler>(ViewMapper))
    {
    }

    protected override NativeGrid CreatePlatformView() => new() { Resources = CreateToolbarResources() };

    protected override void ConnectHandler(NativeGrid platformView)
    {
        base.ConnectHandler(platformView);
        platformView.SizeChanged += SizeChanged;
        VirtualView.SizeChanged += ViewSizeChanged;
        _uiSettings.TextScaleFactorChanged += TextScaleFactorChanged;
        Refresh();
    }

    protected override void DisconnectHandler(NativeGrid platformView)
    {
        platformView.SizeChanged -= SizeChanged;
        VirtualView.SizeChanged -= ViewSizeChanged;
        _uiSettings.TextScaleFactorChanged -= TextScaleFactorChanged;
        Clear(platformView);
        _iconFont = null;
        base.DisconnectHandler(platformView);
    }

    private void SizeChanged(object sender, SizeChangedEventArgs e) => Refresh();
    private void ViewSizeChanged(object? sender, EventArgs e) => Refresh();

    private void TextScaleFactorChanged(Windows.UI.ViewManagement.UISettings sender, object args)
    {
        if (((IElementHandler)this).VirtualView is not OrientedImageryControlsView view) return;
        view.Dispatcher.Dispatch(() =>
        {
            if (view.Handler != this) return;
            foreach (var caption in _captions)
                SizeCaption(caption);
            foreach (var selector in _toolbarSelectors)
                selector.UpdateCaptionHeight(_uiSettings.TextScaleFactor);
            PlatformView.InvalidateMeasure();
            ((IView)view).InvalidateMeasure();
        });
    }

    internal void Reset() => Clear();

    #endregion

    #region Theme resources

    private static Microsoft.UI.Xaml.ResourceDictionary CreateToolbarResources()
    {
        // Override native state brushes, not templates, to retain focus and accessibility behavior.
        var xaml = new System.Text.StringBuilder(
            "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><ResourceDictionary.ThemeDictionaries>");
        foreach (bool highContrast in new[] { false, true })
        {
            xaml.Append("<ResourceDictionary x:Key=\"").Append(highContrast ? "HighContrast" : "Default").Append("\">");
            foreach (string control in new[] { "Button", "ToggleButton" })
            {
                foreach (string state in new[] { "PointerOver", "Pressed", "Checked", "CheckedPointerOver", "CheckedPressed", "CheckedDisabled" })
                {
                    if (control == "Button" && state.StartsWith("Checked", StringComparison.Ordinal))
                        continue;
                    bool disabled = state == "CheckedDisabled";
                    string background = highContrast
                        ? disabled ? "SystemColorButtonFaceColor" : "SystemColorHighlightColor"
                        : disabled ? "ControlFillColorDisabled" :
                            state == "Checked" ? "SystemBaseLowColor" : "SystemBaseMediumLowColor";
                    string foreground = highContrast
                        ? disabled ? "SystemColorGrayTextColor" : "SystemColorHighlightTextColor"
                        : disabled ? "TextFillColorDisabled" : "TextFillColorPrimary";
                    AddBrush(control + "Background" + state, background);
                    AddBrush(control + "Foreground" + state, foreground);
                    AddBrush(control + "BorderBrush" + state, "Transparent");
                }
                AddBrush(control + "BorderBrush", "Transparent");
                AddBrush(control + "BorderBrushDisabled", "Transparent");
            }
            AddBrush("ComboBoxBackgroundPointerOver", highContrast ? "SystemColorHighlightColor" : "SystemBaseMediumLowColor");
            AddBrush("ComboBoxForegroundPointerOver", highContrast ? "SystemColorHighlightTextColor" : "TextFillColorPrimary");
            AddBrush("ComboBoxBorderBrushPointerOver", "Transparent");
            xaml.Append("</ResourceDictionary>");
        }
        xaml.Append("</ResourceDictionary.ThemeDictionaries></ResourceDictionary>");
        return (Microsoft.UI.Xaml.ResourceDictionary)XamlReader.Load(xaml.ToString());

        // ControlFillColorTransparent is a red placeholder in WinUI's HighContrast dictionary.
        void AddBrush(string key, string color) =>
            xaml.Append("<SolidColorBrush x:Key=\"").Append(key).Append("\" Color=\"")
                .Append(color == "Transparent" ? color : "{ThemeResource " + color + "}").Append("\"/>");
    }

    #endregion

    #region Presentation helpers

    private static string Text(string key) => OrientedImageryView.DesktopText(key);

    private static Microsoft.UI.Xaml.Controls.Border ThemeBorder(string key) =>
        (Microsoft.UI.Xaml.Controls.Border)XamlReader.Load(
            "<Border xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Background=\"{ThemeResource " + key + "}\"/>");

    private StackPanel LabeledContent(UIElement icon, string key)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
        };
        panel.Children.Add(new Viewbox
        {
            Width = 32, Height = 32, Child = icon,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
        });
        var label = new TextBlock
        {
            Text = VirtualView.Appearance?.Text ?? Text(key), FontSize = VirtualView.Appearance?.FontSize ?? 12,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
        };
        label.Margin = new NativeThickness(0, VirtualView.Appearance?.Spacing ?? 8, 0, 0);
        SizeCaption(label);
        _captions.Add(label);
        panel.Children.Add(label);
        return panel;
    }

    private void SizeCaption(TextBlock caption)
    {
        // Prefer compact, two-line captions without fixing their height or limiting text scaling.
        caption.MaxWidth = 68 * _uiSettings.TextScaleFactor;
        caption.MinHeight = 32 * _uiSettings.TextScaleFactor;
    }

    private static NativeView Divider()
    {
        var border = ThemeBorder("ControlStrongStrokeColorDefaultBrush");
        border.Width = 1;
        border.Height = 72;
        border.Margin = new NativeThickness(8, 6, 8, 6);
        return border;
    }

    private FontFamily IconFont => _iconFont ??=
        MauiContext!.Services.GetRequiredService<Microsoft.Maui.IFontManager>().GetFontFamily(Microsoft.Maui.Font.OfSize(ToolkitIcons.FontFamilyName, 24));

    private FontIcon Glyph(string glyph) => new()
    {
        Glyph = glyph,
        FontFamily = IconFont,
        FontSize = 24,
        // Use the button's state foreground instead of forced high-contrast text colors.
        HighContrastAdjustment = ElementHighContrastAdjustment.None,
    };

    private static UIElement Geometry(string data)
    {
        var icon = (PathIcon)XamlReader.Load(
            "<PathIcon xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Data=\"" + data + "\"/>");
        return new Viewbox { Width = 30, Height = 30, Child = icon };
    }

    private static UIElement CameraLocationsIcon() => Geometry(
        "F0 M5 23 A4 4 0 1 0 13 23 A4 4 0 1 0 5 23 Z " +
        "M6 23 A3 3 0 1 0 12 23 A3 3 0 1 0 6 23 Z " +
        "M13 17 A4 4 0 1 0 21 17 A4 4 0 1 0 13 17 Z " +
        "M14 17 A3 3 0 1 0 20 17 A3 3 0 1 0 14 17 Z " +
        "M5 5 A4 4 0 1 0 13 5 A4 4 0 1 0 5 5 Z " +
        "M6 5 A3 3 0 1 0 12 5 A3 3 0 1 0 6 5 Z " +
        "M21 26 A4 4 0 1 0 29 26 A4 4 0 1 0 21 26 Z " +
        "M22 26 A3 3 0 1 0 28 26 A3 3 0 1 0 22 26 Z " +
        "M19 8 A4 4 0 1 0 27 8 A4 4 0 1 0 19 8 Z " +
        "M20 8 A3 3 0 1 0 26 8 A3 3 0 1 0 20 8 Z");

    private static IconElement ImageModeIcon(bool explore) =>
        (PathIcon)XamlReader.Load(
            "<PathIcon xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Data=\"" +
            (explore ? OrientedImageryView.ExploreImagesGeometry : OrientedImageryView.DisplayImageGeometry) + "\"/>");

    private void Describe(NativeView control, string key, string? id = null)
    {
        string text = VirtualView.Appearance?.AccessibilityText ?? VirtualView.Appearance?.Text ?? Text(key);
        AutomationProperties.SetName(control, text);
        AutomationProperties.SetAutomationId(control, id ?? "OrientedImageryDesktop" + key);
        ToolTipService.SetToolTip(control, text);
    }

    #endregion

    #region Refresh and cleanup

    public void Refresh()
    {
        if (((IElementHandler)this).VirtualView is not OrientedImageryControlsView view) return;
        if (view.Dispatcher.IsDispatchRequired)
        {
            view.Dispatcher.Dispatch(() =>
            {
                if (view.Handler == this) Refresh();
            });
            return;
        }
        if (_rendering || VirtualView.Owner == null) return;
        _rendering = true;
        try
        {
            if (VirtualView.ToolbarItem is { } item)
            {
                RefreshToolbar(VirtualView.Owner, item);
                foreach (var caption in _captions)
                {
                    if (VirtualView.Appearance?.TextColor is { } color && !VirtualView.Owner.DesktopHighContrast)
                        caption.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(
                            (byte)(color.Alpha * 255), (byte)(color.Red * 255), (byte)(color.Green * 255), (byte)(color.Blue * 255)));
                    else
                        caption.ClearValue(TextBlock.ForegroundProperty);
                }
            }
            else
                RefreshPagination(VirtualView.Owner);
        }
        finally
        {
            _rendering = false;
        }
    }

    private void Clear(NativeGrid? platformView = null)
    {
        // Discard pending swatches and detach subscriptions before rebuilding native content.
        _generation++;
        _model = null;
        _item = null;
        _paginationState = null;
        foreach (var cleanup in _cleanup) cleanup();
        _cleanup.Clear();
        _updates.Clear();
        _captions.Clear();
        _toolbarSelectors.Clear();
        platformView ??= ((IElementHandler)this).PlatformView as NativeGrid;
        if (platformView != null)
        {
            platformView.Children.Clear();
            platformView.ColumnDefinitions.Clear();
            platformView.RowDefinitions.Clear();
        }
    }

    #endregion

}
#endif
