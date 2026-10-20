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
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;

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

    public bool IsNavigation { get; init; }
    public OrientedImageryToolbarItemBase? ToolbarItem { get; init; }

    public void Refresh() => (Handler as DesktopImageryBarHandler)?.Refresh();
}

internal sealed class DesktopImageryBarHandler : ViewHandler<DesktopImageryBar, NativeGrid>
{
    private OrientedImageryViewModel? _model;
    private OrientedImageryToolbarItemBase[] _items = [];
    private readonly List<Action> _updates = [];
    private readonly List<NativeView> _tools = [];
    private readonly List<NativeView> _visibleTools = [];
    private readonly List<Action> _detachSymbols = [];
    private bool _updating;
    private NavigationState? _navigationState;
    private FontFamily? _iconFont;
    private double _toolbarWidth;
    private bool _rendering;
    private int _generation;

    private readonly record struct NavigationState(bool Sequential, int Selected, int Start, int Count, int Total,
        bool Previous, bool Next, bool Searching);

    public DesktopImageryBarHandler()
        : base(new PropertyMapper<DesktopImageryBar, DesktopImageryBarHandler>(ViewMapper))
    {
    }

    protected override NativeGrid CreatePlatformView() => new();

    protected override void ConnectHandler(NativeGrid platformView)
    {
        base.ConnectHandler(platformView);
        platformView.SizeChanged += SizeChanged;
        VirtualView.SizeChanged += ViewSizeChanged;
        Refresh();
    }

    protected override void DisconnectHandler(NativeGrid platformView)
    {
        platformView.SizeChanged -= SizeChanged;
        VirtualView.SizeChanged -= ViewSizeChanged;
        Clear(platformView);
        _iconFont = null;
        base.DisconnectHandler(platformView);
    }

    private void SizeChanged(object sender, SizeChangedEventArgs e) => Refresh();
    private void ViewSizeChanged(object? sender, EventArgs e) => Refresh();

    internal void Reset() => Clear();

    private static string Text(string key) => OrientedImageryView.DesktopText(key);

    private static Microsoft.UI.Xaml.Controls.Border ThemeBorder(string key) =>
        (Microsoft.UI.Xaml.Controls.Border)XamlReader.Load(
            "<Border xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Background=\"{ThemeResource " + key + "}\"/>");

