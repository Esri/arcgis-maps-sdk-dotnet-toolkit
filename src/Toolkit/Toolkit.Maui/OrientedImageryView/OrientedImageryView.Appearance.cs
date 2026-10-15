using System.ComponentModel;
using System.Globalization;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private Label? _title;
    private Label? _status;
    private Label? _notice;
    private ActivityIndicator? _busy;
    private Button? _layerButton;
    private Button? _overlaysButton;
    private Microsoft.Maui.Controls.Shapes.Path? _overlaysIcon;
    private Button? _markersButton;
    private Button? _modeButton;
    private Button? _moreButton;
    private Button? _previousButton;
    private Button? _nextButton;
    private Button? _imagePreviousButton;
    private Button? _imageNextButton;
    private Grid? _panelHost;
    private Grid? _panelLayout;
    private ScrollView? _panelScroll;
    private HorizontalStackLayout? _pages;
    private HorizontalStackLayout? _customToolbar;
    private View? _customToolbarScroll;
    private Grid? _tools;
    private Button?[] _toolButtons = [];
    private (bool Sequential, int Start, int Count, int Total)? _pageState;
    private int _selectedPage = -1;
    private View? _panelAnchor;
    private int _menuVersion;
    private string? _lastNotice;
    private readonly List<Action> _panelUpdates = new();
    private readonly List<Action> _panelCleanup = new();
    private Border? _panel;
    private Grid? _panelBackdrop;
    private bool _updatingPanel;
    private string? _panelKind;
#if !WINDOWS
    private Page? _presentationPage;
    private bool? _pageLandscape;
#endif
#if !WINDOWS
    private PopupViewer? _popupViewer;
    private Grid? _popupHost;
    private Button? _popupCloseButton;
    private Grid? _navigation;
    private Grid? _symbolGrid;
    private readonly List<Border> _symbolTiles = new();
#endif

    private static string Text(string key) => Properties.Resources.GetString("OrientedImageryView" + (key switch
    {
        "SwitchToSearch" => "SwitchToImageSearch",
        "SwitchToSelect" => "SwitchToImageSelection",
        "SelectMarkers" => "SelectMarkerSymbol",
        "CurrentFootprint" => "ShowSelectedImageFootprint",
        "AdditionalFootprints" => "ShowUnselectedImageFootprints",
        "CameraLocations" => "ShowCameraMarkersOnGeoView",
        "NoLayers" => "NoOrientedImageryLayers",
        "NoLayer" => "NoLayerSelected",
        "InvalidLayer" => "SelectedLayerNotOnGeoView",
        _ => key,
    }));

    private static readonly ControlTemplate DefaultControlTemplate =
#if WINDOWS
        new(CreateDesktopTemplate);
#else
        new(() =>
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            RowSpacing = 0,
        };
        root.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
#if __IOS__ && !MACCATALYST
        root.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#F2F2F7"), Color.FromArgb("#1C1C1E"));
#endif
        INameScope scope = new NameScope();
        NameScope.SetNameScope(root, scope);
        void Add(string name, View view, Grid parent, int row = 0, int column = 0)
        {
            scope.RegisterName(name, view);
            Grid.SetRow(view, row);
            Grid.SetColumn(view, column);
            parent.Children.Add(view);
        }

        var heading = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Padding = new Thickness(8, 0),
        };
        root.Children.Add(heading);
        var title = new Label { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation, FontSize = 16 };
        title.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
#if __IOS__ && !MACCATALYST
        title.FontSize = 20;
        title.FontAttributes = FontAttributes.Bold;
        heading.Padding = new Thickness(12, 4);
#endif
        Add("PART_Title", title, heading);
        var layerButton = ToolButton("SelectLayer", "", "OrientedImageryLayers");
        layerButton.Text = null;
        layerButton.WidthRequest = 44;
        layerButton.BorderWidth = 0;
#if __IOS__ && !MACCATALYST
        layerButton.CornerRadius = 22;
        layerButton.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#3A3A3C"));
#endif
        Add("PART_Layers", layerButton, heading, column: 1);
        var layerIcon = MenuIcon(null, LayerGeometry);
        layerIcon.InputTransparent = true;
        layerIcon.HorizontalOptions = LayoutOptions.Center;
        heading.Add(layerIcon, 1);

        var tools = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 4,
            Padding = new Thickness(8, 0),
        };
        Add("PART_Tools", tools, root, 1);
        void AddToolbarTool(string name, string key, View icon, int column)
        {
            var cell = new Grid { MinimumHeightRequest = 44 };
            StackBase content =
#if __IOS__ && !MACCATALYST
                new VerticalStackLayout
#else
                new HorizontalStackLayout
#endif
            {
                Spacing = 4, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            };
            icon.WidthRequest = 16;
            icon.HeightRequest = 16;
            content.Add(icon);
            var label = new Label { Text = Text(key), FontSize = 12, VerticalOptions = LayoutOptions.Center, MaxLines = 1 };
            label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
            content.Add(label);
            var pill = new Border
            {
                Content = content, HeightRequest = 26, VerticalOptions = LayoutOptions.Center,
                Stroke = Color.FromArgb("#CCCCCC"), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 13 }, InputTransparent = true,
            };
#if __IOS__ && !MACCATALYST
            cell.MinimumHeightRequest = 60;
            icon.WidthRequest = 22;
            icon.HeightRequest = 22;
            label.FontSize = 11;
            label.TextColor = IosAccent;
            label.HandlerChanged += (sender, _) =>
            {
                if (sender is Label { Handler.PlatformView: UIKit.UILabel nativeLabel })
                    nativeLabel.Font = UIKit.UIFont.SystemFontOfSize(nativeLabel.Font.PointSize, UIKit.UIFontWeight.Semibold);
            };
            pill.HeightRequest = -1;
            pill.VerticalOptions = LayoutOptions.Fill;
            pill.Margin = new Thickness(0, 4, 0, 8);
            pill.StrokeThickness = 0;
            pill.StrokeShape = new RoundRectangle { CornerRadius = 10 };
            pill.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#3A3A3C"));
#endif
#if ANDROID
            label.HandlerChanged += (sender, _) =>
            {
                if (sender is Label { Handler.PlatformView: Android.Widget.TextView nativeLabel })
                    nativeLabel.Typeface = Android.Graphics.Typeface.Create(nativeLabel.Typeface, 600, false);
            };
#endif
            cell.Add(pill);
            var button = ToolButton(key, "", "OrientedImagery" + key);
            button.Text = null;
            button.BorderWidth = 0;
            button.Padding = 0;
            Add(name, button, cell);
            tools.Add(cell, column);
            cell.SetBinding(IsVisibleProperty, Binding.Create(static (Button b) => b.IsVisible, source: button));
        }
        AddToolbarTool("PART_Mode", "Mode", MenuIcon(ToolkitIcons.Search, null), 0);
        var overlayIcon = MenuIcon(null, OverlayGeometry);
        scope.RegisterName("PART_OverlaysIcon", overlayIcon);
        AddToolbarTool("PART_Overlays", "Overlays", overlayIcon, 1);
        AddToolbarTool("PART_Markers", "Markers", MenuIcon("\uE8ED", null), 2);
        AddToolbarTool("PART_More", "More", new Label
        {
            Text = "\u2022\u2022\u2022", FontSize = 12, TextColor = Color.FromArgb("#007AC2"),
            VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center,
        }, 3);
        var custom = new HorizontalStackLayout { Spacing = 4 };
        scope.RegisterName("PART_CustomToolbar", custom);
        var customScroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = custom };
        Add("PART_CustomToolbarScroll", customScroll, root, 1);

        var image = new Grid();
        Grid.SetRow(image, 2);
        root.Children.Add(image);
        Add("PART_Viewport", new OrientedImageViewport { AutomationId = "OrientedImageryViewport" }, image);
        var status = new Label { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, Margin = 20, AutomationId = "OrientedImageryStatus" };
        status.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#666666"), Color.FromArgb("#DDDDDD"));
        Add("PART_Status", status, image);
        Add("PART_Busy", new ActivityIndicator { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, AutomationId = "OrientedImageryBusy" }, image);
        Add("PART_ImagePrevious", MobileImageArrow(false), image);
        Add("PART_ImageNext", MobileImageArrow(true), image);
        var popupHost = new Grid
        {
            IsVisible = false,
            RowDefinitions = { new RowDefinition(GridLength.Star) },
        };
        Add("PART_PopupHost", popupHost, image);
        var popupClose = ToolButton("CloseSelectedImagePopup", "", "OrientedImageryPopupClose");
        popupClose.Text = "X";
        popupClose.WidthRequest = 44;
        popupClose.BorderWidth = 0;
        popupClose.HorizontalOptions = LayoutOptions.End;
        popupClose.VerticalOptions = LayoutOptions.Start;
        popupClose.IsVisible = false;
