using System.ComponentModel;
using System.Globalization;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    #region Presentation state

    private const double MinimumImageAreaHeight = 200;
    private Label? _status;
    private View? _statusHost;
    private View? _selectionGuidance;
    private Label? _selectionInstruction;
    private Label? _selectionPrefix;
    private View? _selectionFindIcon;
    private View? _selectionOr;
    private View? _selectionDisplayIcon;
    private Label? _notice;
    private ActivityIndicator? _busy;
    private Button? _previousButton;
    private Button? _nextButton;
    private Button? _imagePreviousButton;
    private Button? _imageNextButton;
#if !WINDOWS
    private Grid? _panelLayout;
    private ScrollView? _panelScroll;
#endif
    private HorizontalStackLayout? _pages;
    private Layout? _customToolbar;
    private (bool Sequential, int Start, int Count)? _pageState;
    private int _selectedPage = -1;
    private View? _panelAnchor;
    private int _menuVersion;
    private string? _lastStatusAnnouncement;
    private string? _lastNotice;
    private readonly List<Action> _panelUpdates = new();
    private readonly List<Action> _panelCleanup = new();
#if !WINDOWS && !ANDROID && (!__IOS__ || MACCATALYST)
    private Grid? _panelHost;
    private ScrollView? _toolbarScroll;
    private Border? _panel;
    private Grid? _panelBackdrop;
#endif
    private bool _updatingPanel;
    private bool _refreshing;
    private bool _refreshPending;
#if !WINDOWS
    private Page? _presentationPage;
    private bool? _pageLandscape;
#endif
    private PopupViewer? _popupViewer;
    private Grid? _popupHost;
    private Button? _popupOpenButton;
    private Button? _popupCloseButton;
#if !WINDOWS
    private Grid? _symbolGrid;
    private readonly List<Border> _symbolTiles = new();
#endif

    #endregion

    #region Labels and template construction

    private static string Text(string key) => Properties.Resources.GetString("OrientedImageryView" + key);

    internal static string MarkerOptionName(MarkerSymbol symbol, int index)
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
        return key != null ? Text("Marker" + key) : string.Format(CultureInfo.CurrentCulture, Text("MarkerOption"), index + 1);
    }

    private static readonly ControlTemplate DefaultControlTemplate = new(() =>
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            RowSpacing = 0,
        };
#if __IOS__ && !MACCATALYST
        root.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#F2F2F7"), Color.FromArgb("#1C1C1E"));
#else
        root.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
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

#if WINDOWS
        Add("PART_CustomToolbar", new ToolbarWrapLayout { Margin = 8, CenterRows = true }, root);
#else
        var custom = new ToolbarRowLayout();
        var toolbarScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal, Content = custom,
            Margin = new Thickness(8, 0), HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
            AutomationId = "OrientedImageryToolbarScroll",
        };
        toolbarScroll.SizeChanged += (_, _) => custom.MinimumWidthRequest = Math.Max(0, toolbarScroll.Width);
        scope.RegisterName("PART_CustomToolbar", custom);
        Add("PART_ToolbarScroll", toolbarScroll, root);
#endif

        var image = new Grid
        {
#if !ANDROID
            MinimumHeightRequest = MinimumImageAreaHeight,
#endif
        };
        Grid.SetRow(image, 1);
        root.Children.Add(image);
        Add("PART_Viewport", new OrientedImageViewport { AutomationId = "OrientedImageryViewport" }, image);
        AddStatusPresentation(image, scope);
#if WINDOWS
        Add("PART_Previous", DesktopImageArrow(false), image);
        Add("PART_Next", DesktopImageArrow(true), image);
#else
        Add("PART_ImagePrevious", MobileImageArrow(false), image);
        Add("PART_ImageNext", MobileImageArrow(true), image);
#endif
        AddPopupPresentation(image, scope);

#if WINDOWS
        Add("PART_DesktopNavigation", new OrientedImageryControlsView(), root, 2);
#else
        var pagination = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
            HorizontalOptions = LayoutOptions.Center,
            ColumnSpacing = 0,
            Padding = new Thickness(8, 0),
        };
        scope.RegisterName("PART_Navigation", pagination);
        var paginationScroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = pagination };
        Grid.SetRow(paginationScroll, 2);
        root.Add(paginationScroll);
        var previous = ToolButton("PreviousLabel", ToolkitIcons.ChevronLeft, "OrientedImageryPrevious");
        previous.Text = null;
        previous.WidthRequest = 44;
        previous.BorderWidth = 0;
        Add("PART_Previous", previous, pagination);
        Add("PART_Pages", new HorizontalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Spacing = 0 }, pagination, column: 1);
        var next = ToolButton("NextLabel", ToolkitIcons.ChevronRight, "OrientedImageryNext");
        next.Text = null;
        next.WidthRequest = 44;
        next.BorderWidth = 0;
        Add("PART_Next", next, pagination, column: 2);
#endif
        var notice = new Label { HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(8, 4), AutomationId = "OrientedImageryNotice" };
        notice.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        Add("PART_Notice", notice, root, 3);
#if !WINDOWS && !ANDROID && (!__IOS__ || MACCATALYST)
        var panels = new Grid { IsVisible = false, InputTransparent = true, CascadeInputTransparent = false };
        Add("PART_PanelHost", panels, root);
        Grid.SetRowSpan(panels, root.RowDefinitions.Count);
#endif
#if ANDROID
        return root;
#else
        return ScrollablePresentation(root, image, scope);
