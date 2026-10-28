#if WINDOWS
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Layouts;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.ViewManagement;
using FocusState = Microsoft.UI.Xaml.FocusState;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    #region Native presentation and template lifecycle

    private OrientedImageryControlsView? _desktopPagination;
    private Microsoft.UI.Xaml.FrameworkElement? _nativeView;
    private AccessibilitySettings? _desktopAccessibilitySettings;
    private UISettings? _desktopUiSettings;
    private Grid? _desktopRoot;

    private static Microsoft.Maui.Controls.Button DesktopImageArrow(bool next)
    {
        var button = new Microsoft.Maui.Controls.Button
        {
            Text = next ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronLeft,
            FontFamily = ToolkitIcons.FontFamilyName, FontSize = 20,
            WidthRequest = 25, HeightRequest = 60, MinimumWidthRequest = 0, Padding = 4, BorderWidth = 0, CornerRadius = 0,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = next ? LayoutOptions.End : LayoutOptions.Start,
            AutomationId = next ? "OrientedImageryNext" : "OrientedImageryPrevious",
        };
        SemanticProperties.SetDescription(button, Text(next ? "NextLabel" : "PreviousLabel"));
        ToolTipProperties.SetText(button, Text(next ? "NextLabel" : "PreviousLabel"));
        button.SetAppThemeColor(Microsoft.Maui.Controls.Button.TextColorProperty, Colors.Black, Colors.White);
        button.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        button.HandlerChanged += (_, _) =>
        {
            if (button.Handler?.PlatformView is NativeButton native)
                native.AllowFocusWhenDisabled = true;
        };
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
        if (_desktopUiSettings != null)
            _desktopUiSettings.ColorValuesChanged -= DesktopColorsChanged;
        _desktopUiSettings = null;
        _desktopAccessibilitySettings = null;
        if (_desktopPagination != null) _desktopPagination.Owner = null;
    }

    partial void AttachPlatformPresentation()
    {
        _nativeView = Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (_nativeView != null) _nativeView.KeyDown += NativeKeyDown;
        _desktopAccessibilitySettings = new AccessibilitySettings();
        _desktopUiSettings = new UISettings();
        _desktopUiSettings.ColorValuesChanged += DesktopColorsChanged;
        UpdateDesktopColors();
    }

    private void ReleaseDesktopTemplate()
    {
        if (_desktopPagination != null) _desktopPagination.Owner = null;
        _desktopPagination = null;
        _desktopRoot = null;
    }

    private void InitializeDesktopTemplate()
    {
        _desktopPagination = GetTemplateChild("PART_DesktopNavigation") as OrientedImageryControlsView;
        _desktopRoot = ControlTemplate == DefaultControlTemplate ? _customToolbar?.Parent as Grid : null;
        UpdateDesktopColors();
    }

    partial void RefreshDesktopPresentation()
    {
        if (_desktopPagination != null)
        {
            _desktopPagination.IsVisible = ViewModel.SelectedImage != null;
            if (_desktopPagination.Owner != this) _desktopPagination.Owner = this;
            else _desktopPagination.Refresh();
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

    #endregion

    #region Keyboard focus

    private void FocusDesktopPopup()
    {
        if (_popupCloseButton is not { } target || _nativeView?.XamlRoot is not { } root) return;
        var popup = ViewModel.SelectedImagePopup;
        var expectedFocus = FocusManager.GetFocusedElement(root);
        Dispatcher.Dispatch(() =>
        {
            if (_connected && popup != null && popup == ViewModel.SelectedImagePopup &&
                target == _popupCloseButton && _popupHost?.IsVisible == true &&
                ReferenceEquals(FocusManager.GetFocusedElement(root), expectedFocus) &&
                target.Handler?.PlatformView is NativeButton { IsLoaded: true } button)
                button.Focus(FocusState.Keyboard);
        });
    }

    #endregion

    #region Theme colors

    private void DesktopColorsChanged(UISettings sender, object args) => Dispatcher.Dispatch(UpdateDesktopColors);

    private void UpdateDesktopColors()
    {
        bool highContrast = _desktopAccessibilitySettings?.HighContrast == true && _desktopUiSettings != null;
        Color? SystemColor(UIColorType type)
        {
            if (!highContrast) return null;
            var color = _desktopUiSettings!.GetColorValue(type);
            return Color.FromRgba(color.R, color.G, color.B, color.A);
        }

        var background = SystemColor(UIColorType.Background);
        var foreground = SystemColor(UIColorType.Foreground);
        void SetBackground(BindableObject target, BindableProperty property)
        {
            if (background != null)
                target.SetValue(property, background);
            else
                target.SetAppThemeColor(property, Colors.White, Color.FromArgb("#202020"));
        }

        SetBackground(this, ThemeImageBackgroundProperty);
        if (_desktopRoot == null) return;

        // Native high-contrast icons need the matching system surface behind them.
        SetBackground(_desktopRoot, BackgroundColorProperty);
        foreach (var button in new[] { _previousButton, _nextButton })
        {
            if (button == null) continue;
            SetBackground(button, BackgroundColorProperty);
            if (foreground != null)
                button.TextColor = foreground;
            else
                button.SetAppThemeColor(Microsoft.Maui.Controls.Button.TextColorProperty, Colors.Black, Colors.White);
        }

        if (_popupOpenButton is { ImageSource: FontImageSource infoIcon } infoButton)
        {
            SetBackground(infoButton, BackgroundColorProperty);
            if (foreground != null)
                infoIcon.Color = foreground;
            else
                infoIcon.SetAppThemeColor(FontImageSource.ColorProperty, Color.FromArgb("#007AC2"), Color.FromArgb("#6CB8EC"));
        }
    }

    #endregion

    #region Wrapping toolbar layout

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
            child is BoxView or OrientedImageryControlsView { ToolbarItem: ToolbarDividerVM };

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

    #endregion
}
#endif
