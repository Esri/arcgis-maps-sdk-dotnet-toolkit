using System.Collections.Specialized;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private readonly HashSet<OrientedImageryToolbarCategory> _toolbarCategories = new();
    // Keep the last image mode even if an app removes all ImageSearchModeVM items.
    private bool _imageSearchEnabled = true;
    private int _toolbarRevision;
    private int _renderedToolbarRevision = -1;
    private readonly List<Action> _toolbarUpdates = new();
    private readonly List<Action> _toolbarCleanup = new();
    private INotifyCollectionChanged? _selectorEntries;

    internal bool ImageSearchEnabled => ViewModel.InteractionState switch
    {
        OrientedImageryViewInteractionState.ExploreImages => true,
        OrientedImageryViewInteractionState.ExamineImage => false,
        _ => _imageSearchEnabled,
    };

    internal bool IsAddingMarkers => ViewModel.InteractionState == OrientedImageryViewInteractionState.AddMarkers;

    internal void SetAddingMarkers(bool enabled)
    {
        _imageSearchEnabled = ImageSearchEnabled;
        ViewModel.InteractionState = enabled ? OrientedImageryViewInteractionState.AddMarkers :
            OrientedImageryViewInteractionState.None;
    }

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
        if (_selectorEntries != null)
            _selectorEntries.CollectionChanged -= SelectorEntriesChanged;
        _selectorEntries = null;
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
            ReleaseToolbarPresentation();
            _selectorEntries = (ToolbarItemTemplateSelector as OrientedImageryViewToolbarSelector)?.SelectionPairs as INotifyCollectionChanged;
            if (_selectorEntries != null)
                _selectorEntries.CollectionChanged += SelectorEntriesChanged;
            foreach (var category in ViewModel.ToolbarCategories)
            {
                var view = CreateCustomToolbarView(category) ?? CreateToolbarCategory(category);
                _customToolbar.Add(view);
            }
            _renderedToolbarRevision = _toolbarRevision;
        }
        _customToolbar.IsVisible = HasValidLayer;
        foreach (var update in _toolbarUpdates)
            update();
    }

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
                var bar = new DesktopImageryBar
                {
                    Owner = this, ToolbarItem = item,
                    WidthRequest = item switch
                    {
                        ToolbarDividerVM => 17,
                        ImageSearchModeVM => 80,
                        AllowAddingMarkersVM => 48,
                        SelectNewMarkerSymbolVM => 32,
                        _ => 78,
                    },
                    HeightRequest = 84,
                };
                view = bar;
                _toolbarUpdates.Add(bar.Refresh);
                _toolbarCleanup.Add(() => bar.Owner = null);
            }
            group.Add(view);
            var itemView = view;
            _toolbarUpdates.Add(() => itemView.IsVisible = IsToolbarItemVisible(item));
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
        var cell = new Grid { MinimumHeightRequest = 44, Padding = new Thickness(2, 0) };
        Layout content = navigation ? new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 2, Padding = new Thickness(4, 0),
            VerticalOptions = LayoutOptions.Center,
        } : new HorizontalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var icon = navigation ? MenuIcon(null, NavigationGeometry) : key switch
        {
            OrientedImageryToolbarCategory.ModeCategoryName => ImageModeIcon(ImageSearchEnabled),
            OrientedImageryToolbarCategory.OverlaysCategoryName => MenuIcon(null, OverlayGeometry),
            OrientedImageryToolbarCategory.MarkersCategoryName => MenuIcon("\uE8ED", null),
            _ => new Label { Text = "\u2022\u2022\u2022", TextColor = Color.FromArgb("#007AC2") },
        };
        icon.WidthRequest = icon.HeightRequest = navigation ? 14 : 16;
        var iconHost = new Grid { WidthRequest = icon.WidthRequest, HeightRequest = icon.HeightRequest };
        iconHost.Add(icon);
        content.Add(iconHost);
        var label = new Label
        {
            Text = navigation ? Text("Navigation") : known ? Text(key) : key,
            FontSize = navigation ? 11 : 12, VerticalOptions = LayoutOptions.Center, MaxLines = 1,
            HorizontalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.TailTruncation,
        };
        label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
