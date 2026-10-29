using System.Collections.Specialized;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    #region Toolbar state and interaction modes

    private readonly HashSet<OrientedImageryToolbarCategory> _toolbarCategories = new();
    // Keep the last image mode even if an app removes all ImageSearchModeVM items.
    private bool _imageSearchEnabled = true;
    private int _toolbarRevision;
    private int _renderedToolbarRevision = -1;
    private readonly List<Action> _toolbarUpdates = new();
    private readonly List<Action> _toolbarCleanup = new();

    internal bool ImageSearchEnabled => ViewModel.InteractionState switch
    {
        OrientedImageryViewInteractionState.FindImages => true,
        OrientedImageryViewInteractionState.DisplayImage => false,
        _ => _imageSearchEnabled,
    };

    internal bool IsAddingMarkers => ViewModel.InteractionState == OrientedImageryViewInteractionState.AddMarkers;

    internal static void ToggleImageMode(ImageSearchModeVM mode, OrientedImageryViewInteractionState state)
    {
        if (mode.IsImageSelectionEnabled && mode.SelectedInteractionState == state)
            mode.IsImageSelectionEnabled = false;
        else
        {
            mode.SelectedInteractionState = state;
            mode.IsImageSelectionEnabled = true;
        }
    }

    internal void SetAddingMarkers(bool enabled)
    {
        _imageSearchEnabled = ImageSearchEnabled;
        ViewModel.InteractionState = enabled ? OrientedImageryViewInteractionState.AddMarkers :
            OrientedImageryViewInteractionState.None;
    }

    #endregion

    #region Toolbar subscriptions and templates

    private void ToolbarChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.IsDispatchRequired)
        {
            Dispatcher.Dispatch(() => ToolbarChanged(sender, e));
            return;
        }
        ObserveToolbarCategories();
        Refresh();
    }

    private void ObserveToolbarCategories()
    {
        _toolbarRevision++;
        DismissPanel();
        foreach (var category in _toolbarCategories)
            category.ToolbarItems.CollectionChanged -= ToolbarChanged;
        _toolbarCategories.Clear();
        foreach (var category in ViewModel.ToolbarCategories)
        {
            if (_toolbarCategories.Add(category))
                category.ToolbarItems.CollectionChanged += ToolbarChanged;
        }
    }

    private void InvalidateToolbar()
    {
        _toolbarRevision++;
        DismissPanel();
        Refresh();
    }

    private void SelectorEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(InvalidateToolbar);
        else
            InvalidateToolbar();
    }

    private void ReleaseToolbarPresentation()
    {
        foreach (var cleanup in _toolbarCleanup)
            cleanup();
        _toolbarCleanup.Clear();
        _toolbarUpdates.Clear();
        _customToolbar?.Clear();
        _renderedToolbarRevision = -1;
    }

    private static bool IsBuiltInToolbarItem(OrientedImageryToolbarItemBase item) =>
        item is ImageSearchModeVM or ShowSelectedFootprintVM or ShowUnselectedFootprintsVM or ShowCameraMarkersVM or
            AllowAddingMarkersVM or SelectNewMarkerSymbolVM or ClearMarkersVM or SequentialNavigationVM or AutoUpdateFootprintVM or ToolbarDividerVM;

    private View? CreateCustomToolbarView(object item)
    {
        var template = ToolbarItemTemplateSelector?.SelectTemplate(item, this);
        if (template == null || template == OrientedImageryViewToolbarSelector.DefaultTemplate)
            return null;
        if (template.CreateContent() is not View view)
            throw new InvalidOperationException("An oriented imagery toolbar template must create a MAUI View.");
        view.BindingContext = item;
        return view;
    }

    private void RefreshToolbarPresentation()
    {
        if (_customToolbar == null)
            return;
        if (_renderedToolbarRevision != _toolbarRevision)
        {
            // Rebuild only when toolbar items or templates change.
            ReleaseToolbarPresentation();
            if ((ToolbarItemTemplateSelector as OrientedImageryViewToolbarSelector)?.SelectionPairs is INotifyCollectionChanged entries)
            {
                entries.CollectionChanged += SelectorEntriesChanged;
                _toolbarCleanup.Add(() => entries.CollectionChanged -= SelectorEntriesChanged);
            }
            foreach (var category in ViewModel.ToolbarCategories)
            {
                var view = CreateCustomToolbarView(category) ?? CreateToolbarCategory(category);
                _customToolbar.Add(view);
            }
            _renderedToolbarRevision = _toolbarRevision;
        }
        _customToolbar.IsVisible = HasValidLayer;
        UpdateToolHighlights();
    }

    #endregion

    #region Category presentation

    private bool IsToolbarItemVisible(OrientedImageryToolbarItemBase item) =>
        item is not SequentialNavigationVM || ViewModel.SupportsSequentialNavigation;

    private View CreateToolbarCategory(OrientedImageryToolbarCategory category)
    {
#if WINDOWS
        var group = new ToolbarWrapLayout();
        if (category != ViewModel.ToolbarCategories.FirstOrDefault() && category.ToolbarItems.FirstOrDefault() is not ToolbarDividerVM)
        {
            var divider = new BoxView { WidthRequest = 1, HeightRequest = 72, Margin = new Thickness(8, 6) };
            divider.SetAppThemeColor(BoxView.ColorProperty, Color.FromArgb("#888888"), Color.FromArgb("#AAAAAA"));
            group.Add(divider);
        }
        foreach (var item in category.ToolbarItems)
        {
            var view = CreateCustomToolbarView(item);
            if (view == null)
            {
                if (!IsBuiltInToolbarItem(item))
                    throw new InvalidOperationException($"Register a toolbar template for '{item.GetType().Name}'.");
                var controlsView = new OrientedImageryControlsView
                {
                    Owner = this, ToolbarItem = item,
                    MinimumHeightRequest = 84,
                };
                view = controlsView;
                _toolbarUpdates.Add(controlsView.Refresh);
                _toolbarCleanup.Add(() => controlsView.Owner = null);
            }
            group.Add(view);
            _toolbarUpdates.Add(() => view.IsVisible = IsToolbarItemVisible(item));
        }
        _toolbarUpdates.Add(() =>
        {
            group.MaximumWidthRequest = Width > 16 ? Width - 16 : double.PositiveInfinity;
            group.IsVisible = category.ToolbarItems.Any(item => item is not ToolbarDividerVM && IsToolbarItemVisible(item));
        });
        return group;
#else
        var key = category.Name;
        bool navigation = key == OrientedImageryToolbarCategory.MoreCategoryName &&
            category.ToolbarItems.Count == 1 && category.ToolbarItems[0] is SequentialNavigationVM;
        bool known = key is OrientedImageryToolbarCategory.ModeCategoryName or OrientedImageryToolbarCategory.OverlaysCategoryName or
            OrientedImageryToolbarCategory.MarkersCategoryName or OrientedImageryToolbarCategory.MoreCategoryName;
        var cell = new Grid { MinimumWidthRequest = 44, MinimumHeightRequest = 44 };
        Layout content =
#if __IOS__ && !MACCATALYST
            new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
#else
            new HorizontalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
#endif
        var icon = MainMenuIcon(navigation ? GeometryIcon(SequenceGeometry) : key switch
        {
            OrientedImageryToolbarCategory.ModeCategoryName => ImageModeIcon(ImageSearchEnabled),
            OrientedImageryToolbarCategory.OverlaysCategoryName => GeometryIcon(OverlayGeometry),
            OrientedImageryToolbarCategory.MarkersCategoryName => new Label
            {
                Text = "\uE8ED", FontFamily = ToolkitIcons.FontFamilyName, FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                FontAutoScalingEnabled = false, LineBreakMode = LineBreakMode.NoWrap,
                HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center,
            },
            _ => new Label { Text = "\u2022\u2022\u2022", FontAutoScalingEnabled = false, TextColor = Color.FromArgb("#007AC2") },
        });
        icon.WidthRequest = icon.HeightRequest = navigation ? 14 : 16;
        if (key == OrientedImageryToolbarCategory.MarkersCategoryName)
            icon.HeightRequest = 22;
        var iconHost = new Grid { WidthRequest = icon.WidthRequest, HeightRequest = icon.HeightRequest };
        iconHost.Add(icon);
        content.Add(iconHost);
        var label = new Label
        {
            Text = navigation ? Text("Navigation") : known ? Text(key) : key,
            FontSize = navigation ? 11 : 12, VerticalOptions = LayoutOptions.Center, MaxLines = 1,
            HorizontalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.NoWrap,
        };
        label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
#if ANDROID
        label.HandlerChanged += (sender, _) =>
        {
            if (sender is Label { Handler.PlatformView: Android.Widget.TextView nativeLabel })
                nativeLabel.Typeface = Android.Graphics.Typeface.Create(nativeLabel.Typeface, 600, false);
        };
#endif
        content.Add(label);
        var pill = new Border
        {
            Content = content, MinimumHeightRequest = 26, Padding = new Thickness(4, 2),
            VerticalOptions = LayoutOptions.Center, Margin = new Thickness(2, 0),
            Stroke = Color.FromArgb("#CCCCCC"), StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 13 }, InputTransparent = true,
        };