#if __IOS__ && !MACCATALYST
        popupClose.Text = "\u00D7";
        popupClose.FontSize = 24;
        popupClose.CornerRadius = 22;
#endif
        popupClose.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E6FFFFFF"), Color.FromArgb("#E6202020"));
        Add("PART_Popup", new PopupViewer
        {
            AutomationId = "OrientedImageryPopupViewer", Padding = 0,
            Margin = 0,
        }, popupHost);
        Add("PART_PopupClose", popupClose, popupHost);

        var navigation = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
            HorizontalOptions = LayoutOptions.Center,
            ColumnSpacing = 0,
            Padding = new Thickness(8, 0),
        };
        Add("PART_Navigation", navigation, root, 3);
        var previous = ToolButton("PreviousLabel", ToolkitIcons.ChevronLeft, "OrientedImageryPrevious");
        previous.Text = null;
        previous.WidthRequest = 44;
        previous.BorderWidth = 0;
        Add("PART_Previous", previous, navigation);
        Add("PART_Pages", new HorizontalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Spacing = 0 }, navigation, column: 1);
        var next = ToolButton("NextLabel", ToolkitIcons.ChevronRight, "OrientedImageryNext");
        next.Text = null;
        next.WidthRequest = 44;
        next.BorderWidth = 0;
        Add("PART_Next", next, navigation, column: 2);
        var notice = new Label { HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(8, 4), AutomationId = "OrientedImageryNotice" };
        notice.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        Add("PART_Notice", notice, root, 4);
        var panels = new Grid { IsVisible = false, InputTransparent = true, CascadeInputTransparent = false };
        Add("PART_PanelHost", panels, root);
        Grid.SetRowSpan(panels, 5);
        return root;
    });
#endif

    private static Button MobileImageArrow(bool next)
    {
        var button = ToolButton(next ? "NextLabel" : "PreviousLabel", "",
            next ? "OrientedImageryImageNext" : "OrientedImageryImagePrevious");
        button.Text = next ? "\u25B6" : "\u25C0";
        button.FontSize = 20;
        button.WidthRequest = 25;
        button.MinimumWidthRequest = 25;
        button.Padding = 0;
        button.HeightRequest = 60;
        button.BorderWidth = 0;
        button.CornerRadius = 0;
        button.Margin = 0;
        button.HorizontalOptions = next ? LayoutOptions.End : LayoutOptions.Start;
        button.VerticalOptions = LayoutOptions.Center;
        button.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        return button;
    }

    private static Button ToolButton(string key, string glyph, string automationId)
    {
        var button = new Button
        {
            Text = Text(key),
            ImageSource = string.IsNullOrEmpty(glyph) ? null :
                new FontImageSource { Glyph = glyph, FontFamily = ToolkitIcons.FontFamilyName, Color = Color.FromArgb("#007AC2"), Size = 16 },
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 4),
            FontSize = 12,
            Padding = new Thickness(6, 0),
            MinimumHeightRequest = 44,
            CornerRadius = 12,
            BorderWidth = 1,
            BorderColor = Color.FromArgb("#999999"),
            BackgroundColor = Colors.Transparent,
            AutomationId = automationId,
        };
#if WINDOWS
        button.CornerRadius = 2;
        button.FontSize = 14;
        button.Padding = new Thickness(12, 4);
#endif
        button.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
        SemanticProperties.SetDescription(button, Text(key));
        return button;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        UnwireTemplate();
        base.OnApplyTemplate();
        _viewport = GetTemplateChild("PART_Viewport") as OrientedImageViewport;
#if !WINDOWS
        _popupViewer = GetTemplateChild("PART_Popup") as PopupViewer;
        _popupHost = GetTemplateChild("PART_PopupHost") as Grid;
        _popupCloseButton = GetTemplateChild("PART_PopupClose") as Button;
        if (_popupCloseButton != null) _popupCloseButton.Clicked += ClosePopup;
        _navigation = GetTemplateChild("PART_Navigation") as Grid;
#endif
        _title = GetTemplateChild("PART_Title") as Label;
        _status = GetTemplateChild("PART_Status") as Label;
        _notice = GetTemplateChild("PART_Notice") as Label;
        _busy = GetTemplateChild("PART_Busy") as ActivityIndicator;
        _layerButton = GetTemplateChild("PART_Layers") as Button;
        _overlaysButton = GetTemplateChild("PART_Overlays") as Button;
        _overlaysIcon = GetTemplateChild("PART_OverlaysIcon") as Microsoft.Maui.Controls.Shapes.Path;
        _markersButton = GetTemplateChild("PART_Markers") as Button;
        _modeButton = GetTemplateChild("PART_Mode") as Button;
        _moreButton = GetTemplateChild("PART_More") as Button;
        _toolButtons = [_overlaysButton, _markersButton, _modeButton, _moreButton];
        _previousButton = GetTemplateChild("PART_Previous") as Button;
        _nextButton = GetTemplateChild("PART_Next") as Button;
        _imagePreviousButton = GetTemplateChild("PART_ImagePrevious") as Button;
        _imageNextButton = GetTemplateChild("PART_ImageNext") as Button;
        _pages = GetTemplateChild("PART_Pages") as HorizontalStackLayout;
        _panelHost = GetTemplateChild("PART_PanelHost") as Grid;
        if (_panelHost != null) _panelHost.SizeChanged += PanelHostSizeChanged;
        _customToolbar = GetTemplateChild("PART_CustomToolbar") as HorizontalStackLayout;
        _customToolbarScroll = GetTemplateChild("PART_CustomToolbarScroll") as View;
        _tools = GetTemplateChild("PART_Tools") as Grid;
#if WINDOWS
        InitializeDesktopTemplate();
#endif
        if (_viewport != null)
        {
            _viewport.ImageTapped += ImageTapped;
            _viewport.StateChanged += ViewportChanged;
        }

        if (_layerButton != null) _layerButton.Clicked += SelectLayer;
        if (_overlaysButton != null) _overlaysButton.Clicked += ShowOverlays;
        if (_markersButton != null) _markersButton.Clicked += ShowMarkers;
        if (_modeButton != null) _modeButton.Clicked += ShowModes;
        if (_moreButton != null) _moreButton.Clicked += ShowMore;
        if (_previousButton != null) _previousButton.Clicked += Previous;
        if (_nextButton != null) _nextButton.Clicked += Next;
        if (_imagePreviousButton != null) _imagePreviousButton.Clicked += Previous;
        if (_imageNextButton != null) _imageNextButton.Clicked += Next;
        if (_pages != null) _pages.SizeChanged += PageSizeChanged;
        Refresh();
    }

    /// <inheritdoc/>
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        DetachPlatformPresentation();
#if !WINDOWS
        SetPresentationPage(null);
#endif
        base.OnHandlerChanging(args);
    }

    /// <inheritdoc/>
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null) return;
        AttachPlatformPresentation();
#if !WINDOWS
        ObservePresentationPage();
#endif
        Refresh();
    }

#if !WINDOWS
    private void PresentationLoaded(object? sender, EventArgs e)
    {
        ObservePresentationPage();
        AttachBackNavigation();
    }

    private void PresentationUnloaded(object? sender, EventArgs e)
    {
        SetPresentationPage(null);
        DetachBackNavigation();
    }

    private void ObservePresentationPage()
    {
        Element? ancestor = Parent;
        while (ancestor != null && ancestor is not Page)
            ancestor = ancestor.Parent;
        SetPresentationPage(ancestor as Page);
    }

    private void SetPresentationPage(Page? page)
    {
        if (_presentationPage == page) return;
        if (_presentationPage != null)
            _presentationPage.SizeChanged -= PresentationPageSizeChanged;
        _presentationPage = page;
        _pageLandscape = null;
        if (page != null)
        {
            page.SizeChanged += PresentationPageSizeChanged;
            PresentationPageSizeChanged(page, EventArgs.Empty);
        }
    }

    private void PresentationPageSizeChanged(object? sender, EventArgs e)
    {
        if (_presentationPage is not { Width: > 0, Height: > 0 } page) return;
        bool landscape = page.Width > page.Height;
        if (_pageLandscape != null && _pageLandscape != landscape)
            DismissTools();
        _pageLandscape = landscape;
    }