#if ANDROID
        label.HandlerChanged += (sender, _) =>
        {
            if (sender is Label { Handler.PlatformView: Android.Widget.TextView nativeLabel })
                nativeLabel.Typeface = Android.Graphics.Typeface.Create(nativeLabel.Typeface, 600, false);
        };
#endif
        Grid.SetColumn(label, 1);
        content.Add(label);
        var pill = new Border
        {
            Content = content, HeightRequest = 26, VerticalOptions = LayoutOptions.Center,
            Stroke = Color.FromArgb("#CCCCCC"), StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 13 }, InputTransparent = true,
        };
#if __IOS__ && !MACCATALYST
        var vertical = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        content.Remove(iconHost);
        content.Remove(label);
        vertical.Add(iconHost);
        vertical.Add(label);
        pill.Content = vertical;
        cell.MinimumHeightRequest = 60;
        icon.WidthRequest = icon.HeightRequest = 22;
        iconHost.WidthRequest = iconHost.HeightRequest = 22;
        label.FontSize = 11;
        label.FontAttributes = FontAttributes.Bold;
        label.TextColor = IosAccent;
        pill.HeightRequest = -1;
        pill.VerticalOptions = LayoutOptions.Fill;
        pill.Margin = new Thickness(0, 4, 0, 8);
        pill.StrokeThickness = 0;
        pill.StrokeShape = new RoundRectangle { CornerRadius = 10 };
        pill.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#3A3A3C"));
#endif
        var button = new Button
        {
            BackgroundColor = Colors.Transparent, BorderWidth = 0, Padding = 0, MinimumHeightRequest = cell.MinimumHeightRequest,
            AutomationId = "OrientedImagery" + (navigation ? "Navigation" : key),
        };
        SemanticProperties.SetDescription(button, label.Text);
        cell.Add(pill);
        cell.Add(button);
        FlexLayout.SetGrow(cell, 1);
        FlexLayout.SetBasis(cell, new FlexBasis(0.25f, true));
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
            bool active = _panelAnchor == button ||
                (key == OrientedImageryToolbarCategory.ModeCategoryName &&
                    ViewModel.InteractionState is OrientedImageryViewInteractionState.ExploreImages or OrientedImageryViewInteractionState.ExamineImage) ||
                (key == OrientedImageryToolbarCategory.MarkersCategoryName && IsAddingMarkers) ||
                (key == OrientedImageryToolbarCategory.MoreCategoryName && ViewModel.IsSequentialNavigationEnabled);
            pill.Opacity = button.IsEnabled ? 1 : 0.45;
            bool modeChanged = key == OrientedImageryToolbarCategory.ModeCategoryName && previousSearch != ImageSearchEnabled;
            if (modeChanged)
            {
                iconHost.Clear();
                icon = ImageModeIcon(ImageSearchEnabled);
                icon.WidthRequest = iconHost.WidthRequest;
                icon.HeightRequest = iconHost.HeightRequest;
                iconHost.Add(icon);
            }
            if (previousActive != active || modeChanged)
            {
                var accent = Color.FromArgb("#007AC2");
#if __IOS__ && !MACCATALYST
                accent = IosAccent;
#endif
                pill.BackgroundColor = active ? accent : Colors.Transparent;
                if (active)
                    label.TextColor = Colors.White;
                else
                    label.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
                if (icon is Image { Source: FontImageSource font })
                    font.Color = active ? Colors.White : accent;
                if (icon is Microsoft.Maui.Controls.Shapes.Path path)
                    path.Fill = active ? Colors.White : accent;
                if (icon is Label moreIcon)
                    moreIcon.TextColor = active ? Colors.White : accent;
#if __IOS__ && !MACCATALYST
                label.TextColor = active ? Colors.White : IosAccent;
                if (!active)
                    pill.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#3A3A3C"));
#endif
            }
            if (key == OrientedImageryToolbarCategory.ModeCategoryName)
                SemanticProperties.SetHint(button, $"{Text(ImageSearchEnabled ? "SearchMode" : "SelectMode")}, {Text(active ? "On" : "Off")}");
            else if (key == OrientedImageryToolbarCategory.MarkersCategoryName)
                SemanticProperties.SetHint(button, Text(IsAddingMarkers ? "On" : "Off"));
            else if (navigation)
                SemanticProperties.SetHint(button, Text(ViewModel.IsSequentialNavigationEnabled ? "On" : "Off"));
            previousActive = active;
            previousSearch = ImageSearchEnabled;
        });
        return cell;