#endif
    });

    private static View ScrollablePresentation(Grid root, Grid image, INameScope scope)
    {
        // Neither measures content with infinite width on Windows, allowing popup fields to expand the entire viewer.
        var scroll = new ScrollView { Content = root, Orientation = ScrollOrientation.Vertical };
        NameScope.SetNameScope(scroll, scope);
        var chrome = root.Children.OfType<View>().Where(view => Grid.GetRow(view) != 1 && Grid.GetRowSpan(view) == 1).ToArray();
        void UpdateSize(object? sender, EventArgs e)
        {
            if (scroll.Width <= 0 || scroll.Height <= 0) return;
            double chromeHeight = chrome.Where(view => view.IsVisible)
                .Sum(view => view.Measure(scroll.Width, double.PositiveInfinity).Height);
            // Keep the image bounded even when a popup contains a long list of fields.
            root.HeightRequest = Math.Max(scroll.Height, chromeHeight + image.MinimumHeightRequest);
#if !WINDOWS
            scroll.Orientation = root.HeightRequest > scroll.Height ? ScrollOrientation.Vertical : ScrollOrientation.Neither;
#endif
        }
        scroll.SizeChanged += UpdateSize;
        foreach (var view in chrome)
            view.SizeChanged += UpdateSize;
        return scroll;
    }

    private static void AddPopupPresentation(Grid image, INameScope scope)
    {
        var open = ToolButton("OpenSelectedImagePopup", ToolkitIcons.MoreInformation, "OrientedImageryPopupOpen");
        open.Text = null;
        open.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 0);
        if (open.ImageSource is FontImageSource infoIcon)
        {
            infoIcon.Size = 30;
            infoIcon.SetAppThemeColor(FontImageSource.ColorProperty, Color.FromArgb("#007AC2"), Color.FromArgb("#6CB8EC"));
        }
        open.WidthRequest = 44;
        open.HeightRequest = 44;
        open.BorderWidth = 0;
        open.CornerRadius = 22;
        open.Margin = 16;
        open.Padding = 0;
        open.HorizontalOptions = LayoutOptions.End;
        open.VerticalOptions = LayoutOptions.Start;
        open.IsVisible = false;
        open.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        ToolTipProperties.SetText(open, Text("OpenSelectedImagePopup"));
        scope.RegisterName("PART_PopupOpen", open);
        image.Add(open);
        Grid.SetColumnSpan(open, image.ColumnDefinitions.Count == 0 ? 1 : image.ColumnDefinitions.Count);

#if ANDROID || (__IOS__ && !MACCATALYST)
        scope.RegisterName("PART_Popup", new PopupViewer { AutomationId = "OrientedImageryPopupViewer" });
#else
        var host = new Grid
        {
            IsVisible = false,
            InputTransparent = true,
            CascadeInputTransparent = false,
            ColumnSpacing = 0,
            ColumnDefinitions = { new ColumnDefinition(new GridLength(0.20, GridUnitType.Star)), new ColumnDefinition(new GridLength(0.80, GridUnitType.Star)) },
        };
        scope.RegisterName("PART_PopupHost", host);
        image.Add(host);
        Grid.SetColumnSpan(host, image.ColumnDefinitions.Count == 0 ? 1 : image.ColumnDefinitions.Count);

        var surface = new Grid
        {
            ColumnSpacing = 0,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        };
        surface.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        var close = ToolButton("CloseSelectedImagePopup", "\uEB49", "OrientedImageryPopupClose");
        close.Text = null;
        close.WidthRequest = 44;
        close.HeightRequest = 44;
        close.MinimumWidthRequest = 44;
        close.MinimumHeightRequest = 44;
        close.CornerRadius = 12;
        close.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 0);
        if (close.ImageSource is FontImageSource closeIcon)
        {
            closeIcon.Size = 16;
            closeIcon.SetAppThemeColor(FontImageSource.ColorProperty, Color.FromArgb("#007AC2"), Color.FromArgb("#6CB8EC"));
        }
        close.Padding = 0;
        close.HorizontalOptions = LayoutOptions.End;
        close.VerticalOptions = LayoutOptions.Start;
        close.Margin = new Thickness(0, 4, 4, 0);
        close.BorderWidth = 0;
        close.BackgroundColor = Colors.Transparent;
        ToolTipProperties.SetText(close, Text("CloseSelectedImagePopup"));
        scope.RegisterName("PART_PopupClose", close);
        surface.Add(close, 1);
        var popup = new PopupViewer
        {
            AutomationId = "OrientedImageryPopupViewer",
            Padding = 0,
            Margin = new Thickness(16, 0, 8, 8),
        };
        scope.RegisterName("PART_Popup", popup);
        surface.Add(popup);
        var border = new Border { Content = surface, StrokeThickness = 1 };
        border.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#DDDDDD"), Color.FromArgb("#444444"));
        host.Add(border, 1);
#endif
    }

    private static void AddStatusPresentation(Grid image, INameScope scope)
    {
        var status = new Label
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.WordWrap,
            AutomationId = "OrientedImageryStatus", FontSize = 18,
        };
        status.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#666666"), Color.FromArgb("#AAAAAA"));
        var choices = new FlexLayout
        {
            Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustify.Center, AlignItems = FlexAlignItems.Center,
        };
        var prefix = new Label { Text = Text("ImageSelectionUsing"), FontSize = 18 };
        var findIcon = ImageModeIcon(true);
        var or = new Label { Text = Text("ImageSelectionOr"), FontSize = 18 };
        var displayIcon = ImageModeIcon(false);
        foreach (var view in new View[]
        {
            prefix, findIcon, or, displayIcon,
            new Label { Text = Text("ImageSelectionMode"), FontSize = 18 },
        })
        {
            view.Margin = new Thickness(4, 0);
            AutomationProperties.SetIsInAccessibleTree(view, false);
            if (view is Label label)
                label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#666666"), Color.FromArgb("#AAAAAA"));
            else
            {
                view.WidthRequest = view.HeightRequest = 24;
                if (view is Microsoft.Maui.Controls.Shapes.Path path)
                    path.SetAppThemeColor(Microsoft.Maui.Controls.Shapes.Shape.FillProperty, Color.FromArgb("#666666"), Color.FromArgb("#AAAAAA"));
            }
            choices.Add(view);
        }
        var searchMap = new Label
        {
            Text = Text("ImageSelectionSearchMap"), FontSize = 18, HorizontalTextAlignment = TextAlignment.Center,
        };
        searchMap.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#666666"), Color.FromArgb("#AAAAAA"));
        var guidance = new VerticalStackLayout { Spacing = 6, Children = { searchMap, choices } };
        SemanticProperties.SetDescription(guidance, Text("ImageSelectionInstructions"));
        AutomationProperties.SetIsInAccessibleTree(guidance, true);
        AutomationProperties.SetIsInAccessibleTree(choices, false);
        AutomationProperties.SetIsInAccessibleTree(searchMap, false);
        var content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 400, Margin = 16, Spacing = 6, Children = { status, guidance },
        };
        var host = new Grid();
        host.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        host.Add(new ScrollView { Content = content, VerticalOptions = LayoutOptions.Center });
        scope.RegisterName("PART_Status", status);
        scope.RegisterName("PART_SelectionGuidance", guidance);
        scope.RegisterName("PART_SelectionInstruction", searchMap);
        scope.RegisterName("PART_SelectionPrefix", prefix);
        scope.RegisterName("PART_SelectionFindIcon", findIcon);
        scope.RegisterName("PART_SelectionOr", or);
        scope.RegisterName("PART_SelectionDisplayIcon", displayIcon);
        scope.RegisterName("PART_StatusHost", host);
        image.Add(host);
        Grid.SetColumnSpan(host, Math.Max(1, image.ColumnDefinitions.Count));
    }