#endif

    private void UnwireTemplate()
    {
#if WINDOWS
        ReleaseDesktopTemplate();
#endif
        DismissPanel();
        _toolHighlights.Clear();
        if (_panelHost != null) _panelHost.SizeChanged -= PanelHostSizeChanged;
        if (_viewport != null)
        {
            _viewport.ImageTapped -= ImageTapped;
            _viewport.StateChanged -= ViewportChanged;
            _viewport.Footprint = null;
            _viewport.Markers = null;
        }

        if (_layerButton != null) _layerButton.Clicked -= SelectLayer;
        if (_overlaysButton != null) _overlaysButton.Clicked -= ShowOverlays;
        if (_markersButton != null) _markersButton.Clicked -= ShowMarkers;
        if (_modeButton != null) _modeButton.Clicked -= ShowModes;
        if (_moreButton != null) _moreButton.Clicked -= ShowMore;
        if (_previousButton != null) _previousButton.Clicked -= Previous;
        if (_nextButton != null) _nextButton.Clicked -= Next;
        if (_imagePreviousButton != null) _imagePreviousButton.Clicked -= Previous;
        if (_imageNextButton != null) _imageNextButton.Clicked -= Next;
        if (_pages != null) _pages.SizeChanged -= PageSizeChanged;
        if (_customToolbar != null) BindableLayout.SetItemsSource(_customToolbar, null);
        _viewport = null;
        _title = null;
        _status = null;
        _notice = null;
        _busy = null;
        _layerButton = null;
        _overlaysButton = null;
        _overlaysIcon = null;
        _markersButton = null;
        _modeButton = null;
        _moreButton = null;
        _previousButton = null;
        _nextButton = null;
        _imagePreviousButton = null;
        _imageNextButton = null;
        _pages?.Clear();
        _pages = null;
        _pageState = null;
        _selectedPage = -1;
        _toolButtons = [];
        _customToolbar = null;
        _customToolbarScroll = null;
        _tools = null;
        _panelHost = null;
#if !WINDOWS
        if (_popupViewer != null) _popupViewer.Popup = null;
        if (_popupCloseButton != null) _popupCloseButton.Clicked -= ClosePopup;
        _popupCloseButton = null;
        _popupHost = null;
        _popupViewer = null;
        _navigation = null;
#endif
    }

    private void ViewportChanged(object? sender, EventArgs e) => Refresh();

    private bool _refreshing;
    private bool _refreshPending;

    private void Refresh()
    {
        _refreshPending = true;
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            do
            {
                _refreshPending = false;
                RefreshPresentation();
            }
            while (_refreshPending);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void RefreshPresentation()
    {
        if (ViewModel == null)
            return;
        if (_viewport != null)
        {
            _viewport.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
            var themeBackground = (Color)GetValue(ThemeImageBackgroundProperty);
            _viewport.DisplayBackgroundColor = DisplayBackgroundColor.IsEmpty
                ? System.Drawing.Color.FromArgb(
                    (int)(themeBackground.Alpha * 255), (int)(themeBackground.Red * 255),
                    (int)(themeBackground.Green * 255), (int)(themeBackground.Blue * 255))
                : DisplayBackgroundColor;
            _viewport.Markers = _connected ? ViewModel.DisplayMarkers : null;
            _viewport.Footprint = _connected ? ViewModel.SelectedImageFootprint : null;
        }

        if (_title != null) _title.Text = SelectedLayer?.Name ?? Text("NoLayerTitle");
        if (_layerButton != null) _layerButton.IsEnabled = _layers.Count > 0;
        if (_busy != null)
        {
            _busy.IsRunning = _searching || _viewport?.IsBusy == true || SelectedLayer?.LoadStatus == LoadStatus.Loading;
            _busy.IsVisible = _busy.IsRunning;
        }

        string? error = _operationError?.Message ?? _viewport?.Error?.Message;
        string? message = error;
        if (message == null)
        {
            if (GeoView == null) message = Text("NoGeoView");
            else if (_layers.Count == 0) message = Text("NoLayers");
            else if (SelectedLayer == null) message = Text("NoLayer");
            else if (!_layers.Contains(SelectedLayer)) message = Text("InvalidLayer");
            else if (ViewModel.SelectedImage == null && !_searching) message = Text("NoImage");
        }
        ErrorMessage = error ?? ViewModel.SequentialNavigationError?.Message ?? message;

        if (_status != null)
        {
            _status.Text = message;
            _status.IsVisible = message != null;
        }

        bool hasLayer = SelectedLayer != null && _layers.Contains(SelectedLayer);
        bool hasOverlays = HasTool<ShowSelectedFootprintVM>() || HasTool<ShowUnselectedFootprintsVM>() || HasTool<ShowCameraMarkersVM>();
#if !WINDOWS
        hasOverlays |= HasTool<ShowSelectedImagePopupVM>();
#endif
        SetToolState(_overlaysButton, hasLayer, hasOverlays);
        SetToolState(_markersButton, hasLayer && !_searching, HasTool<AllowAddingMarkersVM>() || HasTool<SelectNewMarkerSymbolVM>() || HasTool<ClearMarkersVM>());
        SetToolState(_modeButton, hasLayer && !ViewModel.IsSequentialNavigationEnabled, HasTool<ImageSearchModeVM>());
#if WINDOWS
        SetToolState(_moreButton, ViewModel.ToggleSequentialNavigationCommand.CanExecute(null), HasTool<SequentialNavigationVM>());
#else
        SetToolState(_moreButton, hasLayer, HasTool<SequentialNavigationVM>());
#endif
        if (_modeButton?.ImageSource is FontImageSource modeIcon)
            modeIcon.Glyph = ViewModel.ImageSearchEnabled ? ToolkitIcons.Search : "\uEA74";
#if !WINDOWS
        if (_modeButton?.Parent is Grid modeCell && modeCell.Children.OfType<Border>().FirstOrDefault()?.Content is Layout modeContent &&
            modeContent.Children[0] is Image { Source: FontImageSource mobileModeIcon })
            mobileModeIcon.Glyph = ViewModel.ImageSearchEnabled ? ToolkitIcons.Search : "\uEA74";
#endif
        UpdateToolHighlights();
        if (_tools != null) _tools.IsVisible = ToolbarItemTemplateSelector == null;
        if (_customToolbar != null)
        {
            if (BindableLayout.GetItemTemplateSelector(_customToolbar) != ToolbarItemTemplateSelector)
                BindableLayout.SetItemTemplateSelector(_customToolbar, ToolbarItemTemplateSelector);
            var items = ToolbarItemTemplateSelector == null ? null : ViewModel.ToolbarItems;
            if (BindableLayout.GetItemsSource(_customToolbar) != items)
                BindableLayout.SetItemsSource(_customToolbar, items);
        }

        if (_customToolbarScroll != null)
            _customToolbarScroll.IsVisible = ToolbarItemTemplateSelector != null;
        if (_notice != null)
        {
            _notice.Text = ViewModel.SequentialNavigationError?.Message;
            _notice.IsVisible = _notice.Text != null;
            if (_connected && _notice.Text != null && _notice.Text != _lastNotice)
                SemanticScreenReader.Default.Announce(_notice.Text);
            _lastNotice = _notice.Text;
        }

        RefreshPages();
#if !WINDOWS
        bool showingPopup = ViewModel.SelectedImagePopup != null;
        if (_popupViewer != null)
        {
            _popupViewer.Popup = ViewModel.SelectedImagePopup;
            _popupViewer.IsVisible = showingPopup;
        }
        if (_popupHost != null) _popupHost.IsVisible = showingPopup;
        if (_popupCloseButton != null) _popupCloseButton.IsVisible = showingPopup;
        if (_viewport != null) _viewport.IsVisible = !showingPopup;
        if (_navigation != null) _navigation.IsVisible = !showingPopup;
        if (showingPopup)
        {
            if (_status != null) _status.IsVisible = false;
            if (_busy != null) _busy.IsVisible = false;
            if (_imagePreviousButton != null) _imagePreviousButton.IsVisible = false;
            if (_imageNextButton != null) _imageNextButton.IsVisible = false;
            if (_notice != null) _notice.IsVisible = false;
        }
#endif
        UpdateOpenPanel();
        RefreshDesktopPresentation();
        UpdateBackNavigation();
    }

    private static void SetToolState(Button? button, bool enabled, bool visible)
    {
        if (button == null) return;
        button.IsEnabled = enabled;
        button.IsVisible = visible;
    }

    private void PageSizeChanged(object? sender, EventArgs e) => RefreshPages();

    private void RefreshPages()
    {
#if WINDOWS
        if (_desktopNavigation != null)
        {
            bool show = ViewModel.SelectedImage != null && !ViewModel.IsSequentialNavigationEnabled;
            if (_previousButton != null)
            {
                _previousButton.IsVisible = show && ViewModel.SelectPreviousImageCommand.CanExecute(null);
                _previousButton.IsEnabled = !_searching;
            }
            if (_nextButton != null)
            {
                _nextButton.IsVisible = show && ViewModel.SelectNextImageCommand.CanExecute(null);
                _nextButton.IsEnabled = !_searching;
            }
            return;
        }
#endif
        bool visible = ViewModel.SelectedImage != null;
#if !WINDOWS
        visible &= ViewModel.SelectedImagePopup == null;
#endif
        SetToolState(_imagePreviousButton, !_searching && ViewModel.SelectPreviousImageCommand.CanExecute(null), visible);
        SetToolState(_imageNextButton, !_searching && ViewModel.SelectNextImageCommand.CanExecute(null), visible);
        if (_previousButton != null)
        {
            _previousButton.IsVisible = visible;
            _previousButton.IsEnabled = !_searching && ViewModel.SelectPreviousImageCommand.CanExecute(null);
        }
        if (_nextButton != null)
        {
            _nextButton.IsVisible = visible;
            _nextButton.IsEnabled = !_searching && ViewModel.SelectNextImageCommand.CanExecute(null);
        }
        if (_pages == null) return;
        if (_previousButton != null)
        {
            _previousButton.Text = null;
            _previousButton.WidthRequest = 44;
        }
        if (_nextButton != null)
        {
            _nextButton.Text = null;
            _nextButton.WidthRequest = 44;
            if (_nextButton.ContentLayout?.Position != Button.ButtonContentLayout.ImagePosition.Right)
                _nextButton.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Right, 4);
        }
        if (!visible)
        {
            if (_pages.Children.Count > 0) _pages.Clear();
            _pageState = null;
            return;
        }

        int selected = -1;
        if (!ViewModel.IsSequentialNavigationEnabled)
        {
            for (int i = 0; i < ViewModel.Images.Count; i++)
                if (ViewModel.Images[i] == ViewModel.SelectedImage) selected = i;
        }
        const double pageWidth = 22;
        double available = Width > 0 ? Math.Max(pageWidth, Width - 104) : 7 * pageWidth;
        var (start, count) = ViewModel.IsSequentialNavigationEnabled ? (0, 0) :
            OrientedImageryPagination.GetRange(ViewModel.Images.Count, selected, Math.Min(available, 9 * pageWidth), pageWidth);
        var state = (ViewModel.IsSequentialNavigationEnabled, start, count, ViewModel.Images.Count);
        var previousState = _pageState;
        bool rebuild = previousState == null || previousState.Value.Sequential != ViewModel.IsSequentialNavigationEnabled ||
            previousState.Value.Count != count;
        _pageState = state;
        if (rebuild)
        {
            _pages.Clear();
        }
        if (ViewModel.IsSequentialNavigationEnabled && rebuild)
        {
            Button SequenceButton(bool next)
            {
                var button = ToolButton(next ? "NextLabel" : "PreviousLabel", "", next ? "OrientedImagerySequenceNext" : "OrientedImagerySequencePrevious");
                button.FontSize = 16;
                button.BorderWidth = 0;
                button.Padding = new Thickness(4, 0);
                button.MinimumWidthRequest = 44;
                button.IsEnabled = !_searching && (next ? ViewModel.SelectNextImageCommand : ViewModel.SelectPreviousImageCommand).CanExecute(null);
                button.Clicked += (_, _) => Navigate(next);
                return button;
            }
            _pages.Add(SequenceButton(false));
            var current = new Grid { WidthRequest = 35, HeightRequest = 44 };
            var sequenceLabel = new Label
            {
                Text = "\u00B7", VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center, FontSize = 18,
            };
            sequenceLabel.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
            current.Add(sequenceLabel);
            current.Add(new BoxView
            {
                Color = Color.FromArgb("#007AC2"), HeightRequest = 2,
                VerticalOptions = LayoutOptions.End, Margin = new Thickness(1, 0),
            });
            _pages.Add(current);
            _pages.Add(SequenceButton(true));
        }
        if (ViewModel.IsSequentialNavigationEnabled)
        {
            var previous = (Button)_pages.Children[0];
            previous.Text = Text("PreviousLabel");
            SemanticProperties.SetDescription(previous, previous.Text);
            previous.IsEnabled = !_searching && ViewModel.SelectPreviousImageCommand.CanExecute(null);
            var next = (Button)_pages.Children[2];
            next.Text = Text("NextLabel");
            SemanticProperties.SetDescription(next, next.Text);
            next.IsEnabled = !_searching && ViewModel.SelectNextImageCommand.CanExecute(null);
            return;
        }

        for (int i = start; i < start + count; i++)
        {
            int slot = i - start;
            Button page;
            Ellipse dot;
            if (rebuild)
            {
                page = new Button
                {
                    WidthRequest = pageWidth, MinimumWidthRequest = pageWidth, MinimumHeightRequest = 44, Padding = 0,
                    BackgroundColor = Colors.Transparent, BorderWidth = 0,
                    AutomationId = "OrientedImageryPage" + slot.ToString(CultureInfo.InvariantCulture),
                };
                page.Clicked += (_, _) =>
                {
                    int index = (_pageState?.Start ?? 0) + slot;
                    if (!_searching && !ViewModel.IsSequentialNavigationEnabled && index < ViewModel.Images.Count)
                        ViewModel.SelectedImage = ViewModel.Images[index];
                };
                dot = new Ellipse
                {
                    InputTransparent = true, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                };
                var cell = new Grid { WidthRequest = pageWidth, HeightRequest = 44 };
                cell.Add(page);
                cell.Add(dot);
                _pages.Add(cell);
            }
            else
            {
                var cell = (Grid)_pages.Children[i - start];
                page = (Button)cell.Children[0];
                dot = (Ellipse)cell.Children[1];
            }
            bool isSelected = i == selected;
            bool wasSelected = (previousState?.Start ?? start) + slot == _selectedPage;
            SemanticProperties.SetDescription(page, string.Format(CultureInfo.CurrentCulture, Text("Page"), i + 1, ViewModel.Images.Count));
            SemanticProperties.SetHint(page, Text(isSelected ? "Selected" : "NotSelected"));
            page.IsEnabled = !_searching;
            double diameter = 10;
            if (i != selected)
            {
                int distanceFromTruncatedEnd = Math.Min(start > 0 ? i - start : int.MaxValue,
                    start + count < ViewModel.Images.Count ? start + count - 1 - i : int.MaxValue);
                diameter = distanceFromTruncatedEnd switch { 0 => 4, 1 => 7, _ => 10 };
            }
            dot.WidthRequest = dot.HeightRequest = diameter;
            if (rebuild || wasSelected != isSelected)
            {
                if (isSelected)
                    dot.SetAppThemeColor(Shape.FillProperty, Colors.Black, Colors.White);
                else
                    dot.Fill = Color.FromArgb("#B3B3B3");
            }
        }
        _selectedPage = selected;
    }

    private void Previous(object? sender, EventArgs e) => Navigate(false);
    private void Next(object? sender, EventArgs e) => Navigate(true);
    private void Navigate(bool next)
    {
        var command = next ? ViewModel.SelectNextImageCommand : ViewModel.SelectPreviousImageCommand;
        if (command.CanExecute(null)) command.Execute(null);
        Refresh();
    }

    private sealed record MenuAction(string Title, Action Execute, bool IsChecked = false, bool IsEnabled = true,
        string? Glyph = null, string? Geometry = null, bool IsSelection = false,
        Func<bool>? Enabled = null, Func<bool>? Checked = null, Func<View>? Icon = null, bool IsSubmenu = false);

    private const string CurrentFootprintGeometry = "M28.9869 20.3149L30.0339 26.5899C28.6639 26.3499 26.4439 26.0599 23.7139 26.0599C23.5449 26.0599 23.3899 26.0669 23.2279 26.0699L22.1879 27.1099C22.6649 27.0789 23.1699 27.0599 23.7139 27.0599C28.1639 27.0599 31.2539 27.8599 31.2539 27.8599L29.8559 19.4469L28.9869 20.3149ZM14.0529 27.9879C12.6329 28.3739 11.0459 28.6599 8.79392 28.6599C5.78392 28.6599 3.42392 27.7699 2.36392 27.2899L6.04392 5.07993C7.06392 5.37993 8.58392 5.72993 10.5939 5.72993C13.0939 5.72993 14.5939 5.29993 16.0339 4.87993C17.4839 4.45993 18.8539 4.06993 21.3239 4.06993C23.5139 4.06993 25.4339 4.39993 26.3739 4.58993L28.0719 14.7939L29.1319 15.1019L27.2539 3.75993C27.2539 3.75993 24.5839 3.06993 21.3239 3.06993C16.1839 3.06993 15.4039 4.72993 10.5939 4.72993C7.77392 4.72993 5.95392 3.98993 5.25392 3.75993L1.25392 27.8599C1.25392 27.8599 4.34392 29.6599 8.79392 29.6599C11.1669 29.6599 12.8529 29.3499 14.3309 28.9429L14.0529 27.9879ZM10.7539 9.85993C9.92392 9.85993 9.25392 10.5299 9.25392 11.3599C9.25392 12.1899 9.92392 12.8599 10.7539 12.8599C11.5839 12.8599 12.2539 12.1899 12.2539 11.3599C12.2539 10.5299 11.5839 9.85993 10.7539 9.85993ZM10.7539 11.8599C10.4739 11.8599 10.2539 11.6399 10.2539 11.3599C10.2539 11.0799 10.4739 10.8599 10.7539 10.8599C11.0339 10.8599 11.2539 11.0799 11.2539 11.3599C11.2539 11.6399 11.0339 11.8599 10.7539 11.8599ZM12.9699 12.4929C12.7409 12.939 12.3831 13.3059 11.9429 13.5459L13.0966 17.2199L15.3639 25.3399L16.6639 29.7999L29.2539 17.2199L12.9699 12.4929ZM14.0139 17.0699L12.9839 13.5299L27.3539 17.6999L17.1539 27.8999L14.0139 17.0699Z";

    private const string OverlayGeometry = "M17.1999 13L22 5H0L9 20H13L13.6 19L19 28H23L32 13H17.1999ZM21.8676 26H20.1324L13.5324 15H28.4676L21.8676 26Z";
    private const string LayerGeometry = "M12 1L23 7L12 13L1 7ZM1 12L12 18L23 12M1 17L12 23L23 17";
    private const string SequenceGeometry = "M6 4V8H2V28H25V24H29V4H6ZM24 27H3V9H24V27ZM28 23H25V8H7V5H28V23ZM16.0732 21L19.5859 17.5L16.0876 14H17.5016L21 17.5L17.4872 21H16.0732ZM6.00897 17.5L9.51568 14H10.9297L7.42303 17.5L10.9063 21H9.49225L6.00897 17.5Z";
    private const string ReturnGeometry = "M4 18H3V29H29V3H12V4H28V28H4V18ZM5.66 13.354L1.807 9.5L5.66 5.646L6.367 6.353L3.767 9H11.5C13.7489 8.9923 15.9086 9.87918 17.503 11.4652C19.0974 13.0512 19.9958 15.2061 20 17.455V22.855H19V17.455C18.9956 13.3147 15.6403 9.95941 11.5 9.955H3.675L6.367 12.646L5.66 13.354Z";
    private const string PopupGeometry = "M7 10h18v10H7zm17 9v-8H8v8zM15 7H7v1h8zm9 1h1V7h-1zm5 14a2 2 0 0 1-2 2h-6.69l-4.277 6.33L11.757 24H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h22a2 2 0 0 1 2 2zM28 5a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v17a1 1 0 0 0 1 1h7.289l3.744 5.543L19.778 23H27a1 1 0 0 0 1-1z";

    private static View MenuIcon(string? glyph, string? geometry)
    {
        var accent = Color.FromArgb("#007AC2");
#if __IOS__ && !MACCATALYST
        accent = IosAccent;
#endif
        if (geometry != null)
        {
            var path = new Microsoft.Maui.Controls.Shapes.Path
            {
                Data = (Microsoft.Maui.Controls.Shapes.Geometry)new PathGeometryConverter().ConvertFromInvariantString(geometry),
                Fill = geometry == LayerGeometry ? Colors.Transparent : accent,
                Stroke = geometry == LayerGeometry ? accent : Colors.Transparent,
                StrokeThickness = 1.5, Aspect = Stretch.Uniform,
                WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center,
            };
            return path;
        }
        return new Image
        {
            Source = new FontImageSource { Glyph = glyph ?? ToolkitIcons.Search, FontFamily = ToolkitIcons.FontFamilyName, Size = 22, Color = accent },
            WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center,
        };
    }

    private static Microsoft.Maui.Controls.Shapes.Path OutlineIcon(string geometry)
    {
        var icon = (Microsoft.Maui.Controls.Shapes.Path)MenuIcon(null, geometry);
        icon.Fill = Colors.Transparent;
        icon.Stroke = Color.FromArgb("#007AC2");
        icon.StrokeThickness = 1;
        return icon;
    }

    private static View MarkerOptionIcon(MarkerSymbol symbol) =>
        new SymbolDisplay { Symbol = symbol, WidthRequest = 24, HeightRequest = 24, VerticalOptions = LayoutOptions.Center };

    private View FootprintIcon(bool additional) => MenuIcon(null, additional ? OverlayGeometry : CurrentFootprintGeometry);

    private View CameraLocationsIcon()
    {
        var icon = OutlineIcon("M4 1A3 3 0 1 0 4 7A3 3 0 1 0 4 1ZM20 1A3 3 0 1 0 20 7A3 3 0 1 0 20 1ZM12 9A3 3 0 1 0 12 15A3 3 0 1 0 12 9ZM4 17A3 3 0 1 0 4 23A3 3 0 1 0 4 17ZM20 17A3 3 0 1 0 20 23A3 3 0 1 0 20 17Z");
        return icon;
    }

    private static View FlyoutIcon(View icon)
    {
        icon.HorizontalOptions = LayoutOptions.Center;
        icon.VerticalOptions = LayoutOptions.Center;
        var border = new Border
        {
            Content = icon, WidthRequest = 32, HeightRequest = 32,
            StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 16 },
            InputTransparent = true, VerticalOptions = LayoutOptions.Center,
        };
        border.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#EEEEEE"), Color.FromArgb("#444444"));
        border.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#DDDDDD"), Color.FromArgb("#666666"));
