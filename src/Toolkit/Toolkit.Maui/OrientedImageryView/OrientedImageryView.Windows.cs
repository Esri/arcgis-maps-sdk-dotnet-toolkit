#if WINDOWS
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Internals;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Grid = Microsoft.Maui.Controls.Grid;
using View = Microsoft.Maui.Controls.View;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private Picker? _layerPicker;
    private DesktopImageryBar? _desktopToolbar;
    private DesktopImageryBar? _desktopNavigation;
    private Microsoft.UI.Xaml.FrameworkElement? _nativeView;

    private static View CreateDesktopTemplate()
    {
        var root = new Grid
        {
            RowSpacing = 0,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
        };
        root.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        INameScope scope = new NameScope();
        NameScope.SetNameScope(root, scope);
        void Add(string name, View view, Grid parent, int row = 0, int column = 0)
        {
            scope.RegisterName(name, view);
            Grid.SetRow(view, row);
            Grid.SetColumn(view, column);
            parent.Children.Add(view);
        }
        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star) },
            Padding = 8,
        };
        root.Children.Add(header);
        var picker = new Picker { MinimumHeightRequest = 40, AutomationId = "OrientedImageryLayerPicker" };
        SemanticProperties.SetDescription(picker, Text("SelectLayer"));
        picker.ItemDisplayBinding = Binding.Create(static (OrientedImageryLayer layer) => layer.Name);
        Add("PART_LayerPicker", picker, header);
        var separator = new BoxView { HeightRequest = 1, VerticalOptions = LayoutOptions.End, InputTransparent = true };
        separator.SetAppThemeColor(BoxView.ColorProperty, Color.FromArgb("#DDDDDD"), Color.FromArgb("#444444"));
        root.Children.Add(separator);
        Add("PART_DesktopToolbar", new DesktopImageryBar { IsNavigation = false }, root, 1);
        var custom = new HorizontalStackLayout { Spacing = 0 };
        scope.RegisterName("PART_CustomToolbar", custom);
        Add("PART_CustomToolbarScroll", new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = custom }, root, 1);

        var image = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        };
        Grid.SetRow(image, 2);
        root.Children.Add(image);
        var viewport = new OrientedImageViewport { AutomationId = "OrientedImageryViewport" };
        Add("PART_Viewport", viewport, image);
        Grid.SetColumnSpan(viewport, 3);
        var status = new Label
        {
            Margin = 16, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center, AutomationId = "OrientedImageryStatus",
        };
        status.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        Add("PART_Status", status, image);
        Grid.SetColumnSpan(status, 3);
        var busy = new ActivityIndicator { VerticalOptions = LayoutOptions.Start, HeightRequest = 24, AutomationId = "OrientedImageryBusy" };
        Add("PART_Busy", busy, image);
        Grid.SetColumnSpan(busy, 3);
        Add("PART_Previous", DesktopImageArrow(false), image);
        Add("PART_Next", DesktopImageArrow(true), image, column: 2);
        var panel = new Grid { IsVisible = false };
        Add("PART_PanelHost", panel, image);
        Grid.SetColumnSpan(panel, 3);
        Add("PART_DesktopNavigation", new DesktopImageryBar { IsNavigation = true }, root, 3);
        var notice = new Label { HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(8, 4), AutomationId = "OrientedImageryNotice" };
        notice.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        Add("PART_Notice", notice, root, 4);
        return root;
    }

    private static Microsoft.Maui.Controls.Button DesktopImageArrow(bool next)
    {
        var button = new Microsoft.Maui.Controls.Button
        {
            ImageSource = new FontImageSource { Glyph = next ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronLeft, FontFamily = ToolkitIcons.FontFamilyName, Size = 20 },
            WidthRequest = 36, HeightRequest = 48, Padding = 4, BorderWidth = 0, CornerRadius = 0,
            VerticalOptions = LayoutOptions.Center, BackgroundColor = Colors.Transparent,
            AutomationId = next ? "OrientedImageryNext" : "OrientedImageryPrevious",
        };
        SemanticProperties.SetDescription(button, Text(next ? "NextLabel" : "PreviousLabel"));
        ToolTipProperties.SetText(button, Text(next ? "NextLabel" : "PreviousLabel"));
        button.SetAppThemeColor(Microsoft.Maui.Controls.Button.TextColorProperty, Colors.Black, Colors.White);
        if (button.ImageSource is FontImageSource icon)
            icon.SetAppThemeColor(FontImageSource.ColorProperty, Colors.Black, Colors.White);
        return button;
    }

    private void NativeKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && DismissTools()) e.Handled = true;
    }

    partial void DetachPlatformPresentation()
    {
        if (_nativeView != null) _nativeView.KeyDown -= NativeKeyDown;
        _nativeView = null;
        if (_desktopToolbar != null) _desktopToolbar.Owner = null;
        if (_desktopNavigation != null) _desktopNavigation.Owner = null;
    }

    partial void AttachPlatformPresentation()
    {
        _nativeView = Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (_nativeView != null) _nativeView.KeyDown += NativeKeyDown;
    }

    private void ReleaseDesktopTemplate()
    {
        if (_layerPicker != null)
        {
            _layerPicker.SelectedIndexChanged -= DesktopLayerSelected;
            _layerPicker.ItemsSource = null;
        }
        _layerPicker = null;
        if (_desktopToolbar != null) _desktopToolbar.Owner = null;
        if (_desktopNavigation != null) _desktopNavigation.Owner = null;
        _desktopToolbar = null;
        _desktopNavigation = null;
    }

    private void InitializeDesktopTemplate()
    {
        _layerPicker = GetTemplateChild("PART_LayerPicker") as Picker;
        if (_layerPicker != null)
        {
            _layerPicker.ItemsSource = _layers;
            _layerPicker.SelectedIndexChanged += DesktopLayerSelected;
        }
        _desktopToolbar = GetTemplateChild("PART_DesktopToolbar") as DesktopImageryBar;
        _desktopNavigation = GetTemplateChild("PART_DesktopNavigation") as DesktopImageryBar;
    }

    partial void RefreshDesktopPresentation()
    {
        if (_layerPicker != null) _layerPicker.SelectedItem = SelectedLayer;
        if (_desktopToolbar != null)
        {
            _desktopToolbar.IsVisible = ToolbarItemTemplateSelector == null;
            if (_desktopToolbar.Owner != this) _desktopToolbar.Owner = this;
            else _desktopToolbar.Refresh();
        }
        if (_desktopNavigation != null)
        {
            _desktopNavigation.IsVisible = ViewModel.SelectedImage != null;
            if (_desktopNavigation.Owner != this) _desktopNavigation.Owner = this;
            else _desktopNavigation.Refresh();
        }
    }

    private void DesktopLayerSelected(object? sender, EventArgs e)
    {
        if (_layerPicker?.SelectedItem is OrientedImageryLayer layer && layer != SelectedLayer)
            SelectedLayer = layer;
    }

    internal void ReportDesktopError(Exception error)
    {
        _operationError = error;
        Refresh();
    }

    internal bool DesktopIsSearching => _searching;
    internal bool DesktopHasLayer => SelectedLayer != null && _layers.Contains(SelectedLayer);
    internal static string DesktopText(string key) => Text(key);
}
#endif
