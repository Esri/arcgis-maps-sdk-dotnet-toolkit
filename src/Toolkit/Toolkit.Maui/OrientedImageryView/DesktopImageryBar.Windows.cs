#if WINDOWS
using System.Globalization;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
using NativeGrid = Microsoft.UI.Xaml.Controls.Grid;
using NativeThickness = Microsoft.UI.Xaml.Thickness;
using NativeVisibility = Microsoft.UI.Xaml.Visibility;
using NativeView = Microsoft.UI.Xaml.FrameworkElement;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal sealed class DesktopImageryBar : Microsoft.Maui.Controls.View
{
    private OrientedImageryView? _owner;

    public OrientedImageryView? Owner
    {
        get => _owner;
        set
        {
            if (_owner == value) return;
            (Handler as DesktopImageryBarHandler)?.Reset();
            _owner = value;
            Refresh();
        }
    }

    public OrientedImageryToolbarItemBase? ToolbarItem { get; init; }

    public void Refresh() => (Handler as DesktopImageryBarHandler)?.Refresh();
}

internal sealed class DesktopImageryBarHandler : ViewHandler<DesktopImageryBar, NativeGrid>
{
    #region Handler state and lifecycle

    private OrientedImageryViewModel? _model;
    private OrientedImageryToolbarItemBase? _item;
    private readonly List<Action> _updates = [];
    private readonly List<Action> _cleanup = [];
    private bool _updating;
    private NavigationState? _navigationState;
    private FontFamily? _iconFont;
    private bool _rendering;
    private int _generation;
    private readonly Windows.UI.ViewManagement.UISettings _uiSettings = new();
    private readonly List<TextBlock> _captions = [];
    private readonly List<ToolbarComboBox> _toolbarSelectors = [];

    private readonly record struct NavigationState(bool Sequential, int Selected, int Start, int Count, int Total,
        bool Previous, bool Next, bool Searching);

    public DesktopImageryBarHandler()
        : base(new PropertyMapper<DesktopImageryBar, DesktopImageryBarHandler>(ViewMapper))
    {
    }

    protected override NativeGrid CreatePlatformView() => new() { Resources = CreateToolbarResources() };

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
        if (((IElementHandler)this).VirtualView is not DesktopImageryBar view) return;
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