#if __IOS__ && !MACCATALYST
        border.StrokeThickness = 0;
        border.BackgroundColor = Colors.Transparent;
#endif
        return border;
    }

    private bool TogglePanel(View anchor)
    {
        if (_panelAnchor != anchor) return false;
        DismissPanel();
        return true;
    }

    private void UpdateOpenPanel()
    {
        if (_panelAnchor == null || _updatingPanel) return;
        _updatingPanel = true;
        int version = _menuVersion;
        try
        {
            for (int i = 0; i < _panelUpdates.Count; i++)
            {
                _panelUpdates[i]();
                if (version != _menuVersion) return;
            }
        }
        finally
        {
            _updatingPanel = false;
        }
        UpdatePanelSize();
    }

    private void SelectLayer(object? sender, EventArgs e)
    {
        if (sender is View anchor)
        {
            if (TogglePanel(anchor)) return;
            _panelKind = "Layers";
            ShowActions(anchor, Text("SelectLayer"), _layers.Select(layer => new MenuAction(layer.Name, () => SelectedLayer = layer, layer == SelectedLayer,
                Geometry: LayerGeometry, IsSelection: true)).ToArray());
        }
    }

    private void ShowModes(object? sender, EventArgs e)
    {
        if (sender is not View anchor) return;
        if (TogglePanel(anchor)) return;
        _panelKind = "Modes";
        ShowActions(anchor, Text("ModeSelection"), new[]
        {
            new MenuAction(Text("SelectMode"), () => SelectImageMode(false), Glyph: "\uEA74", IsSelection: true, Checked: () => !ViewModel.ImageSearchEnabled),
            new MenuAction(Text("SearchMode"), () => SelectImageMode(true), Glyph: ToolkitIcons.Search, IsSelection: true, Checked: () => ViewModel.ImageSearchEnabled),
        });
    }

    private void SelectImageMode(bool search)
    {
#if !WINDOWS
        ViewModel.SketchModeEnabled = false;
#endif
        ViewModel.ImageSearchEnabled = search;
    }

    private void ShowMarkers(object? sender, EventArgs e)
    {
        if (sender is not View anchor) return;
        if (TogglePanel(anchor)) return;
        OpenMarkerTools(anchor);
    }

    private void OpenMarkerTools(View anchor)
    {
        DismissPanel();
        _panelKind = "Markers";
        var content = PanelContent(Text("MarkerTools"));
        if (HasTool<AllowAddingMarkersVM>())
            AddSwitch(content, "AddMarker", () => ViewModel.SketchModeEnabled, value => ViewModel.SketchModeEnabled = value,
                () => MenuIcon("\uE8ED", null));
        if (ViewModel.ToolbarItems.OfType<SelectNewMarkerSymbolVM>().FirstOrDefault() is { } picker)
        {
            var preview = new SymbolDisplay { Symbol = picker.SelectedSymbol, WidthRequest = 24, HeightRequest = 24, VerticalOptions = LayoutOptions.Center };
            AddActionRow(content, new MenuAction(Text("SelectMarkers"), () => ShowSymbols(anchor),
                Icon: () => preview, IsSubmenu: true, Enabled: () => ViewModel.SketchModeEnabled));
            _panelUpdates.Add(() => preview.Symbol = picker.SelectedSymbol);
            PropertyChangedEventHandler changed = (_, _) => DispatchUpdate(UpdateOpenPanel);
            picker.PropertyChanged += changed;
            _panelCleanup.Add(() => picker.PropertyChanged -= changed);
        }
        if (HasTool<ClearMarkersVM>())
            AddActionRow(content, new MenuAction(Text("ClearMarkers"), () => ViewModel.ClearMarkersCommand.Execute(null),
                Enabled: () => ViewModel.ClearMarkersCommand.CanExecute(null), Glyph: "\uE83E"));
        PresentPanel(anchor, content);
    }

    private void ShowSymbols(View anchor)
    {
        DismissPanel();
        _panelKind = "Symbols";
        var picker = ViewModel.ToolbarItems.OfType<SelectNewMarkerSymbolVM>().FirstOrDefault();
        if (picker == null) return;
        var actions = picker.SymbolOptions.Select((symbol, index) =>
        {
            string? key = symbol is SimpleMarkerSymbol simple ? simple.Style switch
            {
                SimpleMarkerSymbolStyle.Circle => "Circle",
                SimpleMarkerSymbolStyle.Square => "Square",
                SimpleMarkerSymbolStyle.Triangle => "Triangle",
                SimpleMarkerSymbolStyle.Diamond => "Diamond",
                SimpleMarkerSymbolStyle.X => "X",
                _ => null,
            } : null;
            string title = key != null ? Text("Marker" + key) : string.Format(CultureInfo.CurrentCulture, Text("MarkerOption"), index + 1);
            return new MenuAction(title, () => { picker.SelectedSymbol = symbol; OpenMarkerTools(anchor); },
                IsSelection: true, Checked: () => symbol == picker.SelectedSymbol, Enabled: () => ViewModel.SketchModeEnabled,
                Icon: () => MarkerOptionIcon(symbol));
        }).ToArray();
#if WINDOWS
        ShowActions(anchor, Text("MarkerSelection"), actions, back: () => OpenMarkerTools(anchor));
#else
        var content = PanelContent(Text("MarkerSelection"));
        _symbolGrid = new Grid { ColumnSpacing = 4, RowSpacing = 0 };
        foreach (var action in actions)
        {
            var guarded = GuardAction(action);
            var button = new Button
            {
                BackgroundColor = Colors.Transparent, BorderWidth = 0, Padding = 0,
                MinimumWidthRequest = 44, MinimumHeightRequest = 44,
            };
            SemanticProperties.SetDescription(button, action.Title);
            button.Clicked += (_, _) => guarded.Execute();
            var tileContent = new Grid();
            tileContent.Add(button);
            var preview = action.Icon();
            preview.InputTransparent = true;
            preview.HorizontalOptions = LayoutOptions.Center;
            tileContent.Add(preview);
            var tile = new Border
            {
                Content = tileContent, StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Margin = new Thickness(0, 2), MinimumWidthRequest = 48, MinimumHeightRequest = 48,
                BackgroundColor = Color.FromArgb("#F2F2F2"),
            };
            _panelUpdates.Add(() =>
            {
                bool selected = action.Checked();
                tile.Stroke = selected ? Color.FromArgb("#007AC2") : Color.FromArgb("#888888");
                tile.IsEnabled = action.Enabled();
                tile.Opacity = tile.IsEnabled ? 1 : 0.45;
                SemanticProperties.SetHint(button, Text(selected ? "Selected" : "NotSelected"));
            });
            _symbolTiles.Add(tile);
        }
        content.Add(_symbolGrid);
        PresentPanel(anchor, content, back: () => OpenMarkerTools(anchor));
#endif
        PropertyChangedEventHandler changed = (_, _) => DispatchUpdate(UpdateOpenPanel);
        picker.PropertyChanged += changed;
        _panelCleanup.Add(() => picker.PropertyChanged -= changed);
        System.Collections.Specialized.NotifyCollectionChangedEventHandler optionsChanged = (_, _) => DispatchUpdate(() =>
        {
            if (_panelKind == "Symbols") ShowSymbols(anchor);
        });
        picker.SymbolOptions.CollectionChanged += optionsChanged;
        _panelCleanup.Add(() => picker.SymbolOptions.CollectionChanged -= optionsChanged);
    }

    private void ShowMore(object? sender, EventArgs e)
    {
        if (sender is not View anchor) return;
        if (TogglePanel(anchor)) return;
        _panelKind = "More";
        var actions = new List<MenuAction>();
        if (HasTool<SequentialNavigationVM>())
            actions.Add(new MenuAction(Text(ViewModel.IsSequentialNavigationEnabled ? "ReturnToResults" : "SequentialMode"),
                () => ViewModel.ToggleSequentialNavigationCommand.Execute(null),
                Enabled: () => ViewModel.ToggleSequentialNavigationCommand.CanExecute(null),
                Geometry: ViewModel.IsSequentialNavigationEnabled ? ReturnGeometry : SequenceGeometry));
#if WINDOWS
        actions.Add(new MenuAction(Text("Popup"), () => ShowImagePopup(anchor), Enabled: () => ViewModel.SelectedImage != null, Geometry: PopupGeometry));
#endif
        ShowActions(anchor, Text("MoreTools"), actions);
    }