#endif
    }

#if !WINDOWS
    private void ShowToolbarCategory(OrientedImageryToolbarCategory category, View anchor, string title)
    {
        DismissPanel();
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
                foreach (var state in mode.ImageSearchModes)
                {
                    bool search = state == OrientedImageryViewInteractionState.ExploreImages;
                    AddActionRow(content, new MenuAction(Text(search ? "SearchMode" : "SelectMode"),
                        () => mode.SelectedInteractionState = state, Icon: () => ImageModeIcon(search),
                        Checked: () => ViewModel.InteractionState == state, Enabled: Enabled));
                }
                break;
            case ShowSelectedFootprintVM:
                AddSwitch(content, "MobileCurrentFootprint", () => ViewModel.ShowSelectedFootprint, value => ViewModel.ShowSelectedFootprint = value, () => FootprintIcon(false), Enabled);
                break;
            case ShowUnselectedFootprintsVM:
                AddSwitch(content, "MobileAdditionalFootprints", () => ViewModel.ShowUnselectedFootprints, value => ViewModel.ShowUnselectedFootprints = value,
                    () => FootprintIcon(true), () => Enabled() && ViewModel.Images.Count > 1);
                break;
            case ShowCameraMarkersVM:
                AddSwitch(content, "MobileCameraLocations", () => ViewModel.ShowCameraLocations, value => ViewModel.ShowCameraLocations = value,
                    CameraLocationsIcon, () => Enabled() && ViewModel.Images.Count > 1);
                break;
            case AllowAddingMarkersVM:
                AddSwitch(content, "AddMarker", () => IsAddingMarkers, SetAddingMarkers, () => MenuIcon("\uE8ED", null), Enabled);
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
                    Enabled: () => Enabled() && ViewModel.ClearMarkersCommand.CanExecute(null), Icon: () => MenuIcon(ToolkitIcons.Trash, null)));
                break;
            case SequentialNavigationVM:
                AddSwitch(content, "SequentialMode", () => ViewModel.IsSequentialNavigationEnabled,
                    value =>
                    {
                        if (value != ViewModel.IsSequentialNavigationEnabled && ViewModel.ToggleSequentialNavigationCommand.CanExecute(null))
                            ViewModel.ToggleSequentialNavigationCommand.Execute(null);
                    },
                    () => MenuIcon(null, SequenceGeometry),
                    () => Enabled() && ViewModel.ToggleSequentialNavigationCommand.CanExecute(null));
                break;
            case AutoUpdateFootprintVM:
                AddSwitch(content, "AutoUpdate", () => ViewModel.AutoUpdateFootprint, value => ViewModel.AutoUpdateFootprint = value,
                    () => MenuIcon(ToolkitIcons.Refresh, null), Enabled);
                break;
            case ToolbarDividerVM:
                content.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#CCCCCC"), Margin = new Thickness(0, 4) });
                break;
            default:
                throw new InvalidOperationException($"Register a toolbar template for '{item.GetType().Name}'.");
        }
    }
#endif
}