#if !WINDOWS
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
#endif

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

    #endregion

    #region Template and handler lifecycle

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        UnwireTemplate();
        base.OnApplyTemplate();
        _viewport = GetTemplateChild("PART_Viewport") as OrientedImageViewport;
        _popupViewer = GetTemplateChild("PART_Popup") as PopupViewer;
        _popupHost = GetTemplateChild("PART_PopupHost") as Grid;
        _popupOpenButton = GetTemplateChild("PART_PopupOpen") as Button;
        _popupCloseButton = GetTemplateChild("PART_PopupClose") as Button;
        _status = GetTemplateChild("PART_Status") as Label;
        _statusHost = GetTemplateChild("PART_StatusHost") as View;
        _selectionGuidance = GetTemplateChild("PART_SelectionGuidance") as View;
        _selectionInstruction = GetTemplateChild("PART_SelectionInstruction") as Label;
        _selectionPrefix = GetTemplateChild("PART_SelectionPrefix") as Label;
        _selectionFindIcon = GetTemplateChild("PART_SelectionFindIcon") as View;
        _selectionOr = GetTemplateChild("PART_SelectionOr") as View;
        _selectionDisplayIcon = GetTemplateChild("PART_SelectionDisplayIcon") as View;
        _notice = GetTemplateChild("PART_Notice") as Label;
        _busy = GetTemplateChild("PART_Busy") as ActivityIndicator;
        _previousButton = GetTemplateChild("PART_Previous") as Button;
        _nextButton = GetTemplateChild("PART_Next") as Button;
        _imagePreviousButton = GetTemplateChild("PART_ImagePrevious") as Button;
        _imageNextButton = GetTemplateChild("PART_ImageNext") as Button;
        _pages = GetTemplateChild("PART_Pages") as HorizontalStackLayout;
#if !WINDOWS && !ANDROID && (!__IOS__ || MACCATALYST)
        _panelHost = GetTemplateChild("PART_PanelHost") as Grid;
        if (_panelHost != null) _panelHost.SizeChanged += PanelHostSizeChanged;
        _toolbarScroll = GetTemplateChild("PART_ToolbarScroll") as ScrollView;
        if (_toolbarScroll != null) _toolbarScroll.Scrolled += ToolbarScrolled;
#endif
        _customToolbar = GetTemplateChild("PART_CustomToolbar") as Layout;
#if WINDOWS
        InitializeDesktopTemplate();
#endif
        if (_viewport != null)
        {
            _viewport.ImageTapped += ImageTapped;
            _viewport.StateChanged += ViewportChanged;
        }

        if (_previousButton != null)
        {
            _previousButton.Clicked += Previous;
            _previousButton.HandlerChanged += PaginationButtonHandlerChanged;
        }
        if (_nextButton != null)
        {
            _nextButton.Clicked += Next;
            _nextButton.HandlerChanged += PaginationButtonHandlerChanged;
        }
        if (_imagePreviousButton != null) _imagePreviousButton.Clicked += Previous;
        if (_imageNextButton != null) _imageNextButton.Clicked += Next;
        if (_pages != null) _pages.SizeChanged += PageSizeChanged;
        Refresh();
        _ = LookAtSearchPointAsync();
    }

    /// <inheritdoc/>
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        DetachPlatformPresentation();
        ReleaseToolbarPresentation();
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
#if ANDROID
        RefreshAndroidPopup();
#elif __IOS__ && !MACCATALYST
        RefreshIosPopup();
#endif
    }

    private void PresentationUnloaded(object? sender, EventArgs e)
    {
        DetachPlatformPresentation();
        SetPresentationPage(null);
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
#if __IOS__ && !MACCATALYST
            UpdateIosPanelSize();
#else
            DismissTools();
#endif
        _pageLandscape = landscape;
    }