#if WINDOWS
    private void ShowImagePopup(View anchor)
    {
        if (ViewModel.SelectedImage is not { } image) return;
        DismissPanel();
        _panelKind = "Popup";
        var content = PanelContent(Text("Popup"));
        foreach (var attribute in image.Attributes)
        {
            var name = new Label { Text = attribute.Key, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 8, 0, 0) };
            var value = new Label { Text = Convert.ToString(attribute.Value, CultureInfo.CurrentCulture), LineBreakMode = LineBreakMode.WordWrap };
            name.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
            value.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
            content.Add(name);
            content.Add(value);
        }
        if (image.Attributes.Count == 0)
        {
            var empty = new Label { Text = Text("NoAttributes") };
            empty.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
            content.Add(empty);
        }
        PresentPanel(anchor, content);
    }
#endif

    private void ShowActions(View anchor, string title, IReadOnlyList<MenuAction> actions, Action? back = null)
    {
        var kind = _panelKind;
        DismissPanel();
        _panelKind = kind;
        _panelAnchor = anchor;
        UpdateToolHighlights();
        UpdateBackNavigation();
        var guarded = actions.Select(GuardAction).ToArray();
        var content = PanelContent(title);
        foreach (var action in guarded)
            AddActionRow(content, action);
        PresentPanel(anchor, content, back);
    }

    private MenuAction GuardAction(MenuAction action)
    {
        int version = _menuVersion;
        var model = ViewModel;
        var layer = SelectedLayer;
        return action with
        {
            Execute = () =>
            {
                if (version != _menuVersion || !_connected || model != ViewModel || layer != SelectedLayer || !(action.Enabled?.Invoke() ?? action.IsEnabled))
                    return;
                DismissPanel();
                action.Execute();
                Refresh();
            },
        };
    }

