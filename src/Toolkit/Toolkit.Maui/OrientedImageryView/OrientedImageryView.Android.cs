#if ANDROID
using AndroidX.Activity;
using Google.Android.Material.BottomSheet;
using Microsoft.Maui.Platform;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    #region Sheet state and lifecycle

    private WeakReference<ToolsBackCallback>? _backCallback;
    private BottomSheetDialog? _toolsSheet;
    private ContentView? _toolsSheetContent;
    private BottomSheetDialog? _infoSheet;
    private ContentView? _infoSheetContent;

    partial void AttachPlatformPresentation() => AttachBackNavigation();

    partial void DetachPlatformPresentation()
    {
        DismissPanel();
        DismissAndroidPopup();
        DetachBackNavigation();
    }

    #endregion

    #region Native sheet presentation

    private void PresentAndroidPanel(ContentView content)
    {
        var context = Handler?.MauiContext ??
            throw new InvalidOperationException("The oriented imagery view must have a handler before opening its tools.");
        _toolsSheetContent = content;
        AddLogicalChild(content);
        var nativeContent = content.ToPlatform(context);
        bool newSheet = _toolsSheet == null;
        if (newSheet)
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
        }, observeSizeChanges: newSheet);
    }

    private static void ShowAndroidSheet(BottomSheetDialog dialog, Android.Views.View nativeContent, Action<double> updateWidth, bool observeSizeChanges = true)
    {
        dialog.Show();
        dialog.Window?.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
        if (nativeContent.Parent is Android.Views.View sheet)
        {
            sheet.SetBackgroundColor(Android.Graphics.Color.Transparent);
            double density = sheet.Resources!.DisplayMetrics!.Density;
            // Submenus replace content but reuse this native sheet and its width subscription.
            if (observeSizeChanges)
            {
                sheet.LayoutChange += (_, args) =>
                {
                    if (args.Right > args.Left && args.Right - args.Left != args.OldRight - args.OldLeft)
                        updateWidth((args.Right - args.Left) / density);
                };
            }
            if (sheet.Width > 0)
                updateWidth(sheet.Width / density);
        }
        dialog.Behavior.State = BottomSheetBehavior.StateExpanded;
        dialog.Behavior.SkipCollapsed = true;
    }

    #endregion

    #region Image information

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
        var heading = PanelHeading(Text("ImageDetails"));
        _infoSheetContent = CreateMobileSheetContent(heading, _popupViewer, () => DismissAndroidPopup(), null, out _, "OrientedImageryInfoSheet");
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

    #endregion

    #region Toolbar sheet sizing and cleanup

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

    #endregion

    #region Back navigation

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
        // Native dialogs handle Back while visible; this callback handles inline popups.
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

    #endregion
}
#endif