#endif

    private void UnwireTemplate()
    {
#if WINDOWS
        ReleaseDesktopTemplate();
#endif
        DismissPanel();
#if ANDROID
        DismissAndroidPopup();
#elif __IOS__ && !MACCATALYST
        DismissIosPopup();
#endif
        ReleaseToolbarPresentation();
#if !WINDOWS && !ANDROID && (!__IOS__ || MACCATALYST)
        if (_panelHost != null) _panelHost.SizeChanged -= PanelHostSizeChanged;
        if (_toolbarScroll != null) _toolbarScroll.Scrolled -= ToolbarScrolled;
        _toolbarScroll = null;
        _panelHost = null;
#endif
        if (_viewport != null)
        {
            _viewport.ImageTapped -= ImageTapped;
            _viewport.StateChanged -= ViewportChanged;
            _viewport.Footprint = null;
            _viewport.Markers = null;
        }

        if (_previousButton != null)
        {
            _previousButton.Clicked -= Previous;
            _previousButton.HandlerChanged -= PaginationButtonHandlerChanged;
        }
        if (_nextButton != null)
        {
            _nextButton.Clicked -= Next;
            _nextButton.HandlerChanged -= PaginationButtonHandlerChanged;
        }
        if (_imagePreviousButton != null) _imagePreviousButton.Clicked -= Previous;
        if (_imageNextButton != null) _imageNextButton.Clicked -= Next;
        if (_pages != null) _pages.SizeChanged -= PageSizeChanged;
        _viewport = null;
        _status = null;
        _statusHost = null;
        _selectionGuidance = null;
        _selectionInstruction = null;
        _selectionPrefix = null;
        _selectionFindIcon = null;
        _selectionOr = null;
        _selectionDisplayIcon = null;
        _notice = null;
        _busy = null;
        _previousButton = null;
        _nextButton = null;
        _imagePreviousButton = null;
        _imageNextButton = null;
        _pages?.Clear();
        _pages = null;
        _pageState = null;
        _selectedPage = -1;
        _customToolbar = null;
        if (_popupViewer != null) _popupViewer.Popup = null;
        if (_popupOpenButton != null) _popupOpenButton.Command = null;
        if (_popupCloseButton != null) _popupCloseButton.Command = null;
        _popupOpenButton = null;
        _popupCloseButton = null;
        _popupHost = null;
        _popupViewer = null;
    }

    private void ViewportChanged(object? sender, EventArgs e) => Refresh();

    #endregion

    #region Status and presentation updates

    private string RefreshSelectionGuidance()
    {
        bool find = ViewModel.InteractionState == OrientedImageryViewInteractionState.FindImages;
        bool display = ViewModel.InteractionState == OrientedImageryViewInteractionState.DisplayImage;
        if (_selectionInstruction != null)
            _selectionInstruction.Text = Text(find ? "ImageSelectionFindMap" :
                display ? "ImageSelectionDisplayFeature" : "ImageSelectionSearchMap");
        if (_selectionPrefix != null)
            _selectionPrefix.Text = Text(find || display ? "ImageSelectionSwitchMode" : "ImageSelectionUsing");
        if (_selectionFindIcon != null) _selectionFindIcon.IsVisible = !find;
        if (_selectionOr != null) _selectionOr.IsVisible = !find && !display;
        if (_selectionDisplayIcon != null) _selectionDisplayIcon.IsVisible = !display;
        string description = Text(find ? "ImageSelectionFindInstructions" :
            display ? "ImageSelectionDisplayInstructions" : "ImageSelectionInstructions");
        if (_selectionGuidance != null)
            SemanticProperties.SetDescription(_selectionGuidance, description);
        return description;
    }

    private void Refresh()
    {
        // Model changes during a refresh request another pass rather than nesting refreshes.
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
        _imageSearchEnabled = ImageSearchEnabled;
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

        bool footprintsEnabled = _connected && _viewport?.Footprint != null && _viewport.IsInteractive;
        if (ViewModel.AreFootprintsEnabled != footprintsEnabled)
            ViewModel.AreFootprintsEnabled = footprintsEnabled;

        RefreshStatus();
        RefreshToolbarPresentation();
        if (_notice != null)
        {
            _notice.Text = ViewModel.SequentialNavigationError?.Message;
            _notice.IsVisible = _notice.Text != null;
        }

        RefreshPages();
        RefreshPopup();
        UpdateOpenPanel();
        RefreshDesktopPresentation();
        UpdateBackNavigation();
    }

    private void RefreshStatus()
    {
        bool loading = _searching || _viewport?.IsBusy == true || SelectedLayer?.LoadStatus == LoadStatus.Loading;
        if (_busy != null)
        {
            _busy.IsRunning = loading;
            _busy.IsVisible = loading;
        }

        string? error = _operationError?.Message ?? _viewport?.Error?.Message;
        string? message = error;
        bool showSelectionGuidance = false;
        if (message == null)
        {
            if (GeoView == null) message = Text("NoGeoView");
            else if (_layers.Count == 0) message = Text("NoLayers");
            else if (SelectedLayer == null) message = Text("NoLayerSelected");
            else if (!_layers.Contains(SelectedLayer)) message = Text("InvalidLayer");
            else if (loading) message = Text("Loading");
            else if (ViewModel.SelectedImage == null)
            {
                message = Text(_hasSearchedForImages ? "NoImagesFound" : "NoImageToDisplay");
                showSelectionGuidance = !_hasSearchedForImages;
            }
        }
        string selectionInstructions = RefreshSelectionGuidance();
        ErrorMessage = error ?? ViewModel.SequentialNavigationError?.Message ??
            (showSelectionGuidance ? $"{message}. {selectionInstructions}" : message);

        if (_status != null)
        {
            _status.Text = message;
            _status.IsVisible = message != null;
        }
        if (_statusHost != null)
            _statusHost.IsVisible = message != null;
        if (_selectionGuidance != null)
            _selectionGuidance.IsVisible = showSelectionGuidance;
        AnnounceStatusMessages(showSelectionGuidance ? $"{message}. {selectionInstructions}" : message,
            ViewModel.SequentialNavigationError?.Message, error == null && message == Text("Loading"));
    }

    private bool CanAnnounce
    {
        get
        {
            if (!_connected || !IsLoaded) return false;
            for (Element? element = this; element != null; element = element.Parent)
                if (element is VisualElement { IsVisible: false }) return false;
            return true;
        }
    }

    private void AnnounceStatusMessages(string? status, string? notice, bool loading)
    {
        if (!CanAnnounce) return;
        string? announcement = null;
        if (!loading)
        {
            if (status != null && status != _lastStatusAnnouncement)
                announcement = status;
            _lastStatusAnnouncement = status;
        }
        if (notice != null && notice != _lastNotice && notice != status)
            announcement = announcement == null ? notice : $"{announcement}. {notice}";
        _lastNotice = notice;
        if (announcement != null)
            SemanticScreenReader.Default.Announce(announcement);
    }

    #endregion

    #region Image information

    private void RefreshPopup()
    {
        bool showingPopup = ViewModel.SelectedImagePopup != null;
#if WINDOWS
        bool focusPopup = showingPopup && _popupHost?.IsVisible == false &&
            _popupOpenButton?.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button
            { FocusState: Microsoft.UI.Xaml.FocusState.Keyboard };
#endif
        if (_popupOpenButton != null)
        {
            _popupOpenButton.Command = ViewModel.OpenSelectedImagePopupCommand;
            _popupOpenButton.IsVisible = ViewModel.SelectedImage != null && !showingPopup;
        }
#if ANDROID
        RefreshAndroidPopup();
#elif __IOS__ && !MACCATALYST
        RefreshIosPopup();
#endif
        if (_popupViewer != null)
        {
            _popupViewer.Popup = ViewModel.SelectedImagePopup;
            _popupViewer.IsVisible = showingPopup;
        }
        if (_popupHost != null) _popupHost.IsVisible = showingPopup;
        if (_popupCloseButton != null) _popupCloseButton.Command = ViewModel.CloseSelectedImagePopupCommand;
#if WINDOWS
        if (focusPopup) FocusDesktopPopup();
#endif
    }

    #endregion

    #region Image pagination

    internal string ImagePageActionKey(bool next) => ViewModel.IsSequentialNavigationEnabled
        ? next ? "ShowNextImageInSequence" : "ShowPreviousImageInSequence"
        : next ? "ShowNextImage" : "ShowPreviousImage";

    internal bool HasImageOverlayArrow(bool next) =>
#if WINDOWS
        (next ? _nextButton : _previousButton) is { IsVisible: true };
#else
        (next ? _imageNextButton : _imagePreviousButton) is { IsVisible: true };