#if !WINDOWS
    private void ClosePopup(object? sender, EventArgs e) => ViewModel.CloseSelectedImagePopupCommand.Execute(null);
#endif

    private void AddActionRow(VerticalStackLayout content, MenuAction action)
    {
        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(32), new ColumnDefinition(GridLength.Star), new ColumnDefinition(28) },
            ColumnSpacing = 6, Padding = new Thickness(4, 2), MinimumHeightRequest = 44,
            IsEnabled = action.IsEnabled, Opacity = action.IsEnabled ? 1 : 0.45,
        };
#if !WINDOWS
        row.Padding = new Thickness(4, 0);
#endif
        var button = new Button { BackgroundColor = Colors.Transparent, Padding = 0 };
        SemanticProperties.SetDescription(button, action.Title);
        button.Clicked += (_, _) =>
        {
            if (action.Enabled?.Invoke() ?? action.IsEnabled) action.Execute();
        };
        row.Add(button, 0, 0);
        Grid.SetColumnSpan(button, 3);
        if (action.Glyph != null || action.Geometry != null || action.Icon != null)
        {
            var icon = action.Icon?.Invoke() ?? MenuIcon(action.Glyph, action.Geometry);
            icon.InputTransparent = true;
            row.Add(FlyoutIcon(icon));
        }
        var title = new Label { Text = action.Title, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap, InputTransparent = true };
        title.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
