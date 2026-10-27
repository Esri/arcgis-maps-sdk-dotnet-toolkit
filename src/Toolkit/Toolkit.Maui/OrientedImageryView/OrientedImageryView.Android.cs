#if ANDROID
using AndroidX.Activity;
using Google.Android.Material.BottomSheet;
using Microsoft.Maui.Platform;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private WeakReference<ToolsBackCallback>? _backCallback;
    private BottomSheetDialog? _toolsSheet;
    private ContentView? _toolsSheetContent;
    private BottomSheetDialog? _infoSheet;
    private ContentView? _infoSheetContent;

    partial void AttachPlatformPresentation() => AttachBackNavigation();

    private void AddAndroidModeSegments(VerticalStackLayout content, ImageSearchModeVM mode, Func<bool> enabled)
    {
        var segments = new Grid { ColumnSpacing = 1 };
        foreach (var state in mode.ImageSearchModes)
        {
            string title = Text(state == OrientedImageryViewInteractionState.FindImages ? "ImageSearchModeFind" : "ImageSearchModeDisplay");
            var action = GuardAction(new MenuAction(title, () => ToggleImageMode(mode, state),
                Icon: () => ImageModeIcon(state == OrientedImageryViewInteractionState.FindImages),
                Checked: () => ViewModel.InteractionState == state, Enabled: enabled), dismiss: false);
            var button = new Button
            {
                CornerRadius = 0, BorderWidth = 0,
                MinimumHeightRequest = 48, Padding = 0,
            };
            var segment = new Grid();
            segment.Add(button);
            var icon = action.Icon();
            icon.WidthRequest = icon.HeightRequest = 24;
            var check = new Label { Text = "\u2713", VerticalOptions = LayoutOptions.Center };
            var label = new Label { Text = title, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap };
            var caption = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 6, Padding = new Thickness(8, 4),
                VerticalOptions = LayoutOptions.Center, InputTransparent = true,
            };
            caption.Add(check);
            caption.Add(icon, 1);
            caption.Add(label, 2);
            segment.Add(caption);
            SemanticProperties.SetDescription(button, title);
            button.Clicked += (_, _) => action.Execute();
            segments.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            segments.Add(segment, segments.ColumnDefinitions.Count - 1);
            _panelUpdates.Add(() =>
            {
                bool selected = action.Checked?.Invoke() == true;
                bool dark = Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
                check.IsVisible = selected;
                button.IsEnabled = action.Enabled();
                caption.Opacity = button.IsEnabled ? 1 : 0.45;
                button.BackgroundColor = Color.FromArgb(selected ? (dark ? "#4F378B" : "#EADDFF") : (dark ? "#211F26" : "#FFFBFE"));
                label.TextColor = check.TextColor = Color.FromArgb(dark ? "#E6E1E5" : "#1D192B");
                SemanticProperties.SetHint(button, Text(selected ? "Selected" : "NotSelected"));
            });
        }
        var outline = new Border
        {
            Content = segments, Margin = new Thickness(0, 8),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            StrokeThickness = 1,
        };
        outline.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#79747E"), Color.FromArgb("#938F99"));
        segments.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#79747E"), Color.FromArgb("#938F99"));
        content.Add(outline);
    }

    partial void DetachPlatformPresentation()
    {
        DismissPanel();
        DismissAndroidPopup();
        DetachBackNavigation();
    }

    private void PresentAndroidPanel(View anchor, VerticalStackLayout content, Action? back)
    {
        var context = Handler?.MauiContext ??
            throw new InvalidOperationException("The oriented imagery view must have a handler before opening its tools.");
        _panelAnchor = anchor;
        var heading = (Label)content.Children[0];
        content.Remove(heading);
        content.Padding = new Thickness(16, 0, 16, 16);
        _panelScroll = new ScrollView { Content = content };
        _toolsSheetContent = CreateAndroidSheetContent(heading, _panelScroll, () => DismissPanel(), back, out _panelLayout, "OrientedImageryToolsSheet");
        AddLogicalChild(_toolsSheetContent);
        var nativeContent = _toolsSheetContent.ToPlatform(context);
        if (_toolsSheet == null)
        {
            var dialog = new BottomSheetDialog(context.Context);
            _toolsSheet = dialog;
            dialog.SetCanceledOnTouchOutside(true);
            dialog.DismissEvent += (_, _) =>
            {
                if (_toolsSheet == dialog)
                    DismissPanel();
            };
        }
        _toolsSheet.SetContentView(nativeContent);
        UpdateAndroidPanelSize();
        ShowAndroidSheet(_toolsSheet, nativeContent, width =>
        {
            if (_toolsSheetContent != null)
                UpdateAndroidPanelSize(width);
        });
        UpdateToolHighlights();
        UpdateBackNavigation();
        UpdateOpenPanel();
        SemanticScreenReader.Default.Announce(heading.Text);
    }

    private static ContentView CreateAndroidSheetContent(Label heading, View body, Action dismiss, Action? back, out Grid layout, string automationId)
    {
        heading.FontSize = 22;
        heading.Margin = 0;
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);
        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Padding = new Thickness(12, 4), ColumnSpacing = 4,
        };
        header.Add(heading, 1);
        if (back != null)
        {
            var previous = ToolButton("MobileBackToMarkers", ToolkitIcons.ChevronLeft, "OrientedImageryBackToMarkers");
            previous.Text = null;
            previous.MinimumWidthRequest = 44;
            previous.BorderWidth = 0;
            previous.Clicked += (_, _) => back();
            header.Add(previous);
        }
        var close = ToolButton("ClosePanel", "", "OrientedImageryPanelClose");
        close.Text = "X";
        close.MinimumWidthRequest = 44;
        close.BorderWidth = 0;
        close.Clicked += (_, _) => dismiss();
        header.Add(close, 2);
        var handle = new Border
        {
            WidthRequest = 32, HeightRequest = 4, StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 2 },
            Margin = new Thickness(0, 10, 0, 6), HorizontalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        handle.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#79747E"), Color.FromArgb("#938F99"));
        layout = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
        };
        layout.Add(handle);
        layout.Add(header, 0, 1);
        layout.Add(body, 0, 2);
        var surface = new Border
        {
            Content = layout, StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
        };
        surface.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#FFFBFE"), Color.FromArgb("#211F26"));
        return new ContentView
        {
            Content = surface, AutomationId = automationId, BackgroundColor = Colors.Transparent,
        };
    }

    private static void ShowAndroidSheet(BottomSheetDialog dialog, Android.Views.View nativeContent, Action<double> updateWidth)
    {
        dialog.Show();
        dialog.Window?.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
        if (nativeContent.Parent is Android.Views.View sheet)
        {
            sheet.SetBackgroundColor(Android.Graphics.Color.Transparent);
            double density = sheet.Resources!.DisplayMetrics!.Density;
            sheet.LayoutChange += (_, args) =>
            {
                if (args.Right > args.Left && args.Right - args.Left != args.OldRight - args.OldLeft)
                    updateWidth((args.Right - args.Left) / density);
            };
            if (sheet.Width > 0)
                updateWidth(sheet.Width / density);
        }
        dialog.Behavior.State = BottomSheetBehavior.StateExpanded;
        dialog.Behavior.SkipCollapsed = true;
    }

    private void RefreshAndroidPopup()
    {
        // A custom template with its own popup host retains ownership of its presentation.
        if (_popupHost != null) return;
        if (ViewModel.SelectedImagePopup == null)
        {
            DismissAndroidPopup(closePopup: false);
            return;
        }
        if (_popupViewer == null || !IsLoaded || Handler?.MauiContext is not { } context) return;
        _popupViewer.Popup = ViewModel.SelectedImagePopup;
        _popupViewer.IsVisible = true;
        if (_infoSheet != null) return;
        DismissPanel();
        var heading = new Label { Text = Text("ImageDetails") };
        _infoSheetContent = CreateAndroidSheetContent(heading, _popupViewer, () => DismissAndroidPopup(), null, out _, "OrientedImageryInfoSheet");
        AddLogicalChild(_infoSheetContent);
        var nativeContent = _infoSheetContent.ToPlatform(context);
        using var frame = new Android.Graphics.Rect();
        context.Context.GetActivity()?.Window?.DecorView?.GetWindowVisibleDisplayFrame(frame);
        double density = nativeContent.Resources!.DisplayMetrics!.Density;
        _infoSheetContent.WidthRequest = frame.Width() / density;
        _infoSheetContent.HeightRequest = frame.Height() / density * 0.75;
        var dialog = new BottomSheetDialog(context.Context);
        _infoSheet = dialog;
        dialog.SetCanceledOnTouchOutside(true);
        dialog.SetContentView(nativeContent);
        dialog.DismissEvent += (_, _) =>
        {
            if (_infoSheet == dialog)
                DismissAndroidPopup();
        };
        ShowAndroidSheet(dialog, nativeContent, width =>
        {
            if (_infoSheetContent != null)
                _infoSheetContent.WidthRequest = width;
        });
        UpdateBackNavigation();
        SemanticScreenReader.Default.Announce(heading.Text);
    }

    private void DismissAndroidPopup(bool closePopup = true)
    {
        var dialog = _infoSheet;
        _infoSheet = null;
        if (_infoSheetContent is { } content)
        {
            _infoSheetContent = null;
            if (_popupViewer?.Parent is Grid layout) layout.Remove(_popupViewer);
            content.Handler?.DisconnectHandler();
            RemoveLogicalChild(content);
        }
        if (dialog != null)
        {
            dialog.Dismiss();
            dialog.Dispose();
        }
        if (closePopup && ViewModel.SelectedImagePopup != null)
            ViewModel.CloseSelectedImagePopupCommand.Execute(null);
        UpdateBackNavigation();
    }

    private void UpdateAndroidPanelSize(double? availableWidth = null)
    {
        if (_toolsSheetContent == null || _panelLayout == null || _panelScroll == null) return;
        var activity = Handler?.MauiContext?.Context?.GetActivity();
        var decor = activity?.Window?.DecorView;
        if (decor == null) return;
        using var frame = new Android.Graphics.Rect();
        decor.GetWindowVisibleDisplayFrame(frame);
        double density = decor.Resources!.DisplayMetrics!.Density;
        double width = availableWidth ?? (_toolsSheetContent.Handler?.PlatformView is Android.Views.View native &&
            native.Parent is Android.Views.View sheet && sheet.Width > 0 ? sheet.Width / density : frame.Width() / density);
        double maximumHeight = frame.Height() / density * 0.85;
        if (width <= 0 || maximumHeight <= 0) return;
        double contentWidth = Math.Max(0, width - 32);
        if (_symbolGrid != null) LayoutSymbolGrid(contentWidth);
        double headerHeight = _panelLayout.Children.Take(2).Sum(child => child.Measure(width, double.PositiveInfinity).Height);
        double bodyHeight = _panelScroll.Content?.Measure(contentWidth, double.PositiveInfinity).Height ?? 0;
        _toolsSheetContent.WidthRequest = width;
        _toolsSheetContent.HeightRequest = Math.Min(maximumHeight, headerHeight + bodyHeight);
    }

    private void DismissAndroidPanel(bool preservePresentation)
    {
        if (_toolsSheetContent != null)
        {
            var content = _toolsSheetContent;
            _toolsSheetContent = null;
            content.Handler?.DisconnectHandler();
            RemoveLogicalChild(content);
        }
        if (!preservePresentation && _toolsSheet is { } dialog)
        {
            _toolsSheet = null;
            dialog.Dismiss();
            dialog.Dispose();
        }
    }

    partial void AttachBackNavigation()
    {
        DetachBackNavigation();
        if (IsLoaded && GeoView != null && Handler?.MauiContext?.Context?.GetActivity() is ComponentActivity activity)
        {
            var callback = new ToolsBackCallback(this, activity.OnBackPressedDispatcher);
            activity.OnBackPressedDispatcher.AddCallback(activity, callback);
            _backCallback = new WeakReference<ToolsBackCallback>(callback);
            UpdateBackNavigation();
        }
    }

    partial void DetachBackNavigation()
    {
        if (_backCallback?.TryGetTarget(out var callback) == true)
        {
            callback.Remove();
            callback.Dispose();
        }
        _backCallback = null;
    }

    partial void UpdateBackNavigation()
    {
        if (_backCallback?.TryGetTarget(out var callback) == true)
            callback.Enabled = _toolsSheet == null && _infoSheet == null && _connected && (_panelAnchor != null || ViewModel.SelectedImagePopup != null);
    }

    private sealed class ToolsBackCallback(OrientedImageryView owner, OnBackPressedDispatcher dispatcher) : OnBackPressedCallback(false)
    {
        public override void HandleOnBackPressed()
        {
            if (owner.DismissTools()) return;
            Enabled = false;
            dispatcher.OnBackPressed();
        }
    }
}
#endif