#endif

    private void SetImagePageButtonState(Button? button, bool next, bool enabled, bool visible)
    {
        if (button == null) return;
        button.IsEnabled = enabled;
        button.IsVisible = visible;
        string name = Text(ImagePageActionKey(next));
        SemanticProperties.SetDescription(button, name);
        ToolTipProperties.SetText(button, name);
    }

    private void PaginationButtonHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is Button button)
            SetPaginationTabStop(button, button == _nextButton);
    }

    private void SetPaginationTabStop(Button button, bool next)
    {
#if ANDROID
        // Keyboard focus is separate from TalkBack accessibility focus.
        if (button.Handler?.PlatformView is Android.Views.View platformButton)
            platformButton.Focusable = !HasImageOverlayArrow(next) &&
                (button == (next ? _nextButton : _previousButton) ||
                    (next ? _nextButton : _previousButton) is not { IsVisible: true });
#endif
    }

    private void PageSizeChanged(object? sender, EventArgs e) => RefreshPages();

    private void RefreshPages()
    {
#if __IOS__ && !MACCATALYST
        var previousState = _pageState;
#endif
        RefreshPageItems();
#if __IOS__ && !MACCATALYST
        // iOS stops child measure invalidation at ScrollViews, leaving the initially empty row at zero height.
        if ((previousState?.Sequential != _pageState?.Sequential || previousState?.Count != _pageState?.Count) &&
            _pages?.Parent?.Parent is ScrollView paginationScroll)
            ((IView)paginationScroll).InvalidateMeasure();
#endif
    }

    private void RefreshPageItems()
    {
        bool canGoPrevious = ViewModel.SelectPreviousImageCommand.CanExecute(null);
        bool canGoNext = ViewModel.SelectNextImageCommand.CanExecute(null);
#if WINDOWS
        if (_desktopPagination != null)
        {
            bool show = ViewModel.SelectedImage != null;
            SetImagePageButtonState(_previousButton, false, !_searching && canGoPrevious, show);
            SetImagePageButtonState(_nextButton, true, !_searching && canGoNext, show);
            return;
        }
#endif
        bool visible = ViewModel.SelectedImage != null;
        SetImagePageButtonState(_imagePreviousButton, false, !_searching && canGoPrevious, visible);
        SetImagePageButtonState(_imageNextButton, true, !_searching && canGoNext, visible);
        SetImagePageButtonState(_previousButton, false, !_searching && canGoPrevious, visible);
        SetImagePageButtonState(_nextButton, true, !_searching && canGoNext, visible);
        if (_previousButton != null) SetPaginationTabStop(_previousButton, false);
        if (_nextButton != null) SetPaginationTabStop(_nextButton, true);
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
        var previousState = _pageState;
        // Reuse page buttons as the visible range moves to avoid losing focus.
        bool rebuild = previousState == null || previousState.Value.Sequential != ViewModel.IsSequentialNavigationEnabled ||
            previousState.Value.Count != count;
        _pageState = (ViewModel.IsSequentialNavigationEnabled, start, count);
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
                button.Clicked += (_, _) => ChangePage(next);
                button.HandlerChanged += (_, _) => SetPaginationTabStop(button, next);
                return button;
            }
            _pages.Add(SequenceButton(false));
            var current = new Grid { MinimumWidthRequest = 35, MinimumHeightRequest = 44 };
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
            SetImagePageButtonState(previous, false, !_searching && canGoPrevious, true);
            SetPaginationTabStop(previous, false);
            var next = (Button)_pages.Children[2];
            next.Text = Text("NextLabel");
            SetImagePageButtonState(next, true, !_searching && canGoNext, true);
            SetPaginationTabStop(next, true);
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

    private void Previous(object? sender, EventArgs e) => ChangePage(false);
    private void Next(object? sender, EventArgs e) => ChangePage(true);
    private void ChangePage(bool next)
    {
        var command = next ? ViewModel.SelectNextImageCommand : ViewModel.SelectPreviousImageCommand;
        if (!_searching && command.CanExecute(null)) command.Execute(null);
        Refresh();
    }

    #endregion

    #region Icons and menu actions

#if !WINDOWS
    private sealed record MenuAction(string Title, Action Execute, Func<bool> Enabled, Func<View> Icon,
        Func<bool>? Checked = null, bool IsSubmenu = false);
#endif

    internal const string CurrentFootprintGeometry = "F1 M9.16358 0L7.67295 0L0 0L4.5818 7.6L11.4546 19H16.5455L28 0H9.16358Z M15.8314 17.7333H12.1686L2.26954 1.26667H25.7305L15.8314 17.7333Z";

    internal const string OverlayGeometry = "F0 M17.1999 13L22 5H0L9 20H13L13.6 19L19 28H23L32 13H17.1999ZM22.439 27H19.561L11.7832 14H30.2168L22.439 27Z";
    internal const string DisplayImageGeometry =
        "F1 M31.1864 15.1864H27.5786C27.1609 9.41668 22.5833 4.83905 16.8515 4.42359L16.8136 4.42142V0.813559C16.8136 0.364475 16.4491 0 16 0C15.5509 0 15.1864 0.364475 15.1864 0.813559V4.42142C9.41668 4.83905 4.83905 9.41668 4.42359 15.1485L4.42142 15.1864H0.813559C0.364475 15.1864 0 15.5509 0 16C0 16.4491 0.364475 16.8136 0.813559 16.8136H4.42142C4.83905 22.5833 9.41668 27.1609 15.1485 27.5764L15.1864 27.5786V31.1864C15.1864 31.6355 15.5509 32 16 32C16.4491 32 16.8136 31.6355 16.8136 31.1864V27.5786C22.5833 27.1609 27.1609 22.5833 27.5764 16.8515L27.5786 16.8136H31.1864C31.6355 16.8136 32 16.4491 32 16C32 15.5509 31.6355 15.1864 31.1864 15.1864Z" +
        "M16.8136 25.9515V22.5085C16.8136 22.0594 16.4491 21.6949 16 21.6949C15.5509 21.6949 15.1864 22.0594 15.1864 22.5085V25.9515C10.3138 25.5414 6.45858 21.6862 6.05071 16.8504L6.04854 16.8136H9.49153C9.94061 16.8136 10.3051 16.4491 10.3051 16C10.3051 15.5509 9.94061 15.1864 9.49153 15.1864H6.04854C6.45858 10.3138 10.3138 6.45858 15.1496 6.05071L15.1864 6.04854V9.49153C15.1864 9.94061 15.5509 10.3051 16 10.3051C16.4491 10.3051 16.8136 9.94061 16.8136 9.49153V6.04854C21.6862 6.45858 25.5414 10.3138 25.9493 15.1496L25.9515 15.1864H22.5085C22.0594 15.1864 21.6949 15.5509 21.6949 16C21.6949 16.4491 22.0594 16.8136 22.5085 16.8136H25.9515C25.5403 21.6862 21.6862 25.5403 16.8504 25.9493L16.8136 25.9515Z";

    internal const string ExploreImagesGeometry =
        "F1 M26.7358 19.6048L27.7447 18.5074L32.9375 12.8587L24.2416 11.0883L11.1977 8.43275L7.75234 12.1806L13.9879 33.4721L26.7358 19.6048Z " +
        "M9.1681 12.5122L11.647 9.81567L30.469 13.6722L14.5913 30.944L9.1681 12.5122Z " +
        "M6.59961 5.13663C7.92743 5.13684 8.99902 6.21202 8.99902 7.54385C8.99895 8.87562 7.92738 9.95087 6.59961 9.95108C5.27167 9.95107 4.1993 8.87575 4.19922 7.54385C4.19922 6.21189 5.27162 5.13664 6.59961 5.13663Z " +
        "M6.59961 6.74112C6.15162 6.74113 5.79883 7.09452 5.79883 7.54385C5.7989 7.99312 6.15167 8.34658 6.59961 8.34659C7.04738 8.34638 7.39934 7.99299 7.39941 7.54385C7.39941 7.09465 7.04742 6.74132 6.59961 6.74112Z " +
        "M21.6003 19.6003L20.2007 21L14.6003 15.3997L16 14L21.6003 19.6003Z " +
        "M21.6003 15.3997L16 21L14.6003 19.6003L20.2007 14L21.6003 15.3997Z";
    internal const string SequenceGeometry = "M6 4V8H2V28H25V24H29V4H6ZM24 27H3V9H24V27ZM28 23H25V8H7V5H28V23ZM16.0732 21L19.5859 17.5L16.0876 14H17.5016L21 17.5L17.4872 21H16.0732ZM6.00897 17.5L9.51568 14H10.9297L7.42303 17.5L10.9063 21H9.49225L6.00897 17.5Z";

    private static Color MenuAccent =>