#if __IOS__ && !MACCATALYST
        cell.MinimumHeightRequest = 60;
        icon.WidthRequest = icon.HeightRequest = 22;
        iconHost.WidthRequest = iconHost.HeightRequest = 22;
        if (key == OrientedImageryToolbarCategory.MarkersCategoryName && icon is Label markerIcon)
        {
            markerIcon.FontSize = 22;
            icon.HeightRequest = iconHost.HeightRequest = 28;
        }
        label.FontSize = 11;
        label.FontAttributes = FontAttributes.Bold;
        label.TextColor = IosAccent;
        pill.VerticalOptions = LayoutOptions.Fill;
        pill.Margin = new Thickness(2, 4, 2, 8);
        pill.StrokeThickness = 0;
        pill.StrokeShape = new RoundRectangle { CornerRadius = 10 };
        pill.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#3A3A3C"));
#endif
        var button = new Button
        {
            BackgroundColor = Colors.Transparent, BorderWidth = 0, Padding = 0,
            MinimumWidthRequest = 44, MinimumHeightRequest = cell.MinimumHeightRequest,
            AutomationId = "OrientedImagery" + (navigation ? "Navigation" : key),
        };
        SemanticProperties.SetDescription(button, key == OrientedImageryToolbarCategory.ModeCategoryName ? Text("ChangeMapInteraction") : label.Text);
        cell.Add(pill);
        cell.Add(button);
        button.Clicked += (_, _) =>
        {
            if (TogglePanel(button))
                return;
            ShowToolbarCategory(category, button, label.Text);
        };
        bool? previousActive = null;
        bool? previousSearch = null;
        _toolbarUpdates.Add(() =>
        {
            cell.IsVisible = category.ToolbarItems.Any(item => item is not ToolbarDividerVM && IsToolbarItemVisible(item));
            button.IsEnabled = HasValidLayer && !_searching;
            bool active = (key == OrientedImageryToolbarCategory.ModeCategoryName &&
                    ViewModel.InteractionState is OrientedImageryViewInteractionState.FindImages or OrientedImageryViewInteractionState.DisplayImage) ||
                (key == OrientedImageryToolbarCategory.MarkersCategoryName && IsAddingMarkers) ||
                (key == OrientedImageryToolbarCategory.MoreCategoryName && ViewModel.IsSequentialNavigationEnabled);
#if ANDROID || (__IOS__ && !MACCATALYST)
            if (key == OrientedImageryToolbarCategory.OverlaysCategoryName)
                active = ViewModel.ShowSelectedFootprint || ViewModel.ShowUnselectedFootprints || ViewModel.ShowCameraLocations;
#endif
            pill.Opacity = button.IsEnabled ? 1 : 0.45;
            bool modeChanged = key == OrientedImageryToolbarCategory.ModeCategoryName && previousSearch != ImageSearchEnabled;
            if (modeChanged)
            {
                iconHost.Clear();
                icon = MainMenuIcon(ImageModeIcon(ImageSearchEnabled));
                icon.WidthRequest = iconHost.WidthRequest;
                icon.HeightRequest = iconHost.HeightRequest;
                iconHost.Add(icon);
            }
            if (previousActive != active || modeChanged)
            {
                var accent = MenuAccent;
                pill.BackgroundColor = active ? accent : Colors.Transparent;
                if (active)
                    label.TextColor = Colors.White;
                else
                    label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
                if (icon is Image { Source: FontImageSource font })
                    font.Color = active ? Colors.White : accent;
                if (icon is Microsoft.Maui.Controls.Shapes.Path path)
                    path.Stroke = path.Fill = active ? Colors.White : accent;
                if (icon is Label moreIcon)
                    moreIcon.TextColor = active ? Colors.White : accent;
#if __IOS__ && !MACCATALYST
                label.TextColor = active ? Colors.White : IosAccent;
                if (!active)
                    pill.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#3A3A3C"));
#endif
            }
            if (key == OrientedImageryToolbarCategory.ModeCategoryName)
                SemanticProperties.SetHint(button, $"{Text(ImageSearchEnabled ? "ImageSearchModeFind" : "ImageSearchModeDisplay")}, {Text(active ? "On" : "Off")}");
            else if (key == OrientedImageryToolbarCategory.MarkersCategoryName)
                SemanticProperties.SetHint(button, Text(IsAddingMarkers ? "On" : "Off"));
            else if (navigation)
                SemanticProperties.SetHint(button, Text(ViewModel.IsSequentialNavigationEnabled ? "On" : "Off"));
#if ANDROID || (__IOS__ && !MACCATALYST)
            else if (key == OrientedImageryToolbarCategory.OverlaysCategoryName)
                SemanticProperties.SetHint(button, Text(active ? "On" : "Off"));
#endif
            previousActive = active;
            previousSearch = ImageSearchEnabled;
        });
        return cell;
