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
using Brush = Microsoft.UI.Xaml.Media.Brush;
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

    public bool IsNavigation { get; init; }

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
    private int _toolbarColumns;
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

    private static Brush ThemeBrush(string key) => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];

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
        _toolbarColumns = 0;
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
        if (_model != model || !_items.SequenceEqual(owner.ToolbarItems))
        {
            Clear();
            _model = model;
            _items = owner.ToolbarItems.ToArray();
            foreach (var item in _items)
            {
                NativeView? control = item switch
                {
                    ImageSearchModeVM => ModeButton(owner),
                    ShowSelectedFootprintVM => Toggle(owner, "CurrentFootprint", Geometry(CurrentFootprint), () => model.ShowSelectedFootprint, value => model.ShowSelectedFootprint = value),
                    ShowUnselectedFootprintsVM => Toggle(owner, "AdditionalFootprints", Geometry(AdditionalFootprints), () => model.ShowUnselectedFootprints, value => model.ShowUnselectedFootprints = value),
                    ShowCameraMarkersVM => Toggle(owner, "CameraLocations", Geometry(Camera), () => model.ShowCameraLocations, value => model.ShowCameraLocations = value),
                    AllowAddingMarkersVM => Toggle(owner, "AddMarker", Glyph("\uE8ED"), () => owner.IsAddingMarkers, owner.SetAddingMarkers),
                    AutoUpdateFootprintVM => Toggle(owner, "AutoUpdate", Glyph(ToolkitIcons.Refresh), () => model.AutoUpdateFootprint, value => model.AutoUpdateFootprint = value),
                    SelectNewMarkerSymbolVM picker => SymbolPicker(owner, picker),
                    ClearMarkersVM => ActionButton(owner, "ClearMarkers", Glyph("\uE83E"), () => model.ClearMarkersCommand.Execute(null), () => model.ClearMarkersCommand.CanExecute(null)),
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
        int visibleCount = 0;
        int cells = 0;
        bool changed = false;
        foreach (var tool in _tools)
        {
            if (tool.Visibility != NativeVisibility.Visible) continue;
            if (visibleCount >= _visibleTools.Count || _visibleTools[visibleCount] != tool)
                changed = true;
            visibleCount++;
            cells += tool is ComboBox ? 2 : 1;
        }
        double width = VirtualView.Width > 0 ? VirtualView.Width : VirtualView.Owner?.Width ?? 44;
        int columns = Math.Max(1, Math.Min(cells, (int)(Math.Max(44, width) / 44)));
        if (!changed && _visibleTools.Count == visibleCount && _toolbarColumns == columns)
            return;
        _toolbarColumns = columns;
        _visibleTools.Clear();
        foreach (var tool in _tools)
            if (tool.Visibility == NativeVisibility.Visible) _visibleTools.Add(tool);
        PlatformView.ColumnDefinitions.Clear();
        PlatformView.RowDefinitions.Clear();
        for (int i = 0; i < columns; i++)
            PlatformView.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(44) });
        int row = 0;
        int column = 0;
        foreach (var tool in _visibleTools)
        {
            int span = tool is ComboBox ? Math.Min(2, columns) : 1;
            if (column + span > columns)
            {
                row++;
                column = 0;
            }
            NativeGrid.SetColumn(tool, column);
            NativeGrid.SetColumnSpan(tool, span);
            NativeGrid.SetRow(tool, row);
            column += span;
        }
        int rows = visibleCount == 0 ? 0 : row + 1;
        for (int i = 0; i < rows; i++)
            PlatformView.RowDefinitions.Add(new Microsoft.UI.Xaml.Controls.RowDefinition { Height = new Microsoft.UI.Xaml.GridLength(44) });
        PlatformView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
        VirtualView.HeightRequest = rows * 44;
    }

    private NativeButton ActionButton(OrientedImageryView owner, string key, UIElement icon, Action execute, Func<bool> enabled)
    {
        var button = new NativeButton
        {
            Content = icon, MinWidth = 40, MinHeight = 40, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(0), HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        };
        Describe(button, key);
        button.Click += (_, _) => { if (!_updating && enabled()) execute(); };
        _updates.Add(() => button.IsEnabled = owner.DesktopHasLayer && !owner.DesktopIsSearching && enabled());
        return button;
    }

    private ToggleButton Toggle(OrientedImageryView owner, string key, UIElement icon, Func<bool> get, Action<bool> set)
    {
        var toggle = new ToggleButton
        {
            Content = icon, MinWidth = 40, MinHeight = 40, Padding = new NativeThickness(4),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), CornerRadius = new CornerRadius(0),
        };
        Describe(toggle, key);
        toggle.Click += (_, _) => { if (!_updating) set(toggle.IsChecked == true); };
        _updates.Add(() =>
        {
            bool selected = get();
            toggle.IsChecked = selected;
            toggle.IsEnabled = owner.DesktopHasLayer && !owner.DesktopIsSearching;
            toggle.BorderThickness = new NativeThickness(selected ? 1 : 0);
            toggle.BorderBrush = ThemeBrush("TextFillColorPrimaryBrush");
        });
        return toggle;
    }

    private NativeButton ModeButton(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        var icon = Glyph("\uEA72");
        var button = ActionButton(owner, "Mode", icon, () => owner.SelectImageMode(!owner.ImageSearchEnabled), () => !model.IsSequentialNavigationEnabled);
        _updates.Add(() =>
        {
            icon.Glyph = owner.ImageSearchEnabled ? "\uEA72" : "\uEA74";
            string key = owner.ImageSearchEnabled ? "SwitchToSelect" : "SwitchToSearch";
            if (AutomationProperties.GetName(button) != Text(key))
                Describe(button, key, "OrientedImageryDesktopMode");
        });
        return button;
    }

    private NativeButton SequenceButton(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        var button = ActionButton(owner, "SequentialMode", Geometry(Sequence), () => model.ToggleSequentialNavigationCommand.Execute(null), () => model.ToggleSequentialNavigationCommand.CanExecute(null));
        bool previous = false;
        Describe(button, "SequentialMode", "OrientedImageryDesktopSequentialMode");
        _updates.Add(() =>
        {
            if (previous != model.IsSequentialNavigationEnabled)
            {
                button.Content = Geometry(model.IsSequentialNavigationEnabled ? ReturnToResults : Sequence);
                previous = model.IsSequentialNavigationEnabled;
            }
            string key = model.IsSequentialNavigationEnabled ? "ReturnToResults" : "SequentialMode";
            if (AutomationProperties.GetName(button) != Text(key))
                Describe(button, key, "OrientedImageryDesktopSequentialMode");
        });
        return button;
    }

    private ComboBox SymbolPicker(OrientedImageryView owner, SelectNewMarkerSymbolVM picker)
    {
        var combo = new ComboBox
        {
            Width = 72, MinWidth = 40, MinHeight = 40, Padding = new NativeThickness(2), CornerRadius = new CornerRadius(0),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
        };
        Describe(combo, "SelectMarkers");
        MarkerSymbol[] symbols = [];
        void UpdateSymbols()
        {
            if (symbols.SequenceEqual(picker.SymbolOptions)) return;
            symbols = picker.SymbolOptions.ToArray();
            combo.Items.Clear();
            foreach (var symbol in symbols)
            {
                var image = new Microsoft.UI.Xaml.Controls.Image { Width = 32, Height = 32 };
                var item = new ComboBoxItem { Content = image, Tag = symbol, Padding = new NativeThickness(4), MinHeight = 40 };
                AutomationProperties.SetName(item, symbol is SimpleMarkerSymbol simple ? Text("Marker" + simple.Style) :
                    string.Format(CultureInfo.CurrentCulture, Text("MarkerOption"), combo.Items.Count + 1));
                combo.Items.Add(item);
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
        combo.SelectionChanged += (_, _) =>
        {
            if (!_updating && combo.SelectedItem is ComboBoxItem { Tag: MarkerSymbol symbol })
                picker.SelectedSymbol = symbol;
        };
        _updates.Add(() =>
        {
            UpdateSymbols();
            combo.SelectedIndex = Array.IndexOf(symbols, picker.SelectedSymbol);
            combo.Visibility = owner.IsAddingMarkers ? NativeVisibility.Visible : NativeVisibility.Collapsed;
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
                image.Source = source;
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
        grid.Children.Add(new Microsoft.UI.Xaml.Controls.Border
        {
            Height = 2, Margin = new NativeThickness(1, 0, 1, 0), VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom,
            Background = ThemeBrush("SystemControlHighlightAccentBrush"), Visibility = selected ? NativeVisibility.Visible : NativeVisibility.Collapsed,
        });
        return grid;
    }

    private const string AdditionalFootprints = "M17.1999 13L22 5H0L9 20H13L13.6 19L19 28H23L32 13H17.1999ZM12.439 19H9.56103L1.7832 6H20.2168L16.0287 13H9.99999L13.0187 18.0311L12.439 19ZM22.439 27H19.561L11.7832 14H30.2168L22.439 27Z";
    private const string Sequence = "M6 4V8H2V28H25V24H29V4H6ZM24 27H3V9H24V27ZM28 23H25V8H7V5H28V23ZM16.0732 21L19.5859 17.5L16.0876 14H17.5016L21 17.5L17.4872 21H16.0732ZM6.00897 17.5L9.51568 14H10.9297L7.42303 17.5L10.9063 21H9.49225L6.00897 17.5Z";
    private const string ReturnToResults = "M4 18H3V29H29V3H12V4H28V28H4V18ZM5.66 13.354L1.807 9.5L5.66 5.646L6.367 6.353L3.767 9H11.5C13.7489 8.9923 15.9086 9.87918 17.503 11.4652C19.0974 13.0512 19.9958 15.2061 20 17.455V22.855H19V17.455C18.9956 13.3147 15.6403 9.95941 11.5 9.955H3.675L6.367 12.646L5.66 13.354Z";
    private const string Camera = "M11 13H6V12H11V13ZM30 23.5V11.5C29.9983 10.12 28.88 9.00165 27.5 9H27C26.4479 8.99945 26.0006 8.55206 26 8C25.9989 6.89589 25.1041 6.0011 24 6H16C14.8959 6.0011 14.0011 6.89589 14 8C13.9994 8.55206 13.5521 8.99945 13 9H11V8H6V9H4.5C3.11997 9.00165 2.00165 10.12 2 11.5V23.5C2.00165 24.88 3.11997 25.9983 4.5 26H27.5C28.88 25.9983 29.9983 24.88 30 23.5ZM7 10V9H10V10H13C14.1041 9.9989 14.9989 9.10411 15 8C15.0006 7.44794 15.4479 7.00055 16 7H24C24.5521 7.00055 24.9994 7.44794 25 8C25.0011 9.10411 25.8959 9.9989 27 10H27.5C28.328 10.0011 28.9989 10.672 29 11.5V23.5C28.9989 24.328 28.328 24.9989 27.5 25H4.5C3.67203 24.9989 3.0011 24.328 3 23.5V11.5C3.0011 10.672 3.67203 10.0011 4.5 10H7ZM25.8 17C25.8 13.7967 23.2033 11.2 20 11.2C16.7967 11.2 14.2 13.7967 14.2 17C14.2 20.2033 16.7967 22.8 20 22.8C23.2019 22.7967 25.7967 20.2019 25.8 17ZM24.8 17C24.8 19.651 22.651 21.8 20 21.8C17.349 21.8 15.2 19.651 15.2 17C15.2 14.349 17.349 12.2 20 12.2C22.6498 12.2028 24.7972 14.3502 24.8 17Z";
    private const string CurrentFootprint = "M28.9869 20.3149L30.0339 26.5899C28.6639 26.3499 26.4439 26.0599 23.7139 26.0599C23.5449 26.0599 23.3899 26.0669 23.2279 26.0699L22.1879 27.1099C22.6649 27.0789 23.1699 27.0599 23.7139 27.0599C28.1639 27.0599 31.2539 27.8599 31.2539 27.8599L29.8559 19.4469L28.9869 20.3149ZM14.0529 27.9879C12.6329 28.3739 11.0459 28.6599 8.79392 28.6599C5.78392 28.6599 3.42392 27.7699 2.36392 27.2899L6.04392 5.07993C7.06392 5.37993 8.58392 5.72993 10.5939 5.72993C13.0939 5.72993 14.5939 5.29993 16.0339 4.87993C17.4839 4.45993 18.8539 4.06993 21.3239 4.06993C23.5139 4.06993 25.4339 4.39993 26.3739 4.58993L28.0719 14.7939L29.1319 15.1019L27.2539 3.75993C27.2539 3.75993 24.5839 3.06993 21.3239 3.06993C16.1839 3.06993 15.4039 4.72993 10.5939 4.72993C7.77392 4.72993 5.95392 3.98993 5.25392 3.75993L1.25392 27.8599C1.25392 27.8599 4.34392 29.6599 8.79392 29.6599C11.1669 29.6599 12.8529 29.3499 14.3309 28.9429L14.0529 27.9879ZM10.7539 9.85993C9.92392 9.85993 9.25392 10.5299 9.25392 11.3599C9.25392 12.1899 9.92392 12.8599 10.7539 12.8599C11.5839 12.8599 12.2539 12.1899 12.2539 11.3599C12.2539 10.5299 11.5839 9.85993 10.7539 9.85993ZM10.7539 11.8599C10.4739 11.8599 10.2539 11.6399 10.2539 11.3599C10.2539 11.0799 10.4739 10.8599 10.7539 10.8599C11.0339 10.8599 11.2539 11.0799 11.2539 11.3599C11.2539 11.6399 11.0339 11.8599 10.7539 11.8599ZM12.9699 12.4929C12.7409 12.939 12.3831 13.3059 11.9429 13.5459L13.0966 17.2199L15.3639 25.3399L16.6639 29.7999L29.2539 17.2199L12.9699 12.4929ZM14.0139 17.0699L12.9839 13.5299L27.3539 17.6999L17.1539 27.8999L14.0139 17.0699Z";
}
#endif