#if __IOS__ && !MACCATALYST
        IosAccent;
#else
        Color.FromArgb("#007AC2");
#endif

    private static Microsoft.Maui.Controls.Shapes.Path GeometryIcon(string geometry) => new()
    {
        Data = (Microsoft.Maui.Controls.Shapes.Geometry)new PathGeometryConverter().ConvertFromInvariantString(geometry),
        Fill = MenuAccent, Stroke = Colors.Transparent,
        StrokeThickness = 1.5, Aspect = Stretch.Uniform,
        WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center,
    };

    private static Image GlyphIcon(string glyph) => new()
    {
        Source = new FontImageSource
        {
            Glyph = glyph, FontFamily = ToolkitIcons.FontFamilyName, Size = 22, Color = MenuAccent,
        },
        WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center,
    };

    private static View ImageModeIcon(bool explore) =>
        GeometryIcon(explore ? ExploreImagesGeometry : DisplayImageGeometry);

#if !WINDOWS
    private static View CameraLocationsIcon()
    {
        var icon = GeometryIcon(
            "M5.5 23A3.5 3.5 0 1 0 12.5 23A3.5 3.5 0 1 0 5.5 23Z " +
            "M13.5 17A3.5 3.5 0 1 0 20.5 17A3.5 3.5 0 1 0 13.5 17Z " +
            "M5.5 5A3.5 3.5 0 1 0 12.5 5A3.5 3.5 0 1 0 5.5 5Z " +
            "M21.5 26A3.5 3.5 0 1 0 28.5 26A3.5 3.5 0 1 0 21.5 26Z " +
            "M19.5 8A3.5 3.5 0 1 0 26.5 8A3.5 3.5 0 1 0 19.5 8Z");
        icon.Stroke = icon.Fill;
        icon.Fill = Colors.Transparent;
        icon.StrokeThickness = 1;
        return icon;
    }

    private static View FlyoutIcon(View icon)
    {
#if ANDROID || (__IOS__ && !MACCATALYST)
        SetSheetIconColor(icon);
#endif
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
#if ANDROID
        border.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#CACACC"), Color.FromArgb("#444444"));
        border.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#CACACC"), Color.FromArgb("#666666"));
#endif
#if __IOS__ && !MACCATALYST
        border.StrokeThickness = 0;
        border.BackgroundColor = Colors.Transparent;
#endif
        return border;
    }

#if ANDROID || (__IOS__ && !MACCATALYST)
    private static void SetSheetIconColor(object? icon)
    {
        var light = Color.FromArgb("#777680");
        var dark = Color.FromArgb("#AEAEB2");
        if (icon is Microsoft.Maui.Controls.Shapes.Path path)
        {
            if (path.Fill is SolidColorBrush fill && fill.Color != Colors.Transparent)
                path.SetAppThemeColor(Microsoft.Maui.Controls.Shapes.Shape.FillProperty, light, dark);
            if (path.Stroke is SolidColorBrush stroke && stroke.Color != Colors.Transparent)
                path.SetAppThemeColor(Microsoft.Maui.Controls.Shapes.Shape.StrokeProperty, light, dark);
        }
        else if (icon is Image image)
            SetSheetIconColor(image.Source);
        else if (icon is FontImageSource glyph)
            glyph.SetAppThemeColor(FontImageSource.ColorProperty, light, dark);
    }
#endif

    private bool TogglePanel(View anchor)
    {
        if (_panelAnchor != anchor) return false;
        DismissPanel();
        return true;
    }
#endif

    #endregion

    #region Options and marker panels

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

#if !WINDOWS
    private void ShowSymbols(View anchor, SelectNewMarkerSymbolVM picker, Action back)
    {
        DismissPanel(preservePresentation: true);
        var content = PanelContent(Text("MarkerSelection"));
        _symbolGrid = new Grid { ColumnSpacing = 4, RowSpacing = 0 };
        foreach (var symbol in picker.SymbolOptions)
        {
            var action = new MenuAction(MarkerOptionName(symbol, _symbolTiles.Count), () => { picker.SelectedSymbol = symbol; back(); },
                Checked: () => symbol == picker.SelectedSymbol, Enabled: () => IsAddingMarkers,
                Icon: () => new SymbolDisplay
                {
                    Symbol = symbol, WidthRequest = 24, HeightRequest = 24, VerticalOptions = LayoutOptions.Center,
                });
            var guarded = GuardAction(action,
#if ANDROID || (__IOS__ && !MACCATALYST)
                dismiss: false
#else
                dismiss: true
#endif
            );
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
                bool selected = action.Checked?.Invoke() == true;
#if ANDROID
                tile.Stroke = Color.FromArgb(selected ? "#454451" : "#A09DA4");
#else
                tile.Stroke = selected ? Color.FromArgb("#007AC2") : Color.FromArgb("#888888");
#endif
                tile.IsEnabled = action.Enabled();
                tile.Opacity = tile.IsEnabled ? 1 : 0.45;
                SemanticProperties.SetHint(button, Text(selected ? "Selected" : "NotSelected"));
            });
            _symbolTiles.Add(tile);
        }
        content.Add(_symbolGrid);
        PresentPanel(anchor, content, back);
        PropertyChangedEventHandler changed = (_, _) => DispatchUpdate(UpdateOpenPanel);
        picker.PropertyChanged += changed;
        _panelCleanup.Add(() => picker.PropertyChanged -= changed);
        System.Collections.Specialized.NotifyCollectionChangedEventHandler optionsChanged = (_, _) => DispatchUpdate(() =>
        {
            if (_symbolGrid != null) ShowSymbols(anchor, picker, back);
        });
        picker.SymbolOptions.CollectionChanged += optionsChanged;
        _panelCleanup.Add(() => picker.SymbolOptions.CollectionChanged -= optionsChanged);
    }

    private MenuAction GuardAction(MenuAction action, bool dismiss = true)
    {
        // A button from a replaced menu must not act on the current layer.
        int version = _menuVersion;
        var model = ViewModel;
        var layer = SelectedLayer;
        return action with
        {
            Execute = () =>
            {
                if (version != _menuVersion || !_connected || model != ViewModel || layer != SelectedLayer || !action.Enabled())
                    return;
                if (dismiss) DismissPanel();
                action.Execute();
                Refresh();
            },
        };
    }

    private void AddActionRow(VerticalStackLayout content, MenuAction action)
    {
        action = GuardAction(action,
#if ANDROID || (__IOS__ && !MACCATALYST)
            dismiss: false);
#else
            dismiss: action.Checked != null);