#endif
    }

    #endregion

#if !WINDOWS
    #region Mobile toolbar layout

    private sealed class ToolbarRowLayout : HorizontalStackLayout
    {
        protected override ILayoutManager CreateLayoutManager() => new ToolbarRowLayoutManager(this);
    }

    private sealed class ToolbarRowLayoutManager(ToolbarRowLayout layout) : HorizontalStackLayoutManager(layout)
    {
        public override Microsoft.Maui.Graphics.Size ArrangeChildren(Microsoft.Maui.Graphics.Rect bounds)
        {
            var children = layout.Children.Where(child => child.Visibility != Visibility.Collapsed).ToArray();
            double contentWidth = children.Sum(child => child.DesiredSize.Width);
            double extra = children.Length == 0 ? 0 :
                Math.Max(0, bounds.Width - layout.Padding.HorizontalThickness - contentWidth) / children.Length;
            double x = bounds.X + layout.Padding.Left;
            foreach (var child in children)
            {
                double width = child.DesiredSize.Width + extra;
                child.Arrange(new Microsoft.Maui.Graphics.Rect(x, bounds.Y + layout.Padding.Top,
                    width, bounds.Height - layout.Padding.VerticalThickness));
                x += width;
            }
            return bounds.Size;
        }
    }

    private static View MainMenuIcon(View icon)
    {
        if (icon is Microsoft.Maui.Controls.Shapes.Path path)
        {
            path.Stroke = path.Fill;
            path.StrokeThickness = 0.75;
        }
        return icon;
    }

    #endregion

    #region Mobile toolbar options

    private void ShowToolbarCategory(OrientedImageryToolbarCategory category, View anchor, string title)
    {
        DismissPanel(preservePresentation: true);
        var panel = PanelContent(title);
        foreach (var item in category.ToolbarItems.Where(IsToolbarItemVisible))
        {
            var custom = CreateCustomToolbarView(item);
            if (custom != null)
                panel.Add(custom);
            else
                AddMobileToolbarItem(panel, item, anchor, () => ShowToolbarCategory(category, anchor, title));
        }
        PresentPanel(anchor, panel);
    }

    private void AddMobileToolbarItem(VerticalStackLayout content, OrientedImageryToolbarItemBase item, View anchor, Action back)
    {
        bool Enabled() => HasValidLayer && !_searching;
        switch (item)
        {
            case ImageSearchModeVM mode:
#if ANDROID || (__IOS__ && !MACCATALYST)
                AddMobileModeSegments(content, mode, Enabled);
#else
                foreach (var state in mode.ImageSearchModes)
                {
                    bool search = state == OrientedImageryViewInteractionState.FindImages;
                    AddActionRow(content, new MenuAction(Text(search ? "ImageSearchModeFind" : "ImageSearchModeDisplay"),
                        () => ToggleImageMode(mode, state), Icon: () => ImageModeIcon(search),
                        Checked: () => ViewModel.InteractionState == state, Enabled: Enabled));
                }
#endif
                break;
            case ShowSelectedFootprintVM:
                AddSwitch(content, "MobileCurrentFootprint",
                    () => ViewModel.ShowSelectedFootprint, value => ViewModel.ShowSelectedFootprint = value,
                    () => GeometryIcon(CurrentFootprintGeometry), Enabled);
                break;
            case ShowUnselectedFootprintsVM:
                AddSwitch(content, "MobileAdditionalFootprints",
                    () => ViewModel.ShowUnselectedFootprints, value => ViewModel.ShowUnselectedFootprints = value,
                    () => GeometryIcon(OverlayGeometry), () => Enabled() && ViewModel.Images.Count > 1);
                break;
            case ShowCameraMarkersVM:
                AddSwitch(content, "MobileCameraLocations",
                    () => ViewModel.ShowCameraLocations, value => ViewModel.ShowCameraLocations = value,
                    CameraLocationsIcon, () => Enabled() && ViewModel.Images.Count > 1);
                break;
            case AllowAddingMarkersVM:
                AddSwitch(content, "AddMarkerLabel", () => IsAddingMarkers, SetAddingMarkers, () => GlyphIcon("\uE8ED"), Enabled);
                break;
            case SelectNewMarkerSymbolVM picker:
                var preview = new SymbolDisplay { Symbol = picker.SelectedSymbol, WidthRequest = 24, HeightRequest = 24 };
                AddActionRow(content, new MenuAction(Text("SelectMarkerSymbol"), () => ShowSymbols(anchor, picker, back),
                    Icon: () => preview, IsSubmenu: true, Enabled: () => Enabled() && IsAddingMarkers));
                _panelUpdates.Add(() => preview.Symbol = picker.SelectedSymbol);
                System.ComponentModel.PropertyChangedEventHandler changed = (_, _) => DispatchUpdate(UpdateOpenPanel);
                picker.PropertyChanged += changed;
                _panelCleanup.Add(() => picker.PropertyChanged -= changed);
                break;
            case ClearMarkersVM:
                AddActionRow(content, new MenuAction(Text("ClearMarkers"), () => ViewModel.ClearMarkersCommand.Execute(null),
                    Enabled: () => Enabled() && ViewModel.ClearMarkersCommand.CanExecute(null), Icon: () => GlyphIcon(ToolkitIcons.Trash)));
                break;
            case SequentialNavigationVM:
                AddSwitch(content, "SequentialMode", () => ViewModel.IsSequentialNavigationEnabled,
                    value =>
                    {
                        if (value != ViewModel.IsSequentialNavigationEnabled && ViewModel.ToggleSequentialNavigationCommand.CanExecute(null))
                            ViewModel.ToggleSequentialNavigationCommand.Execute(null);
                    },
                    () => GeometryIcon(SequenceGeometry),
                    () => Enabled() && ViewModel.ToggleSequentialNavigationCommand.CanExecute(null));
                break;
            case AutoUpdateFootprintVM:
                AddSwitch(content, "AutoUpdate",
                    () => ViewModel.AutoUpdateFootprint, value => ViewModel.AutoUpdateFootprint = value,
                    () => GlyphIcon(ToolkitIcons.Refresh), Enabled);
                break;
            case ToolbarDividerVM:
                content.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#CCCCCC"), Margin = new Thickness(0, 4) });
                break;
            default:
                throw new InvalidOperationException($"Register a toolbar template for '{item.GetType().Name}'.");
        }
    }

    #endregion
#endif
}