#if __IOS__ && !MACCATALYST
        title.FontSize = 15;
        if (action.Title == Text("ClearMarkers"))
        {
            title.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#FF3B30"), Color.FromArgb("#FF453A"));
            if (row.Children.OfType<Border>().FirstOrDefault()?.Content is Image { Source: FontImageSource clearIcon })
                clearIcon.Color = Color.FromArgb("#FF3B30");
        }
        if (_panelKind == "Layers")
        {
            row.ColumnDefinitions[0].Width = 0;
            row.ColumnSpacing = 0;
            foreach (var icon in row.Children.OfType<Border>())
                icon.IsVisible = false;
        }
#endif
        row.Add(title, 1);
        var selection = new Label { Text = "\u2022", TextColor = Color.FromArgb("#007AC2"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center,
            FontSize = 24, InputTransparent = true };
#if __IOS__ && !MACCATALYST
        selection.Text = "\u2713";
        selection.FontSize = 17;
        selection.TextColor = IosAccent;
#endif
        if (action.IsSelection) row.Add(selection, 2);
        if (action.IsSubmenu)
        {
            var chevron = MenuIcon(ToolkitIcons.ChevronRight, null);
            chevron.InputTransparent = true;
            row.Add(chevron, 2);
        }
        _panelUpdates.Add(() =>
        {
            bool enabled = action.Enabled?.Invoke() ?? action.IsEnabled;
            row.IsEnabled = enabled;
            row.Opacity = enabled ? 1 : 0.45;
            bool selected = action.Checked?.Invoke() ?? action.IsChecked;
            selection.IsVisible = selected;
            if (action.IsSelection) SemanticProperties.SetHint(button, Text(selected ? "Selected" : "NotSelected"));
        });
        AddPanelItem(content, row);
    }

    private void ShowOverlays(object? sender, EventArgs e)
    {
        if (sender is not View anchor) return;
        if (TogglePanel(anchor)) return;
        DismissPanel();
        _panelKind = "Overlays";
        var content = PanelContent(Text("OverlaySelection"));
        if (HasTool<ShowSelectedFootprintVM>()) AddSwitch(content, "MobileCurrentFootprint", () => ViewModel.ShowSelectedFootprint, value => ViewModel.ShowSelectedFootprint = value,
            () => FootprintIcon(false));
        if (HasTool<ShowUnselectedFootprintsVM>()) AddSwitch(content, "MobileAdditionalFootprints", () => ViewModel.ShowUnselectedFootprints, value => ViewModel.ShowUnselectedFootprints = value,
            () => FootprintIcon(true));
        if (HasTool<ShowCameraMarkersVM>()) AddSwitch(content, "MobileCameraLocations", () => ViewModel.ShowCameraLocations, value => ViewModel.ShowCameraLocations = value,
            CameraLocationsIcon);
        if (HasTool<AutoUpdateFootprintVM>()) AddSwitch(content, "AutoUpdate", () => ViewModel.AutoUpdateFootprint, value => ViewModel.AutoUpdateFootprint = value);
#if !WINDOWS
        if (HasTool<ShowSelectedImagePopupVM>())
            AddActionRow(content, GuardAction(new MenuAction(Text("Popup"),
                () => ViewModel.OpenSelectedImagePopupCommand.Execute(null),
                Enabled: () => ViewModel.OpenSelectedImagePopupCommand.CanExecute(null), Geometry: PopupGeometry)));
#endif
        PresentPanel(anchor, content);
    }

    private static VerticalStackLayout PanelContent(string title)
    {
        var content = new VerticalStackLayout { Spacing = 2, Padding = 8 };
#if !WINDOWS
        content.Spacing = 0;
#endif
        var heading = new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 0, 0, 4) };
#if __IOS__ && !MACCATALYST
        heading.FontSize = 15;
        heading.VerticalTextAlignment = TextAlignment.Center;
        content.Spacing = 4;
#endif
        heading.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        content.Add(heading);
        return content;
    }

    private void AddSwitch(VerticalStackLayout content, string key, Func<bool> get, Action<bool> set, Func<View>? icon = null)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(32), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 6 };
        var label = new Label { Text = Text(key), VerticalOptions = LayoutOptions.Center };
        label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        var toggle = new Switch { IsToggled = get(), MinimumHeightRequest = 44, AutomationId = "OrientedImagery" + key };
#if __IOS__ && !MACCATALYST
        row.MinimumHeightRequest = 44;
        label.FontSize = 15;
        label.LineBreakMode = LineBreakMode.WordWrap;
        label.VerticalTextAlignment = TextAlignment.Center;
        toggle.MinimumHeightRequest = -1;
        toggle.VerticalOptions = LayoutOptions.Center;
        toggle.OnColor = Color.FromArgb("#34C759");
#endif
        SemanticProperties.SetDescription(toggle, Text(key));
        toggle.Toggled += (_, e) => { if (!_updatingPanel && _connected && _panelAnchor != null) set(e.Value); };
        if (icon != null)
        {
            var view = FlyoutIcon(icon());
            row.Add(view);
        }
        row.Add(label, 1);
        row.Add(toggle, 2);
        _panelUpdates.Add(() => toggle.IsToggled = get());
        AddPanelItem(content, row);
    }

    private static void AddPanelItem(VerticalStackLayout content, View item)
    {
#if ANDROID
        if (content.Children.Count > 1)
            content.Add(PanelDivider());
#endif
#if __IOS__ && !MACCATALYST
        var group = new Border
        {
            Content = item, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 0), MinimumHeightRequest = 44,
        };
        group.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#598E8E93"), Color.FromArgb("#5948484A"));
        content.Add(group);
#else
        content.Add(item);
#endif
    }

#if ANDROID
    private static BoxView PanelDivider()
    {
        var divider = new BoxView { HeightRequest = 1, InputTransparent = true, AutomationId = "OrientedImageryMenuDivider" };
        divider.SetAppThemeColor(BoxView.ColorProperty, Color.FromArgb("#DDDDDD"), Color.FromArgb("#555555"));
        return divider;
    }
#endif

    private void PresentPanel(View anchor, VerticalStackLayout content, Action? back = null)
    {
        if (_panelHost == null) return;
        _panelAnchor = anchor;
        UpdateToolHighlights();
        UpdateBackNavigation();
        var backdrop = new Grid { BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Fill };
        _panelBackdrop = backdrop;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => DismissPanel();
        backdrop.GestureRecognizers.Add(tap);
        _panelHost.Add(backdrop);
        var heading = (Label)content.Children[0];
        content.Remove(heading);
        content.Padding = 0;
        _panelLayout = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            Padding = 8,
        };
#if !WINDOWS
        _panelLayout.Padding = new Thickness(8, 4);
#endif
        _panelScroll = new ScrollView { Content = content, VerticalOptions = LayoutOptions.Start };
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        header.Add(heading, 1);
        if (back != null)
        {
            var backButton = ToolButton("MobileBackToMarkers", ToolkitIcons.ChevronLeft, "OrientedImageryBackToMarkers");
            backButton.Text = null;
            backButton.WidthRequest = 44;
            backButton.BorderWidth = 0;
            backButton.Clicked += (_, _) => back();
            header.Add(backButton);
        }
#if !WINDOWS
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var closeButton = ToolButton("ClosePanel", "", "OrientedImageryPanelClose");
        closeButton.Text = "X";
#if __IOS__ && !MACCATALYST
        closeButton.Text = "\u00D7";
        closeButton.FontSize = 22;
        closeButton.TextColor = IosAccent;
#endif
        closeButton.WidthRequest = 44;
        closeButton.BorderWidth = 0;
        closeButton.Clicked += (_, _) => DismissPanel();
        header.Add(closeButton, 2);