#endif
        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(32), new ColumnDefinition(GridLength.Star), new ColumnDefinition(28) },
            ColumnSpacing = 6, Padding = new Thickness(4, 0), MinimumHeightRequest = 44,
        };
        var button = new Button { BackgroundColor = Colors.Transparent, Padding = 0 };
        SemanticProperties.SetDescription(button, action.Title);
        button.Clicked += (_, _) => action.Execute();
        row.Add(button, 0, 0);
        Grid.SetColumnSpan(button, 3);
        var icon = action.Icon();
        icon.InputTransparent = true;
        row.Add(FlyoutIcon(icon));
        var title = new Label { Text = action.Title, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap, InputTransparent = true };
#if ANDROID
        title.FontSize = 16;
        row.MinimumHeightRequest = 56;
#endif
        title.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
#if __IOS__ && !MACCATALYST
        title.FontSize = 17;
        row.MinimumHeightRequest = 52;
        if (action.Title == Text("ClearMarkers"))
        {
            title.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#FF3B30"), Color.FromArgb("#FF453A"));
            if (icon is Image { Source: FontImageSource clearIcon })
                clearIcon.SetAppThemeColor(FontImageSource.ColorProperty, Color.FromArgb("#FF3B30"), Color.FromArgb("#FF453A"));
        }
#endif
        row.Add(title, 1);
        var selection = new Label { Text = "\u2022", TextColor = Color.FromArgb("#007AC2"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center,
            FontSize = 24, FontAutoScalingEnabled = false, InputTransparent = true };
#if ANDROID || (__IOS__ && !MACCATALYST)
        selection.Text = "\u2713";
        selection.FontSize = 17;
#endif
#if ANDROID || (__IOS__ && !MACCATALYST)
        selection.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#777680"), Color.FromArgb("#AEAEB2"));
#endif
        if (action.Checked != null) row.Add(selection, 2);
        if (action.IsSubmenu)
        {
            var chevron = GlyphIcon(ToolkitIcons.ChevronRight);
#if ANDROID || (__IOS__ && !MACCATALYST)
            SetSheetIconColor(chevron);
#endif
            chevron.InputTransparent = true;
            row.Add(chevron, 2);
        }
        _panelUpdates.Add(() =>
        {
            bool enabled = action.Enabled();
            row.IsEnabled = enabled;
            row.Opacity = enabled ? 1 : 0.45;
            bool selected = action.Checked?.Invoke() == true;
            selection.IsVisible = selected;
            if (action.Checked != null) SemanticProperties.SetHint(button, Text(selected ? "Selected" : "NotSelected"));
        });
        AddPanelItem(content, row);
    }

    private static VerticalStackLayout PanelContent(string title)
    {
        var content = new VerticalStackLayout { Spacing = 0, Padding = 8 };
        var heading = new Label
        {
            Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 4),
        };
#if __IOS__ && !MACCATALYST
        heading.FontSize = 15;
        heading.VerticalTextAlignment = TextAlignment.Center;
        content.Spacing = 8;
#endif
        heading.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        content.Add(heading);
        return content;
    }

    private void AddSwitch(VerticalStackLayout content, string key, Func<bool> get, Action<bool> set, Func<View> icon, Func<bool> enabled)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(32), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 6 };
        string title = Text(key == "AddMarkerLabel" ? "MobileAddMarkers" : key);
        var label = new Label { Text = title, VerticalOptions = LayoutOptions.Center };
#if ANDROID
        label.FontSize = 16;
        row.MinimumHeightRequest = 56;
#endif
        label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        var toggle = new Switch { IsToggled = get(), MinimumHeightRequest = 44, AutomationId = "OrientedImagery" + key };
#if ANDROID
        toggle.SetAppThemeColor(Switch.OnColorProperty, Color.FromArgb("#585992"), Color.FromArgb("#C3BFFF"));
#endif
#if __IOS__ && !MACCATALYST
        row.MinimumHeightRequest = 52;
        label.FontSize = 17;
        label.LineBreakMode = LineBreakMode.WordWrap;
        toggle.MinimumHeightRequest = -1;
        toggle.VerticalOptions = LayoutOptions.Center;
        toggle.OnColor = Color.FromArgb("#34C759");
#endif
        bool CanInteract() => !_updatingPanel && _connected && _panelAnchor != null && enabled();
        SemanticProperties.SetDescription(toggle, title);
        toggle.Toggled += (_, e) =>
        {
            if (CanInteract())
                set(e.Value);
        };
#if ANDROID || (__IOS__ && !MACCATALYST)
        label.VerticalOptions = LayoutOptions.Fill;
        label.VerticalTextAlignment = TextAlignment.Center;
        var labelTap = new TapGestureRecognizer();
        labelTap.Tapped += (_, _) =>
        {
            if (toggle.IsEnabled && CanInteract())
                toggle.IsToggled = !toggle.IsToggled;
        };
        label.GestureRecognizers.Add(labelTap);
#endif
        row.Add(FlyoutIcon(icon()));
        row.Add(label, 1);
        row.Add(toggle, 2);
        _panelUpdates.Add(() =>
        {
            toggle.IsToggled = get();
            row.IsEnabled = enabled();
            row.Opacity = row.IsEnabled ? 1 : 0.45;
        });
        AddPanelItem(content, row);
    }

    private static void AddPanelItem(VerticalStackLayout content, View item)
    {
#if ANDROID
        content.Add(item);
        AddAndroidPanelDivider(content);
#elif __IOS__ && !MACCATALYST
        var group = new Border
        {
            Content = item, StrokeThickness = 0, Padding = new Thickness(12, 4),
        };
        var shape = new RoundRectangle { CornerRadius = 26 };
        group.StrokeShape = shape;
        group.SizeChanged += (_, _) => shape.CornerRadius = new CornerRadius(group.Height / 2);
        group.MinimumHeightRequest = 52;
        SetIosOptionRowBackground(group);
        content.Add(group);
#else
        content.Add(item);
#endif
    }