    private static StackPanel LabeledContent(UIElement icon, string key, double labelWidth = 68)
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
            Text = Text(key), Width = labelWidth, Height = 32, FontSize = 12,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
        };
        label.Margin = new NativeThickness(0, 8, 0, 0);
        panel.Children.Add(label);
        return panel;
    }

    private static StackPanel DropdownContent(UIElement icon)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
        };
        panel.Children.Add(new NativeGrid { Width = 16, Height = 32, Children = { icon } });
        var caption = new NativeGrid { Height = 32, Margin = new NativeThickness(0, 8, 0, 0) };
        caption.Children.Add((Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load(
            "<Path xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "Width=\"12\" Height=\"8\" VerticalAlignment=\"Center\" " +
            "Stroke=\"{ThemeResource TextFillColorPrimaryBrush}\" StrokeThickness=\"2\" " +
            "StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" StrokeLineJoin=\"Round\" Data=\"M1 1L6 6L11 1\"/>"));
        panel.Children.Add(caption);
        return panel;
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

    private IconElement ImageModeIcon(bool explore) => explore
        ? (PathIcon)XamlReader.Load(
            "<PathIcon xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Data=\"F1 " +
            "M26.7358 19.6048L27.7447 18.5074L32.9375 12.8587L24.2416 11.0883L11.1977 8.43275L7.75234 12.1806L13.9879 33.4721L26.7358 19.6048Z " +
            "M9.1681 12.5122L11.647 9.81567L30.469 13.6722L14.5913 30.944L9.1681 12.5122Z " +
            "M6.59961 5.13663C7.92743 5.13684 8.99902 6.21202 8.99902 7.54385C8.99895 8.87562 7.92738 9.95087 6.59961 9.95108C5.27167 9.95107 4.1993 8.87575 4.19922 7.54385C4.19922 6.21189 5.27162 5.13664 6.59961 5.13663Z " +
            "M6.59961 6.74112C6.15162 6.74113 5.79883 7.09452 5.79883 7.54385C5.7989 7.99312 6.15167 8.34658 6.59961 8.34659C7.04738 8.34638 7.39934 7.99299 7.39941 7.54385C7.39941 7.09465 7.04742 6.74132 6.59961 6.74112Z " +
            "M21.6003 19.6003L20.2007 21L14.6003 15.3997L16 14L21.6003 19.6003Z " +
            "M21.6003 15.3997L16 21L14.6003 19.6003L20.2007 14L21.6003 15.3997Z\"/>")
        : new FontIcon { Glyph = "\uEA74", FontFamily = IconFont, FontSize = 32 };

    private static void Describe(NativeView control, string key, string? id = null)
    {
        string text = Text(key);
        AutomationProperties.SetName(control, text);
        AutomationProperties.SetAutomationId(control, id ?? "OrientedImageryDesktop" + key);
        ToolTipService.SetToolTip(control, text);
    }

    public void Refresh()
    {
        if (_rendering || VirtualView.Owner == null) return;
        _rendering = true;
        try
        {
            if (VirtualView.IsNavigation)
                RefreshNavigation(VirtualView.Owner);
            else
                RefreshToolbar(VirtualView.Owner);
        }
        finally
        {
            _rendering = false;
        }
    }

    private void Clear(NativeGrid? platformView = null)
    {
        _generation++;
        _model = null;
        _items = [];
        _navigationState = null;
        foreach (var detach in _detachSymbols) detach();
        _detachSymbols.Clear();
        _updates.Clear();
        _tools.Clear();
        _visibleTools.Clear();
        _toolbarWidth = 0;
        platformView ??= ((IElementHandler)this).PlatformView as NativeGrid;
        if (platformView != null)
        {
            platformView.Children.Clear();
            platformView.ColumnDefinitions.Clear();
            platformView.RowDefinitions.Clear();
        }
    }

    private void RefreshToolbar(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        var items = VirtualView.ToolbarItem is { } toolbarItem ? new[] { toolbarItem } : owner.ToolbarItems;
        if (_model != model || !_items.SequenceEqual(items))
        {
            Clear();
            _model = model;
            _items = items.ToArray();
            foreach (var item in _items)
            {
                NativeView? control = item switch
                {
                    ImageSearchModeVM mode => ModePicker(owner, mode),
                    ToolbarDividerVM => Divider(),
                    ShowSelectedFootprintVM => Toggle(owner, "CurrentFootprint", "CurrentFootprintLabel", Geometry(CurrentFootprint), () => model.ShowSelectedFootprint, value => model.ShowSelectedFootprint = value),
                    ShowUnselectedFootprintsVM => Toggle(owner, "AdditionalFootprints", "AdditionalFootprintsLabel", Geometry(AdditionalFootprints), () => model.ShowUnselectedFootprints, value => model.ShowUnselectedFootprints = value, () => model.Images.Count > 1),
                    ShowCameraMarkersVM => Toggle(owner, "CameraLocations", "CameraLocationsLabel", CameraLocationsIcon(), () => model.ShowCameraLocations, value => model.ShowCameraLocations = value, () => model.Images.Count > 1),
                    AllowAddingMarkersVM => Toggle(owner, "AddMarker", "AddMarkerLabel", Glyph("\uE8ED"), () => owner.IsAddingMarkers, owner.SetAddingMarkers),
                    AutoUpdateFootprintVM => Toggle(owner, "AutoUpdate", "AutoUpdate", Glyph(ToolkitIcons.Refresh), () => model.AutoUpdateFootprint, value => model.AutoUpdateFootprint = value),
                    SelectNewMarkerSymbolVM picker => SymbolPicker(owner, picker),
                    ClearMarkersVM => ActionButton(owner, "ClearMarkers", "ClearMarkersLabel", Glyph("\uEB04"), () => model.ClearMarkersCommand.Execute(null), () => model.ClearMarkersCommand.CanExecute(null)),
                    SequentialNavigationVM => SequenceButton(owner),
                    _ => null,
                };
                if (control != null)
                {
                    _tools.Add(control);
                    PlatformView.Children.Add(control);
                }
            }
        }
        _updating = true;
        try { foreach (var update in _updates) update(); }
        finally { _updating = false; }
        LayoutToolbar();
    }

    private void LayoutToolbar()
    {
        if (VirtualView.ToolbarItem != null)
        {
            // MAUI owns category wrapping; each presenter must measure only its own tool.
            foreach (var tool in _tools)
            {
                tool.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                tool.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
            }
            VirtualView.HeightRequest = 84;
            return;
        }
        double width = Math.Max(1, Math.Floor(VirtualView.Width > 0 ? VirtualView.Width : VirtualView.Owner?.Width ?? 76));
        var visible = _tools.Where(tool => tool.Visibility == NativeVisibility.Visible).ToArray();
        if (_toolbarWidth == width && _visibleTools.SequenceEqual(visible))
            return;
        _toolbarWidth = width;
        _visibleTools.Clear();
        _visibleTools.AddRange(visible);
        PlatformView.ColumnDefinitions.Clear();
        PlatformView.RowDefinitions.Clear();
        var rows = new List<List<NativeView>>();
        double rowWidth = 0;
        double ToolWidth(NativeView tool) => tool is Microsoft.UI.Xaml.Controls.Border ? 17 :
            tool is NativeGrid ? 80 :
            tool is NativeButton { Flyout: not null } button ? button.MinWidth :
            AutomationProperties.GetAutomationId(tool) == "OrientedImageryDesktopAddMarker" ? 48 : 78;
        foreach (var tool in _visibleTools)
        {
            double toolWidth = Math.Min(width, ToolWidth(tool));
            if (rows.Count == 0 || rowWidth + toolWidth > width)
            {
                rows.Add([]);
                rowWidth = 0;
            }
            rows[rows.Count - 1].Add(tool);
            rowWidth += toolWidth;
        }
        for (int row = 0; row < rows.Count; row++)
        {
            PlatformView.RowDefinitions.Add(new Microsoft.UI.Xaml.Controls.RowDefinition { Height = new Microsoft.UI.Xaml.GridLength(84) });
            double left = Math.Max(0, (width - rows[row].Sum(tool => Math.Min(width, ToolWidth(tool)))) / 2);
            foreach (var tool in rows[row])
            {
                bool divider = tool is Microsoft.UI.Xaml.Controls.Border;
                tool.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
                tool.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                tool.Margin = new NativeThickness(left + (divider ? 8 : 0), 0, 0, 0);
                tool.Width = divider ? 1 : Math.Min(width, ToolWidth(tool));
                NativeGrid.SetRow(tool, row);
                left += Math.Min(width, ToolWidth(tool));
            }
        }
        PlatformView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        VirtualView.HeightRequest = rows.Count * 84;
    }

    private NativeButton ActionButton(OrientedImageryView owner, string key, string labelKey, UIElement icon, Action execute, Func<bool> enabled)
    {
        var button = new NativeButton
        {
            Content = LabeledContent(icon, labelKey), MinWidth = 0, Height = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(0), HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        };
        Describe(button, key);
        button.Click += (_, _) => { if (!_updating && enabled()) execute(); };
        _updates.Add(() => button.IsEnabled = owner.DesktopHasLayer && !owner.DesktopIsSearching && enabled());
        return button;
    }

    private ToggleButton Toggle(OrientedImageryView owner, string key, string labelKey, UIElement icon, Func<bool> get, Action<bool> set, Func<bool>? enabled = null)
    {
        var toggle = new ToggleButton
        {
            Content = LabeledContent(icon, labelKey, key == "AddMarker" ? 40 : 68),
            MinWidth = 0, Height = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(0),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), CornerRadius = new CornerRadius(0),
        };
        Describe(toggle, key);
        toggle.Click += (_, _) => { if (!_updating) set(toggle.IsChecked == true); };
        _updates.Add(() =>
        {
            bool selected = get();
            toggle.IsChecked = selected;
            toggle.IsEnabled = owner.DesktopHasLayer && !owner.DesktopIsSearching && (enabled?.Invoke() ?? true);
        });
        return toggle;
    }

    private NativeGrid ModePicker(OrientedImageryView owner, ImageSearchModeVM mode)
    {
        var model = owner.ViewModel;
        var host = new NativeGrid
        {
            Height = 84,
            ColumnDefinitions =
            {
                new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(56) },
                new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(24) },
            },
        };
        var button = new ToggleButton
        {
            MinWidth = 0, Height = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(0),
        };
        button.Click += (_, _) =>
        {
            if (!_updating && button.IsEnabled)
                mode.IsImageSelectionEnabled = button.IsChecked == true;
        };
        var dropdown = DropdownButton(24);
        dropdown.Padding = new NativeThickness(0);
        dropdown.Content = DropdownContent(new NativeGrid());
        NativeGrid.SetColumn(dropdown, 1);
        host.Children.Add(button);
        host.Children.Add(dropdown);
        var menu = new MenuFlyout();
        string groupName = Guid.NewGuid().ToString("N");
        dropdown.Flyout = menu;
        _detachSymbols.Add(menu.Hide);
        Describe(button, "ImageSearchMode", "OrientedImageryDesktopMode");
        Describe(dropdown, "ImageSearchMode", "OrientedImageryDesktopModeDropdown");
        foreach (var state in mode.ImageSearchModes)
        {
            bool search = state == OrientedImageryViewInteractionState.ExploreImages;
            string key = search ? "ImageSearchModeSearch" : "ImageSearchModeSelect";
            var item = new RadioMenuFlyoutItem
            {
                Text = Text(key), Icon = ImageModeIcon(search), GroupName = groupName,
            };
            AutomationProperties.SetName(item, Text(key));
            item.Click += (_, _) =>
            {
                if (!_updating && button.IsEnabled)
                    mode.SelectedInteractionState = state;
            };
            _updates.Add(() => item.IsChecked = model.InteractionState == state);
            menu.Items.Add(item);
        }
        bool? previous = null;
        _updates.Add(() =>
        {
            bool search = mode.SelectedInteractionState == OrientedImageryViewInteractionState.ExploreImages;
            if (previous != search)
            {
                button.Content = LabeledContent(ImageModeIcon(search),
                    search ? "ImageSearchModeSearch" : "ImageSearchModeSelect", 48);
                previous = search;
            }
            button.IsEnabled = owner.DesktopHasLayer && !owner.DesktopIsSearching;
            dropdown.IsEnabled = button.IsEnabled;
            button.IsChecked = mode.IsImageSelectionEnabled;
        });
        return host;
    }

    private static NativeButton DropdownButton(double width) => new()
    {
        MinWidth = width, Height = 84, Padding = new NativeThickness(4),
        BorderThickness = new NativeThickness(0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
        CornerRadius = new CornerRadius(0),
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
    };

    private NativeButton SequenceButton(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        var button = ActionButton(owner, "SequentialMode", "NavigationModeLabel", Geometry(Sequence), () => model.ToggleSequentialNavigationCommand.Execute(null), () => model.ToggleSequentialNavigationCommand.CanExecute(null));
        bool previous = false;
        Describe(button, "SequentialMode", "OrientedImageryDesktopSequentialMode");
        _updates.Add(() =>
        {
            if (previous != model.IsSequentialNavigationEnabled)
            {
                button.Content = LabeledContent(Geometry(model.IsSequentialNavigationEnabled ? ReturnToResults : Sequence), "NavigationModeLabel");
                previous = model.IsSequentialNavigationEnabled;
            }
            string key = model.IsSequentialNavigationEnabled ? "ReturnToResults" : "SequentialMode";
            if (AutomationProperties.GetName(button) != Text(key))
                Describe(button, key, "OrientedImageryDesktopSequentialMode");
        });
        return button;
    }

    private NativeButton SymbolPicker(OrientedImageryView owner, SelectNewMarkerSymbolVM picker)
    {
        var button = DropdownButton(32);
        button.Padding = new NativeThickness(0);
        var selectedImage = new Microsoft.UI.Xaml.Controls.Image { Width = 16, Height = 32 };
        button.Content = DropdownContent(selectedImage);
        var menu = new MenuFlyout();
        string groupName = Guid.NewGuid().ToString("N");
        button.Flyout = menu;
        _detachSymbols.Add(menu.Hide);
        Describe(button, "SelectMarkers");
        MarkerSymbol[] symbols = [];
        var images = new List<Microsoft.UI.Xaml.Controls.Image>();
        void UpdateSymbols()
        {
            if (symbols.SequenceEqual(picker.SymbolOptions)) return;
            symbols = picker.SymbolOptions.ToArray();
            menu.Items.Clear();
            images.Clear();
            foreach (var symbol in symbols)
            {
                var image = new Microsoft.UI.Xaml.Controls.Image { Width = 32, Height = 32 };
                images.Add(image);
                string name = symbol is SimpleMarkerSymbol simple ? Text("Marker" + simple.Style) :
                    string.Format(CultureInfo.CurrentCulture, Text("MarkerOption"), menu.Items.Count + 1);
                var item = new RadioMenuFlyoutItem { Text = name, Icon = new ImageIcon(), GroupName = groupName };
                AutomationProperties.SetName(item, name);
                item.Click += (_, _) =>
                {
                    if (!_updating && button.IsEnabled) picker.SelectedSymbol = symbol;
                };
                menu.Items.Add(item);
                _ = RenderSymbolAsync(owner, symbol, image, _generation);
            }
        }
        System.ComponentModel.PropertyChangedEventHandler changed = (_, _) => Refresh();
        System.Collections.Specialized.NotifyCollectionChangedEventHandler collectionChanged = (_, _) => Refresh();
        picker.PropertyChanged += changed;
        picker.SymbolOptions.CollectionChanged += collectionChanged;
        _detachSymbols.Add(() =>
        {
            picker.PropertyChanged -= changed;
            picker.SymbolOptions.CollectionChanged -= collectionChanged;
        });
        _updates.Add(() =>
        {
            UpdateSymbols();
            int selected = Array.IndexOf(symbols, picker.SelectedSymbol);
            selectedImage.Source = selected >= 0 ? images[selected].Source : null;
            for (int i = 0; i < menu.Items.Count; i++)
            {
                var item = (RadioMenuFlyoutItem)menu.Items[i];
                item.IsChecked = i == selected;
                ((ImageIcon)item.Icon).Source = images[i].Source;
            }
            button.IsEnabled = owner.DesktopHasLayer && !owner.DesktopIsSearching;
        });
        return button;
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
        var focused = PlatformView.XamlRoot == null ? null : FocusManager.GetFocusedElement(PlatformView.XamlRoot) as NativeButton;
        bool restoreFocus = focused != null && PlatformView.Children.OfType<StackPanel>().Any(panel => panel.Children.Contains(focused));
        string? focusedId = restoreFocus ? AutomationProperties.GetAutomationId(focused!) : null;
        var focusState = restoreFocus ? focused!.FocusState : FocusState.Unfocused;
        if (_model == model && _navigationState is { } previous &&
            previous.Sequential == state.Sequential && (state.Sequential || previous.Count == state.Count) &&
            PlatformView.Children.FirstOrDefault() is StackPanel existingRow)
        {
            _navigationState = state;
            UpdateNavigationRow(existingRow, state);
            if (restoreFocus)
                RestoreNavigationFocus(existingRow, focusedId, selected, focusState);
            return;
        }
        Clear();
        _navigationState = state;
        _model = model;
        var row = new StackPanel { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal, HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center, Height = 32 };
        PlatformView.Children.Add(row);
        NativeButton NavigateButton(bool next, bool label)
        {
            var command = next ? model.SelectNextImageCommand : model.SelectPreviousImageCommand;
            var button = new NativeButton
            {
                Content = label ? Text(next ? "NextLabel" : "PreviousLabel") : new FontIcon
                {
                    Glyph = next ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronLeft,
                    FontFamily = IconFont, FontSize = 16,
                },
                Height = 32, MinHeight = 0, MinWidth = 0, Padding = new NativeThickness(label ? 4 : 0),
                BorderThickness = new NativeThickness(0), CornerRadius = new CornerRadius(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                IsEnabled = !owner.DesktopIsSearching && command.CanExecute(null),
            };
            if (!label) button.Width = 33;
            Describe(button, next ? "NextLabel" : "PreviousLabel", next ? "OrientedImageryPaginatorNext" : "OrientedImageryPaginatorPrevious");
            button.Click += (_, _) => { if (command.CanExecute(null)) command.Execute(null); };
            return button;
        }
        row.Children.Add(NavigateButton(false, false));
        if (model.IsSequentialNavigationEnabled) row.Children.Add(NavigateButton(false, true));
        if (model.IsSequentialNavigationEnabled)
            row.Children.Add(PageIndicator(".", true));
        else
        {
            for (int i = start; i < start + count; i++)
            {
                int slot = i - start;
                var button = new NativeButton
                {
                    Content = PageIndicator((i + 1).ToString(CultureInfo.CurrentCulture), i == selected),
                    Width = 35, Height = 32, MinWidth = 0, MinHeight = 0, Padding = new NativeThickness(0),
                    BorderThickness = new NativeThickness(0), CornerRadius = new CornerRadius(0),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), IsEnabled = !owner.DesktopIsSearching,
                };
                AutomationProperties.SetName(button, string.Format(CultureInfo.CurrentCulture, Text(i == selected ? "SelectedPageFormat" : "GoToPageFormat"), i + 1, model.Images.Count));
                AutomationProperties.SetAutomationId(button, "OrientedImageryPage" + i.ToString(CultureInfo.InvariantCulture));
                button.Click += (_, _) =>
                {
                    int index = (_navigationState?.Start ?? 0) + slot;
                    if (VirtualView.Owner?.ViewModel == model && !model.IsSequentialNavigationEnabled && index < model.Images.Count)
                        model.SelectedImage = model.Images[index];
                };
                row.Children.Add(button);
            }
        }
        if (model.IsSequentialNavigationEnabled) row.Children.Add(NavigateButton(true, true));
        row.Children.Add(NavigateButton(true, false));
        VirtualView.HeightRequest = 32;
        if (restoreFocus)
            RestoreNavigationFocus(row, focusedId, selected, focusState);
    }

    private static void RestoreNavigationFocus(StackPanel row, string? focusedId, int selected, FocusState focusState)
    {
        var target = row.Children.OfType<NativeButton>().FirstOrDefault(button => AutomationProperties.GetAutomationId(button) == focusedId);
        target ??= row.Children.OfType<NativeButton>().FirstOrDefault(button => AutomationProperties.GetAutomationId(button) == "OrientedImageryPage" + selected.ToString(CultureInfo.InvariantCulture));
        target?.Focus(focusState);
    }

    private static void UpdateNavigationRow(StackPanel row, NavigationState state)
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
            AutomationProperties.SetName(button, string.Format(CultureInfo.CurrentCulture,
                Text(index == state.Selected ? "SelectedPageFormat" : "GoToPageFormat"), index + 1, state.Total));
            AutomationProperties.SetAutomationId(button, "OrientedImageryPage" + index.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static NativeGrid PageIndicator(string text, bool selected)
    {
        var grid = new NativeGrid { Width = 35, Height = 32 };
        grid.Children.Add(new TextBlock { Text = text, FontSize = 14, HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center, VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center });
        var underline = ThemeBorder("SystemControlHighlightAccentBrush");
        underline.Height = 2;
        underline.Margin = new NativeThickness(1, 0, 1, 0);
        underline.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom;
        underline.Visibility = selected ? NativeVisibility.Visible : NativeVisibility.Collapsed;
        grid.Children.Add(underline);
        return grid;
    }

    private const string AdditionalFootprints = "M17.1999 13L22 5H0L9 20H13L13.6 19L19 28H23L32 13H17.1999ZM12.439 19H9.56103L1.7832 6H20.2168L16.0287 13H9.99999L13.0187 18.0311L12.439 19ZM22.439 27H19.561L11.7832 14H30.2168L22.439 27Z";
    private const string Sequence = "M6 4V8H2V28H25V24H29V4H6ZM24 27H3V9H24V27ZM28 23H25V8H7V5H28V23ZM16.0732 21L19.5859 17.5L16.0876 14H17.5016L21 17.5L17.4872 21H16.0732ZM6.00897 17.5L9.51568 14H10.9297L7.42303 17.5L10.9063 21H9.49225L6.00897 17.5Z";
    private const string ReturnToResults = "M4 18H3V29H29V3H12V4H28V28H4V18ZM5.66 13.354L1.807 9.5L5.66 5.646L6.367 6.353L3.767 9H11.5C13.7489 8.9923 15.9086 9.87918 17.503 11.4652C19.0974 13.0512 19.9958 15.2061 20 17.455V22.855H19V17.455C18.9956 13.3147 15.6403 9.95941 11.5 9.955H3.675L6.367 12.646L5.66 13.354Z";
    private const string CurrentFootprint = "F1 M9.16358 0L7.67295 0L0 0L4.5818 7.6L11.4546 19H16.5455L28 0H9.16358Z M15.8314 17.7333H12.1686L2.26954 1.26667H25.7305L15.8314 17.7333Z";
}
#endif