#endif
        _panelLayout.Add(header);
        _panelLayout.Add(_panelScroll, 0, 1);
        var panel = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            StrokeThickness = 1, Stroke = Colors.Gray,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            MaximumWidthRequest = 320,
            Margin = 4,
        };
#if __IOS__ && !MACCATALYST
        panel.BackgroundColor = Colors.Transparent;
        panel.StrokeThickness = 0;
        _panelLayout.Padding = new Thickness(10, 4, 10, 10);
        var surface = new Grid();
        surface.Add(new IosPanelMaterial());
        surface.Add(_panelLayout);
        panel.Content = surface;
#else
        panel.Content = _panelLayout;
        panel.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#303030"));
#endif
        _panel = panel;
        _panelHost.Add(panel);
        _panelHost.IsVisible = true;
        UpdateOpenPanel();
        SemanticScreenReader.Default.Announce(heading.Text);
    }

    private void PanelHostSizeChanged(object? sender, EventArgs e) => UpdatePanelSize();

    private void UpdatePanelSize()
    {
        if (_panelHost == null || _panelLayout == null || _panelScroll == null || _panel == null || _panelAnchor == null) return;
        double x = 0, y = _panelAnchor.Height;
        Element? element = _panelAnchor;
        while (element is VisualElement visual && element != _panelHost.Parent)
        {
            x += visual.X;
            y += visual.Y;
            element = visual.Parent;
        }
        double maximumWidth = Math.Max(0, Math.Min(320, _panelHost.Width - 8));
#if !WINDOWS
        if (_symbolGrid != null)
            LayoutSymbolGrid(Math.Max(0, maximumWidth - _panelLayout.Padding.HorizontalThickness - 2 * _panel.StrokeThickness));
#endif
        double contentWidth = _panelLayout.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
        double panelWidth = Math.Min(maximumWidth, contentWidth + 2 * _panel.StrokeThickness);
        _panel.WidthRequest = panelWidth;
        x = Math.Clamp(x, 4, Math.Max(4, _panelHost.Width - panelWidth - 4));
        _panel.Margin = new Thickness(x, y, 4, 4);
        if (_panelBackdrop != null)
        {
            double toolbarBottom = _tools is null ? y : _tools.Y + _tools.Height;
            _panelBackdrop.Margin = new Thickness(0, toolbarBottom, 0, 0);
        }
        double width = Math.Max(0, panelWidth - _panelLayout.Padding.HorizontalThickness - 2 * _panel.StrokeThickness);
        double headingHeight = _panelLayout.Children[0].Measure(width, double.PositiveInfinity).Height;
        _panelScroll.MaximumHeightRequest = Math.Max(0,
            _panelHost.Height - y - 4 - _panelLayout.Padding.VerticalThickness - headingHeight);
    }

#if !WINDOWS
    private void LayoutSymbolGrid(double availableWidth)
    {
        if (_symbolGrid == null) return;
        double width = Math.Min(224, availableWidth);
        _symbolGrid.WidthRequest = width;
        int columns = Math.Max(1, Math.Min(4, (int)((width + _symbolGrid.ColumnSpacing) / 56)));
        if (_symbolGrid.ColumnDefinitions.Count == columns && _symbolGrid.Children.Count > 0) return;
        _symbolGrid.Clear();
        _symbolGrid.ColumnDefinitions.Clear();
        _symbolGrid.RowDefinitions.Clear();
        for (int i = 0; i < columns; i++)
            _symbolGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int i = 0; i < _symbolTiles.Count; i++)
        {
            int row = i / columns;
#if ANDROID
            row *= 2;
#endif
            if (i % columns == 0)
            {
#if ANDROID
                if (i > 0)
                {
                    _symbolGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var divider = PanelDivider();
                    _symbolGrid.Add(divider, 0, row - 1);
                    Grid.SetColumnSpan(divider, columns);
                }
#endif
                _symbolGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
            _symbolGrid.Add(_symbolTiles[i], i % columns, row);
        }
    }
#endif

    private bool DismissTools()
    {
        bool open = _panelAnchor != null;
        DismissPanel();
#if !WINDOWS
        if (!open && ViewModel.SelectedImagePopup != null)
        {
            ViewModel.CloseSelectedImagePopupCommand.Execute(null);
            return true;
        }
#endif
        return open;
    }

    private void DismissPanel()
    {
#if __IOS__ && !MACCATALYST
        if (_panel?.Content is Grid surface)
            surface.Children.OfType<IosPanelMaterial>().FirstOrDefault()?.Dispose();
#endif
        _menuVersion++;
        foreach (var cleanup in _panelCleanup) cleanup();
        _panelCleanup.Clear();
        _panelUpdates.Clear();
        _panelKind = null;
        _panel = null;
        _panelBackdrop = null;
        _panelScroll = null;
        _panelLayout = null;
#if !WINDOWS
        _symbolGrid = null;
        _symbolTiles.Clear();
#endif
        if (_panelHost != null)
        {
            _panelHost.IsVisible = false;
            _panelHost.Clear();
        }
        var anchor = _panelAnchor;
        _panelAnchor = null;
        UpdateToolHighlights();
        UpdateBackNavigation();
        anchor?.Focus();
    }

    private readonly Dictionary<Button, bool> _toolHighlights = new();

    private void UpdateToolHighlights()
    {
        if (_overlaysIcon != null && _overlaysButton != null)
        {
            _overlaysIcon.IsVisible = _overlaysButton.IsVisible;
            _overlaysIcon.Opacity = _overlaysButton.IsEnabled ? 1 : 0.45;
            _overlaysIcon.Fill = _overlaysButton == _panelAnchor ? Colors.White : Color.FromArgb("#007AC2");
#if __IOS__ && !MACCATALYST
            _overlaysIcon.Fill = _overlaysButton == _panelAnchor ? Colors.White : IosAccent;
#endif
        }
        foreach (var button in _toolButtons)
        {
            if (button == null) continue;
            bool active = button == _panelAnchor || (button == _markersButton && ViewModel.SketchModeEnabled) ||
                (button == _moreButton && ViewModel.IsSequentialNavigationEnabled);
            bool changed = !_toolHighlights.TryGetValue(button, out bool previous) || previous != active;
            _toolHighlights[button] = active;
            if (changed)
            {
                button.BackgroundColor = active ? Color.FromArgb("#007AC2") : Colors.Transparent;
                if (active)
                    button.TextColor = Colors.White;
                else
                    button.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
                if (button.ImageSource is FontImageSource icon)
                    icon.Color = active ? Colors.White : Color.FromArgb("#007AC2");
            }
#if !WINDOWS
            if (button.Parent is Grid cell && cell.Children.OfType<Border>().FirstOrDefault() is { Content: Layout content } pill)
            {
                pill.Opacity = button.IsEnabled ? 1 : 0.45;
                if (!changed) continue;
                pill.BackgroundColor = active ? Color.FromArgb("#007AC2") : Colors.Transparent;
                button.BackgroundColor = Colors.Transparent;
                foreach (var label in content.Children.OfType<Label>())
                {
                    if (label == content.Children[0])
                        label.TextColor = active ? Colors.White : Color.FromArgb("#007AC2");
                    else if (active)
                        label.TextColor = Colors.White;
                    else
                        label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
                }
                if (content.Children[0] is Image { Source: FontImageSource mobileIcon })
                    mobileIcon.Color = active ? Colors.White : Color.FromArgb("#007AC2");
#if __IOS__ && !MACCATALYST
                if (active)
                    pill.BackgroundColor = IosAccent;
                else
                    pill.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#3A3A3C"));
                foreach (var label in content.Children.OfType<Label>())
                    label.TextColor = active ? Colors.White : IosAccent;
                if (content.Children[0] is Image { Source: FontImageSource iosIcon })
                    iosIcon.Color = active ? Colors.White : IosAccent;
#endif
            }
#endif
        }
        if (_modeButton != null)
            SemanticProperties.SetHint(_modeButton, Text(ViewModel.ImageSearchEnabled ? "SearchMode" : "SelectMode"));
        if (_markersButton != null)
            SemanticProperties.SetHint(_markersButton, Text(ViewModel.SketchModeEnabled ? "On" : "Off"));
    }

    partial void RefreshDesktopPresentation();
    partial void AttachPlatformPresentation();
    partial void DetachPlatformPresentation();
    partial void AttachBackNavigation();
    partial void DetachBackNavigation();
    partial void UpdateBackNavigation();
}
