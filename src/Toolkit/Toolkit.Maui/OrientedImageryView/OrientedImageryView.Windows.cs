#if WINDOWS
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Layouts;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Grid = Microsoft.Maui.Controls.Grid;
using View = Microsoft.Maui.Controls.View;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private DesktopImageryBar? _desktopNavigation;
    private Microsoft.UI.Xaml.FrameworkElement? _nativeView;

    private static View CreateDesktopTemplate()
    {
        var root = new Grid
        {
            RowSpacing = 0,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
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
        var custom = new ToolbarWrapLayout { Margin = 8, CenterRows = true };
        Add("PART_CustomToolbar", custom, root);

        var image = new Grid
        {
            MinimumHeightRequest = MinimumImageAreaHeight,
        };
        Grid.SetRow(image, 1);
        root.Children.Add(image);
        var viewport = new OrientedImageViewport { AutomationId = "OrientedImageryViewport" };
        Add("PART_Viewport", viewport, image);
        AddStatusPresentation(image, scope);
        var busy = new ActivityIndicator { VerticalOptions = LayoutOptions.Start, HeightRequest = 24, AutomationId = "OrientedImageryBusy" };
        Add("PART_Busy", busy, image);
        Add("PART_Previous", DesktopImageArrow(false), image);
        Add("PART_Next", DesktopImageArrow(true), image);
        AddPopupPresentation(image, scope);
        var panel = new Grid { IsVisible = false };
        Add("PART_PanelHost", panel, image);
        Add("PART_DesktopNavigation", new DesktopImageryBar(), root, 2);
        var notice = new Label { HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(8, 4), AutomationId = "OrientedImageryNotice" };
        notice.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
        Add("PART_Notice", notice, root, 3);
        return ScrollablePresentation(root, image, scope);
    }

    private static Microsoft.Maui.Controls.Button DesktopImageArrow(bool next)
    {
        var button = new Microsoft.Maui.Controls.Button
        {
            Text = next ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronLeft,
            FontFamily = ToolkitIcons.FontFamilyName, FontSize = 20,
            WidthRequest = 25, HeightRequest = 60, MinimumWidthRequest = 0, Padding = 4, BorderWidth = 0, CornerRadius = 0,
            VerticalOptions = LayoutOptions.Center, BackgroundColor = Colors.Transparent,
            HorizontalOptions = next ? LayoutOptions.End : LayoutOptions.Start,
            AutomationId = next ? "OrientedImageryNext" : "OrientedImageryPrevious",
        };
        SemanticProperties.SetDescription(button, Text(next ? "NextLabel" : "PreviousLabel"));
        ToolTipProperties.SetText(button, Text(next ? "NextLabel" : "PreviousLabel"));
        button.SetAppThemeColor(Microsoft.Maui.Controls.Button.TextColorProperty, Colors.Black, Colors.White);
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
        if (_desktopNavigation != null) _desktopNavigation.Owner = null;
    }

    partial void AttachPlatformPresentation()
    {
        _nativeView = Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (_nativeView != null) _nativeView.KeyDown += NativeKeyDown;
    }

    private void ReleaseDesktopTemplate()
    {
        if (_desktopNavigation != null) _desktopNavigation.Owner = null;
        _desktopNavigation = null;
    }

    private void InitializeDesktopTemplate()
    {
        _desktopNavigation = GetTemplateChild("PART_DesktopNavigation") as DesktopImageryBar;
    }

    partial void RefreshDesktopPresentation()
    {
        if (_desktopNavigation != null)
        {
            _desktopNavigation.IsVisible = ViewModel.SelectedImage != null;
            if (_desktopNavigation.Owner != this) _desktopNavigation.Owner = this;
            else _desktopNavigation.Refresh();
        }
    }

    internal void ReportDesktopError(Exception error)
    {
        _operationError = error;
        Refresh();
    }

    internal bool DesktopIsSearching => _searching;
    internal bool DesktopHasLayer => SelectedLayer != null && _layers.Contains(SelectedLayer);
    internal static string DesktopText(string key) => Text(key);

    private sealed class ToolbarWrapLayout : Layout
    {
        public bool CenterRows { get; init; }

        protected override ILayoutManager CreateLayoutManager() => new ToolbarWrapLayoutManager(this);
    }

    private sealed class ToolbarWrapLayoutManager(ToolbarWrapLayout layout) : ILayoutManager
    {
        // WinUI can round the arranged width down by a fraction of a DIP.
        private const double WrapTolerance = 1;
        private readonly Dictionary<IView, Microsoft.Maui.Graphics.Size> _sizes = new();

        private static bool IsDivider(IView child) =>
            child is BoxView or DesktopImageryBar { ToolbarItem: ToolbarDividerVM };

        public Microsoft.Maui.Graphics.Size Measure(double widthConstraint, double heightConstraint)
        {
            _sizes.Clear();
            double availableWidth = Math.Max(0, widthConstraint - layout.Padding.HorizontalThickness);
            double width = 0, height = 0, rowWidth = 0, rowHeight = 0;
            bool onlyDividers = true;
            foreach (var child in layout.Children)
            {
                if (child.Visibility == Visibility.Collapsed)
                    continue;
                var size = child.Measure(double.PositiveInfinity, heightConstraint);
                double itemWidth = onlyDividers ? Math.Max(0, availableWidth - rowWidth) : availableWidth;
                if (size.Width > itemWidth)
                    size = child.Measure(itemWidth, heightConstraint);
                _sizes.Add(child, size);
                if (rowWidth > 0 && rowWidth + size.Width > availableWidth + WrapTolerance)
                {
                    width = Math.Max(width, rowWidth);
                    height += rowHeight;
                    rowWidth = rowHeight = 0;
                    onlyDividers = true;
                }
                rowWidth += Math.Min(availableWidth, size.Width);
                rowHeight = Math.Max(rowHeight, size.Height);
                onlyDividers &= IsDivider(child);
            }
            return new Microsoft.Maui.Graphics.Size(
                Math.Max(width, rowWidth) + layout.Padding.HorizontalThickness,
                height + rowHeight + layout.Padding.VerticalThickness);
        }

        public Microsoft.Maui.Graphics.Size ArrangeChildren(Microsoft.Maui.Graphics.Rect bounds)
        {
            double availableWidth = Math.Max(0, bounds.Width - layout.Padding.HorizontalThickness);
            double y = bounds.Y + layout.Padding.Top, rowWidth = 0, rowHeight = 0;
            var row = new List<IView>();
            void ArrangeRow()
            {
                double x = bounds.X + layout.Padding.Left +
                    (layout.CenterRows ? Math.Max(0, availableWidth - rowWidth) / 2 : 0);
                foreach (var child in row)
                {
                    var size = _sizes[child];
                    double width = Math.Min(availableWidth, size.Width);
                    child.Arrange(new Microsoft.Maui.Graphics.Rect(x, y, width, rowHeight));
                    x += width;
                }
                y += rowHeight;
                row.Clear();
                rowWidth = rowHeight = 0;
            }
            foreach (var child in layout.Children)
            {
                if (child.Visibility == Visibility.Collapsed || !_sizes.TryGetValue(child, out var size))
                    continue;
                double width = Math.Min(availableWidth, size.Width);
                if (row.Count > 0 && rowWidth + width > availableWidth + WrapTolerance)
                    ArrangeRow();
                row.Add(child);
                rowWidth += width;
                rowHeight = Math.Max(rowHeight, size.Height);
            }
            ArrangeRow();
            return bounds.Size;
        }
    }
}
#endif