    #region Native icons and captions

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
            Text = Text(key), FontSize = 12,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
        };
        label.Margin = new NativeThickness(0, 8, 0, 0);
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

    private static void Describe(NativeView control, string key, string? id = null)
    {
        string text = Text(key);
        AutomationProperties.SetName(control, text);
        AutomationProperties.SetAutomationId(control, id ?? "OrientedImageryDesktop" + key);
        ToolTipService.SetToolTip(control, text);
    }

    #endregion

    #region Refresh and cleanup

    public void Refresh()
    {
        if (((IElementHandler)this).VirtualView is not DesktopImageryBar view) return;
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
                RefreshToolbar(VirtualView.Owner, item);
            else
                RefreshNavigation(VirtualView.Owner);
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
        _navigationState = null;
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

    #region Toolbar controls

    private void RefreshToolbar(OrientedImageryView owner, OrientedImageryToolbarItemBase item)
    {
        var model = owner.ViewModel;
        if (_model != model || _item != item)
        {
            Clear();
            _model = model;
            _item = item;
            NativeView? control = item switch
            {
                ImageSearchModeVM mode => ModePicker(owner, mode),
                ToolbarDividerVM => Divider(),
                ShowSelectedFootprintVM => Toggle(owner, "ShowSelectedImageFootprint", "CurrentFootprintLabel",
                    Geometry(OrientedImageryView.CurrentFootprintGeometry),
                    () => model.ShowSelectedFootprint, value => model.ShowSelectedFootprint = value,
                    automationId: "OrientedImageryDesktopCurrentFootprint"),
                ShowUnselectedFootprintsVM => Toggle(owner, "ShowUnselectedImageFootprints", "AdditionalFootprintsLabel",
                    Geometry(OrientedImageryView.OverlayGeometry),
                    () => model.ShowUnselectedFootprints, value => model.ShowUnselectedFootprints = value,
                    () => model.Images.Count > 1, "OrientedImageryDesktopAdditionalFootprints"),
                ShowCameraMarkersVM => Toggle(owner, "ShowCameraMarkersOnGeoView", "CameraLocationsLabel", CameraLocationsIcon(),
                    () => model.ShowCameraLocations, value => model.ShowCameraLocations = value,
                    () => model.Images.Count > 1, "OrientedImageryDesktopCameraLocations"),
                AllowAddingMarkersVM => Toggle(owner, "AddMarkersAutomationName", "AddMarkerLabel", Glyph("\uE8ED"),
                    () => owner.IsAddingMarkers, owner.SetAddingMarkers,
                    automationId: "OrientedImageryDesktopAddMarker"),
                AutoUpdateFootprintVM => Toggle(owner, "AutoUpdate", "AutoUpdate", Glyph(ToolkitIcons.Refresh),
                    () => model.AutoUpdateFootprint, value => model.AutoUpdateFootprint = value),
                SelectNewMarkerSymbolVM picker => SymbolPicker(owner, picker),
                ClearMarkersVM => ActionButton(owner, "ClearMarkers", "ClearMarkersLabel", Glyph("\uEB04"),
                    () => model.ClearMarkersCommand.Execute(null), () => model.ClearMarkersCommand.CanExecute(null)),
                SequentialNavigationVM => SequenceButton(owner),
                _ => null,
            };
            if (control != null)
            {
                control.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                control.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                PlatformView.Children.Add(control);
                ((IView)VirtualView).InvalidateMeasure();
            }
        }
        // Setting native selections raises events; do not treat these updates as user input.
        _updating = true;
        try
        {
            foreach (var update in _updates)
                update();
        }
        finally
        {
            _updating = false;
        }
    }

    private NativeButton ActionButton(OrientedImageryView owner, string key, string labelKey, UIElement icon, Action execute, Func<bool> enabled)
    {
        var button = new NativeButton
        {
            Content = LabeledContent(icon, labelKey), MinWidth = 0, MinHeight = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(4), Margin = new NativeThickness(3, 0, 3, 0),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        };
        Describe(button, key);
        button.Click += (_, _) => { if (!_updating && enabled()) execute(); };
        _updates.Add(() => button.IsEnabled = owner.DesktopHasLayer && enabled());
        return button;
    }

    private ToggleButton Toggle(OrientedImageryView owner, string key, string labelKey, UIElement icon, Func<bool> get, Action<bool> set, Func<bool>? enabled = null, string? automationId = null)
    {
        var toggle = new ToggleButton
        {
            Content = LabeledContent(icon, labelKey),
            MinWidth = 0, MinHeight = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(4), Margin = new NativeThickness(3, 0, 3, 0),
        };
        Describe(toggle, key, automationId);
        toggle.Click += (_, _) => { if (!_updating) set(toggle.IsChecked == true); };
        _updates.Add(() =>
        {
            bool selected = get();
            toggle.IsChecked = selected;
            toggle.IsEnabled = owner.DesktopHasLayer && (enabled?.Invoke() ?? true);
        });
        return toggle;
    }

    #endregion

    #region Mode and symbol selectors

    private sealed class ToolbarComboBox : ComboBox
    {
        private readonly bool _showSymbol;
        private double _captionHeight = 32;

        public ToolbarComboBox(bool showSymbol)
        {
            _showSymbol = showSymbol;
            MinWidth = 0;
            MinHeight = 84;
            Padding = new NativeThickness(0);
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            CornerRadius = new CornerRadius(0);
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            UseSystemFocusVisuals = true;
        }

        public void UpdateCaptionHeight(double textScale)
        {
            _captionHeight = 32 * textScale;
            MinHeight = Math.Max(84, 48 + _captionHeight);
            if (GetTemplateChild("ContentPresenter") is NativeView content)
                content.Margin = new NativeThickness(0, 0, 0, 8 + _captionHeight);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Rearrange native parts rather than copying WinUI's keyboard, focus and state template.
            if (GetTemplateChild("LayoutRoot") is NativeGrid root && root.ColumnDefinitions.Count > 1)
                root.ColumnDefinitions[1].Width = new Microsoft.UI.Xaml.GridLength(0);
            if (GetTemplateChild("Background") is Microsoft.UI.Xaml.Controls.Border background)
                background.MinWidth = 0;
            if (GetTemplateChild("ContentPresenter") is Microsoft.UI.Xaml.Controls.ContentPresenter content)
            {
                content.Visibility = _showSymbol ? NativeVisibility.Visible : NativeVisibility.Collapsed;
                content.Width = content.Height = 32;
                content.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                content.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                content.Margin = new NativeThickness(0, 0, 0, 8 + _captionHeight);
                content.RenderTransform = new ScaleTransform { ScaleX = 0.5, ScaleY = 0.5, CenterX = 16, CenterY = 16 };
            }
            if (GetTemplateChild("DropDownGlyph") is NativeView chevron)
            {
                NativeGrid.SetColumn(chevron, 0);
                chevron.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                chevron.Margin = new NativeThickness(0, 40, 0, 0);
            }
            if (GetTemplateChild("Pill") is NativeView pill)
                pill.Visibility = NativeVisibility.Collapsed;
            if (GetTemplateChild("PopupBorder") is Microsoft.UI.Xaml.Controls.Border border)
            {
                border.MinWidth = 40;
                border.CornerRadius = new CornerRadius(0);
            }
            if (GetTemplateChild("Popup") is Microsoft.UI.Xaml.Controls.Primitives.Popup popup)
            {
                popup.PlacementTarget = this;
                popup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft;
            }
        }
    }

    private ToolbarComboBox CreateToolbarSelector(bool showSymbol)
    {
        var selector = new ToolbarComboBox(showSymbol);
        selector.UpdateCaptionHeight(_uiSettings.TextScaleFactor);
        _toolbarSelectors.Add(selector);
        return selector;
    }

    private NativeGrid ModePicker(OrientedImageryView owner, ImageSearchModeVM mode)
    {
        var host = new NativeGrid
        {
            MinHeight = 84,
            ColumnDefinitions =
            {
                new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) },
                new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(24) },
            },
        };
        var button = new ToggleButton
        {
            MinWidth = 0, MinHeight = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(1), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(4), Margin = new NativeThickness(3, 0, 3, 0),
        };
        button.Click += (_, _) =>
        {
            if (!_updating && button.IsEnabled)
                mode.IsImageSelectionEnabled = button.IsChecked == true;
        };
        var dropdown = CreateToolbarSelector(false);
        bool selectionChangedWhileOpen = false;
        dropdown.DropDownOpened += (_, _) => selectionChangedWhileOpen = false;
        NativeGrid.SetColumn(dropdown, 1);
        host.Children.Add(button);
        host.Children.Add(dropdown);
        _cleanup.Add(() => dropdown.IsDropDownOpen = false);
        Describe(button, "ImageSearchMode", "OrientedImageryDesktopMode");
        Describe(dropdown, "ImageSearchMode", "OrientedImageryDesktopModeDropdown");
        foreach (var state in mode.ImageSearchModes)
        {
            bool search = state == OrientedImageryViewInteractionState.FindImages;
            string key = search ? "ImageSearchModeFind" : "ImageSearchModeDisplay";
            var content = new StackPanel { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal, Spacing = 8 };
            content.Children.Add(new Viewbox { Width = 24, Height = 24, Child = ImageModeIcon(search) });
            content.Children.Add(new TextBlock { Text = Text(key), VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center });
            var item = new ComboBoxItem
            {
                Content = content, Tag = state, MinHeight = 40, Padding = new NativeThickness(8, 4, 8, 4),
            };
            AutomationProperties.SetName(item, Text(key));
            item.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, e) =>
            {
                if (!_updating && dropdown.IsEnabled && item.IsSelected &&
                    e.GetCurrentPoint(item).Properties.IsLeftButtonPressed)
                {
                    e.Handled = true;
                    dropdown.IsDropDownOpen = false;
                    OrientedImageryView.ToggleImageMode(mode, state);
                }
            }), true);
            dropdown.Items.Add(item);
        }
        dropdown.PreviewKeyDown += (_, e) =>
        {
            // Arrow navigation already activates its selection; Enter only confirms it.
            if (!_updating && dropdown.IsEnabled && dropdown.IsDropDownOpen && !selectionChangedWhileOpen &&
                e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space &&
                FocusManager.GetFocusedElement(dropdown.XamlRoot) is ComboBoxItem
                {
                    IsSelected: true, Tag: OrientedImageryViewInteractionState state,
                })
            {
                e.Handled = true;
                dropdown.IsDropDownOpen = false;
                OrientedImageryView.ToggleImageMode(mode, state);
            }
        };
        dropdown.SelectionChanged += (_, _) =>
        {
            if (!_updating && dropdown.IsEnabled &&
                dropdown.SelectedItem is ComboBoxItem { Tag: OrientedImageryViewInteractionState state })
            {
                selectionChangedWhileOpen = dropdown.IsDropDownOpen;
                OrientedImageryView.ToggleImageMode(mode, state);
            }
        };
        bool? previous = null;
        _updates.Add(() =>
        {
            bool search = mode.SelectedInteractionState == OrientedImageryViewInteractionState.FindImages;
            if (previous != search)
            {
                if (button.Content is StackPanel previousContent)
                    foreach (var caption in previousContent.Children.OfType<TextBlock>())
                        _captions.Remove(caption);
                button.Content = LabeledContent(ImageModeIcon(search),
                    search ? "ImageSearchModeFindToolbarLabel" : "ImageSearchModeDisplayToolbarLabel");
                previous = search;
                ((IView)VirtualView).InvalidateMeasure();
            }
            dropdown.SelectedItem = mode.IsImageSelectionEnabled ? dropdown.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is OrientedImageryViewInteractionState state && state == mode.SelectedInteractionState) : null;
            button.IsEnabled = owner.DesktopHasLayer;
            dropdown.IsEnabled = button.IsEnabled;
            button.IsChecked = mode.IsImageSelectionEnabled;
        });
        System.ComponentModel.PropertyChangedEventHandler changed = (_, e) =>
        {
            if (e.PropertyName is null or "" or nameof(ImageSearchModeVM.SelectedInteractionState) or nameof(ImageSearchModeVM.IsImageSelectionEnabled))
                Refresh();
        };
        mode.PropertyChanged += changed;
        _cleanup.Add(() => mode.PropertyChanged -= changed);
        return host;
    }

    private ToggleButton SequenceButton(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        return Toggle(owner, "SequentialMode", "SequentialMode", Geometry(OrientedImageryView.SequenceGeometry),
            () => model.IsSequentialNavigationEnabled, value => model.IsSequentialNavigationEnabled = value,
            () => model.ToggleSequentialNavigationCommand.CanExecute(null), "OrientedImageryDesktopSequentialMode");
    }

    private ComboBox SymbolPicker(OrientedImageryView owner, SelectNewMarkerSymbolVM picker)
    {
        var combo = CreateToolbarSelector(true);
        Describe(combo, "SelectMarkerSymbol", "OrientedImageryDesktopSelectMarkers");
        _cleanup.Add(() => combo.IsDropDownOpen = false);
        MarkerSymbol[] symbols = [];
        combo.SelectionChanged += (_, _) =>
        {
            if (!_updating && combo.IsEnabled && combo.SelectedIndex >= 0 && combo.SelectedIndex < symbols.Length)
                picker.SelectedSymbol = symbols[combo.SelectedIndex];
        };
        void UpdateSymbols()
        {
            if (symbols.SequenceEqual(picker.SymbolOptions)) return;
            symbols = picker.SymbolOptions.ToArray();
            _generation++;
            combo.Items.Clear();
            foreach (var symbol in symbols)
            {
                var image = new Microsoft.UI.Xaml.Controls.Image { Width = 32, Height = 32 };
                string name = OrientedImageryView.MarkerOptionName(symbol, combo.Items.Count);
                var item = new ComboBoxItem
                {
                    Content = image, Padding = new NativeThickness(4),
                    MinWidth = 40, MinHeight = 40,
                    HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                };
                AutomationProperties.SetName(item, name);
                combo.Items.Add(item);
                _ = RenderSymbolAsync(owner, symbol, image, _generation);
            }
        }
        System.ComponentModel.PropertyChangedEventHandler changed = (_, _) => Refresh();
        System.Collections.Specialized.NotifyCollectionChangedEventHandler collectionChanged = (_, _) => Refresh();
        picker.PropertyChanged += changed;
        picker.SymbolOptions.CollectionChanged += collectionChanged;
        _cleanup.Add(() =>
        {
            picker.PropertyChanged -= changed;
            picker.SymbolOptions.CollectionChanged -= collectionChanged;
        });
        _updates.Add(() =>
        {
            UpdateSymbols();
            combo.SelectedIndex = Array.IndexOf(symbols, picker.SelectedSymbol);
            combo.IsEnabled = owner.DesktopHasLayer;
        });
        return combo;
    }

    private async Task RenderSymbolAsync(OrientedImageryView owner, MarkerSymbol symbol, Microsoft.UI.Xaml.Controls.Image image, int generation)
    {
        try
        {
            var swatch = await symbol.CreateSwatchAsync(96);
            var source = await Esri.ArcGISRuntime.UI.RuntimeImageExtensions.ToImageSourceAsync(swatch);
            if (generation == _generation && VirtualView.Owner == owner)
            {
                image.Source = source;
                Refresh();
            }
        }
        catch (Exception ex)
        {
            if (generation == _generation && VirtualView.Owner == owner) owner.ReportDesktopError(ex);
        }
    }

    #endregion

    #region Image navigation

    private void RefreshNavigation(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        int selected = -1;
        for (int i = 0; i < model.Images.Count; i++)
            if (model.Images[i] == model.SelectedImage) selected = i;
        double width = VirtualView.Width > 0 ? VirtualView.Width : owner.Width;
        var (start, count) = OrientedImageryPagination.GetRange(model.Images.Count, selected, Math.Max(35, width - 66), 35);
        var state = new NavigationState(model.IsSequentialNavigationEnabled, selected, start, count, model.Images.Count,
            model.SelectPreviousImageCommand.CanExecute(null), model.SelectNextImageCommand.CanExecute(null), owner.DesktopIsSearching);
        if (_navigationState == state && _model == model) return;
        // Keep keyboard focus on the same action or image when the page range changes.
        var focused = PlatformView.XamlRoot == null ? null :
            FocusManager.GetFocusedElement(PlatformView.XamlRoot) as NativeButton;
        bool restoreFocus = focused != null &&
            PlatformView.Children.OfType<NavigationRow>().Any(panel => panel.Children.Contains(focused));
        string? focusedId = restoreFocus ? AutomationProperties.GetAutomationId(focused!) : null;
        var focusState = restoreFocus ? focused!.FocusState : FocusState.Unfocused;
        NavigationRow row;
        if (_model == model && _navigationState is { } previous &&
            previous.Sequential == state.Sequential && (state.Sequential || previous.Count == state.Count) &&
            PlatformView.Children.FirstOrDefault() is NavigationRow existingRow)
        {
            row = existingRow;
        }
        else
        {
            Clear();
            _model = model;
            row = CreateNavigationRow(owner, state);
            PlatformView.Children.Add(row);
        }
        _navigationState = state;
        UpdateNavigationRow(row, state);
        ((IView)VirtualView).InvalidateMeasure();
        if (restoreFocus)
            RestoreNavigationFocus(row, focusedId, selected, focusState);
    }

    private NavigationRow CreateNavigationRow(OrientedImageryView owner, NavigationState state)
    {
        var model = owner.ViewModel;
        var row = new NavigationRow();
        NativeButton NavigateButton(bool next, bool label)
        {
            var button = new NativeButton
            {
                Content = label ? Text(next ? "NextLabel" : "PreviousLabel") : new FontIcon
                {
                    Glyph = next ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronLeft,
                    FontFamily = IconFont, FontSize = 16,
                },
                MinHeight = 32, MinWidth = 0, Padding = new NativeThickness(label ? 4 : 0),
                BorderThickness = new NativeThickness(0), CornerRadius = new CornerRadius(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                AllowFocusWhenDisabled = true,
            };
            if (!label) button.Width = 33;
            string key = next ? "NextLabel" : "PreviousLabel";
            string id = next ? "OrientedImageryPaginatorNext" : "OrientedImageryPaginatorPrevious";
            Describe(button, key, label ? id + "Label" : id);
            button.Click += (_, _) =>
            {
                var command = next ? model.SelectNextImageCommand : model.SelectPreviousImageCommand;
                if (!owner.DesktopIsSearching && command.CanExecute(null)) command.Execute(null);
            };
            return button;
        }
        row.Children.Add(NavigateButton(false, false));
        if (state.Sequential)
        {
            row.Children.Add(NavigateButton(false, true));
            row.Children.Add(PageIndicator(".", true));
        }
        else
        {
            for (int slot = 0; slot < state.Count; slot++)
            {
                int pageSlot = slot;
                var button = new NativeButton
                {
                    Content = PageIndicator("", false),
                    MinWidth = 35, MinHeight = 32, Padding = new NativeThickness(0),
                    BorderThickness = new NativeThickness(0), CornerRadius = new CornerRadius(0),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };
                button.Click += (_, _) =>
                {
                    int index = (_navigationState?.Start ?? 0) + pageSlot;
                    if (VirtualView.Owner?.ViewModel == model && !model.IsSequentialNavigationEnabled && index < model.Images.Count)
                        model.SelectedImage = model.Images[index];
                };
                button.PreviewKeyDown += (_, e) =>
                {
                    if (e.Key is not (Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Right))
                        return;
                    if (VirtualView.Owner == owner && owner.ViewModel == model &&
                        !owner.DesktopIsSearching && !model.IsSequentialNavigationEnabled)
                    {
                        var command = e.Key == Windows.System.VirtualKey.Right
                            ? model.SelectNextImageCommand : model.SelectPreviousImageCommand;
                        if (command.CanExecute(null)) command.Execute(null);
                    }
                    e.Handled = true;
                };
                row.Children.Add(button);
            }
        }
        if (state.Sequential) row.Children.Add(NavigateButton(true, true));
        row.Children.Add(NavigateButton(true, false));
        return row;
    }

    private static void RestoreNavigationFocus(NavigationRow row, string? focusedId, int selected, FocusState focusState)
    {
        // Numbered navigation follows selection, including when the visible range shifts.
        if (focusedId?.StartsWith("OrientedImageryPage", StringComparison.Ordinal) == true)
            focusedId = "OrientedImageryPage" + selected.ToString(CultureInfo.InvariantCulture);
        var target = row.Children.OfType<NativeButton>().FirstOrDefault(button => AutomationProperties.GetAutomationId(button) == focusedId);
        target ??= row.Children.OfType<NativeButton>().FirstOrDefault(button => AutomationProperties.GetAutomationId(button) == "OrientedImageryPage" + selected.ToString(CultureInfo.InvariantCulture));
        target?.Focus(focusState);
    }

    private static void UpdateNavigationRow(NavigationRow row, NavigationState state)
    {
        var previous = (NativeButton)row.Children[0];
        previous.IsEnabled = !state.Searching && state.Previous;
        var next = (NativeButton)row.Children[row.Children.Count - 1];
        next.IsEnabled = !state.Searching && state.Next;
        if (state.Sequential)
        {
            ((NativeButton)row.Children[1]).IsEnabled = previous.IsEnabled;
            ((NativeButton)row.Children[3]).IsEnabled = next.IsEnabled;
            return;
        }
        for (int slot = 0; slot < state.Count; slot++)
        {
            int index = state.Start + slot;
            var button = (NativeButton)row.Children[slot + 1];
            var indicator = (NativeGrid)button.Content;
            ((TextBlock)indicator.Children[0]).Text = (index + 1).ToString(CultureInfo.CurrentCulture);
            ((Microsoft.UI.Xaml.Controls.Border)indicator.Children[1]).Visibility =
                index == state.Selected ? NativeVisibility.Visible : NativeVisibility.Collapsed;
            button.IsEnabled = !state.Searching;
            button.IsTabStop = slot == Math.Clamp(state.Selected - state.Start, 0, state.Count - 1);
            AutomationProperties.SetName(button, string.Format(CultureInfo.CurrentCulture,
                Text(index == state.Selected ? "SelectedPageFormat" : "GoToPageFormat"), index + 1, state.Total));
            AutomationProperties.SetAutomationId(button, "OrientedImageryPage" + index.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static NativeGrid PageIndicator(string text, bool selected)
    {
        var grid = new NativeGrid { MinWidth = 35, MinHeight = 32 };
        grid.Children.Add(new TextBlock
        {
            Text = text, FontSize = 14, Margin = new NativeThickness(4, 2, 4, 4),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
        });
        var underline = ThemeBorder("SystemControlHighlightAccentBrush");
        underline.Height = 2;
        underline.Margin = new NativeThickness(1, 0, 1, 0);
        underline.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom;
        underline.Visibility = selected ? NativeVisibility.Visible : NativeVisibility.Collapsed;
        grid.Children.Add(underline);
        return grid;
    }

    #endregion

    #region Navigation row layout

    private sealed class NavigationRow : Panel
    {
        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            double width = 0, height = 0, rowWidth = 0, rowHeight = 0;
            foreach (var child in Children)
            {
                child.Measure(new Windows.Foundation.Size(availableSize.Width, double.PositiveInfinity));
                var size = child.DesiredSize;
                if (rowWidth > 0 && rowWidth + size.Width > availableSize.Width)
                {
                    width = Math.Max(width, rowWidth);
                    height += rowHeight;
                    rowWidth = rowHeight = 0;
                }
                rowWidth += size.Width;
                rowHeight = Math.Max(rowHeight, size.Height);
            }
            return new Windows.Foundation.Size(Math.Max(width, rowWidth), height + rowHeight);
        }

        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            double y = 0, width = 0, height = 0;
            var row = new List<UIElement>();
            void ArrangeRow()
            {
                double x = Math.Max(0, (finalSize.Width - width) / 2);
                foreach (var child in row)
                {
                    child.Arrange(new Windows.Foundation.Rect(x, y, child.DesiredSize.Width, height));
                    x += child.DesiredSize.Width;
                }
                y += height;
                row.Clear();
                width = height = 0;
            }
            foreach (var child in Children)
            {
                if (width > 0 && width + child.DesiredSize.Width > finalSize.Width)
                    ArrangeRow();
                row.Add(child);
                width += child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }
            ArrangeRow();
            return finalSize;
        }
    }

    #endregion
}
#endif