#if ANDROID
    private static BoxView PanelDivider()
    {
        var divider = new BoxView { HeightRequest = 1, InputTransparent = true, AutomationId = "OrientedImageryMenuDivider" };
        divider.SetAppThemeColor(BoxView.ColorProperty, Color.FromArgb("#DCD8E2"), Color.FromArgb("#55515B"));
        return divider;
    }

    private static void AddAndroidPanelDivider(VerticalStackLayout content)
    {
        if (content.Children.LastOrDefault() is not BoxView { AutomationId: "OrientedImageryMenuDivider" })
            content.Add(PanelDivider());
    }
#endif

    private void PresentPanel(View anchor, VerticalStackLayout content, Action? back = null)
    {
#if ANDROID || (__IOS__ && !MACCATALYST)
        _panelAnchor = anchor;
        var heading = (Label)content.Children[0];
        content.Remove(heading);
        content.Padding = new Thickness(16, 0, 16, 16);
        _panelScroll = new ScrollView { Content = content };
        var surface = CreateMobileSheetContent(heading, _panelScroll, () => DismissPanel(), back,
            out _panelLayout, "OrientedImageryToolsSheet");
#if ANDROID
        PresentAndroidPanel(surface);
#else
        PresentIosSheet(surface, info: false);
#endif
        UpdateToolHighlights();
        UpdateBackNavigation();
        UpdateOpenPanel();
        SemanticScreenReader.Default.Announce(heading.Text);
#else
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
            Padding = new Thickness(8, 4),
        };
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
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var closeButton = ToolButton("ClosePanel", "", "OrientedImageryPanelClose");
        closeButton.Text = "X";
        closeButton.FontAutoScalingEnabled = false;
        closeButton.WidthRequest = 44;
        closeButton.BorderWidth = 0;
        closeButton.Clicked += (_, _) => DismissPanel();
        header.Add(closeButton, 2);
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
        panel.Content = _panelLayout;
        panel.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#303030"));
        _panel = panel;
        _panelHost.Add(panel);
        _panelHost.IsVisible = true;
        UpdateOpenPanel();
        SemanticScreenReader.Default.Announce(heading.Text);
#endif
    }
#endif

    #endregion

    #region Panel sizing and dismissal

#if !WINDOWS && !ANDROID && (!__IOS__ || MACCATALYST)
    private void PanelHostSizeChanged(object? sender, EventArgs e) => UpdatePanelSize();

    private void ToolbarScrolled(object? sender, ScrolledEventArgs e) => UpdatePanelSize();
#endif

    private void UpdatePanelSize()
    {
#if ANDROID
        UpdateAndroidPanelSize();
#elif __IOS__ && !MACCATALYST
        UpdateIosPanelSize();
#elif !WINDOWS
        if (_panelHost == null || _panelLayout == null || _panelScroll == null || _panel == null || _panelAnchor == null) return;
        double x = 0, y = _panelAnchor.Height;
        Element? element = _panelAnchor;
        while (element is VisualElement visual && element != _panelHost.Parent)
        {
            x += visual.X;
            y += visual.Y;
            if (visual is ScrollView scroll)
            {
                x -= scroll.ScrollX;
                y -= scroll.ScrollY;
            }
            element = visual.Parent;
        }
        double maximumWidth = Math.Max(0, Math.Min(320, _panelHost.Width - 8));
        if (_symbolGrid != null)
            LayoutSymbolGrid(Math.Max(0, maximumWidth - _panelLayout.Padding.HorizontalThickness - 2 * _panel.StrokeThickness));
        double contentWidth = _panelLayout.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
        double panelWidth = Math.Min(maximumWidth, contentWidth + 2 * _panel.StrokeThickness);
        _panel.WidthRequest = panelWidth;
        x = Math.Clamp(x, 4, Math.Max(4, _panelHost.Width - panelWidth - 4));
        _panel.Margin = new Thickness(x, y, 4, 4);
        if (_panelBackdrop != null)
        {
            double toolbarBottom = _customToolbar is null ? y : _customToolbar.Y + _customToolbar.Height;
            _panelBackdrop.Margin = new Thickness(0, toolbarBottom, 0, 0);
        }
        double width = Math.Max(0, panelWidth - _panelLayout.Padding.HorizontalThickness - 2 * _panel.StrokeThickness);
        double headingHeight = _panelLayout.Children[0].Measure(width, double.PositiveInfinity).Height;
        _panelScroll.MaximumHeightRequest = Math.Max(0,
            _panelHost.Height - y - 4 - _panelLayout.Padding.VerticalThickness - headingHeight);
#endif
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
        if (!open && ViewModel.SelectedImagePopup != null)
        {
            ViewModel.CloseSelectedImagePopupCommand.Execute(null);
            return true;
        }
        return open;
    }

    private void DismissPanel(bool preservePresentation = false)
    {
#if ANDROID
        DismissAndroidPanel(preservePresentation);
#endif
#if __IOS__ && !MACCATALYST
        if (!_iosSheetIsInfo && !preservePresentation)
            DismissIosSheet();
#endif
        _menuVersion++;
        foreach (var cleanup in _panelCleanup) cleanup();
        _panelCleanup.Clear();
        _panelUpdates.Clear();
#if !WINDOWS && !ANDROID && (!__IOS__ || MACCATALYST)
        _panel = null;
        _panelBackdrop = null;
#endif
#if !WINDOWS
        _panelScroll = null;
        _panelLayout = null;
        _symbolGrid = null;
        _symbolTiles.Clear();
#endif
#if !WINDOWS && !ANDROID && (!__IOS__ || MACCATALYST)
        if (_panelHost != null)
        {
            _panelHost.IsVisible = false;
            _panelHost.Clear();
        }
#endif
        var anchor = _panelAnchor;
        _panelAnchor = null;
        UpdateToolHighlights();
        UpdateBackNavigation();
#if ANDROID || (__IOS__ && !MACCATALYST)
        if (!preservePresentation && anchor?.IsLoaded == true) anchor.Focus();
#else
        anchor?.Focus();
#endif
    }

    private void UpdateToolHighlights()
    {
        foreach (var update in _toolbarUpdates)
            update();
    }

    #endregion

    #region Platform presentation hooks

    partial void RefreshDesktopPresentation();
    partial void AttachPlatformPresentation();
    partial void DetachPlatformPresentation();
    partial void AttachBackNavigation();
    partial void DetachBackNavigation();
    partial void UpdateBackNavigation();

    #endregion
}
