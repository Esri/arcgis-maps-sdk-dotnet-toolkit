#if __IOS__
using Microsoft.Maui.Platform;
using UIKit;
using ContentView = Microsoft.Maui.Controls.ContentView;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    #region Sheet state and lifecycle

    private static readonly Color IosAccent = Color.FromArgb("#007AFF");
    private UIViewController? _iosSheet;
    private ContentView? _iosSheetContent;
    private Action? _releaseIosSheetDelegate;
    private bool _iosSheetIsInfo;
    private bool _iosSheetPresenting;
    private bool _iosSheetClosing;
    private double _iosToolsHeight;

    partial void DetachPlatformPresentation()
    {
        DismissPanel();
        DismissIosPopup();
    }

    #endregion

    #region Image information

    private void RefreshIosPopup()
    {
        if (_popupHost != null) return;
        if (ViewModel.SelectedImagePopup == null)
        {
            DismissIosPopup(closePopup: false);
            return;
        }
        if (_popupViewer == null || !IsLoaded || Handler?.MauiContext == null) return;
        _popupViewer.Popup = ViewModel.SelectedImagePopup;
        _popupViewer.IsVisible = true;
        if (_iosSheetIsInfo) return;
        DismissPanel(preservePresentation: true);
        var heading = PanelHeading(Text("ImageDetails"));
        var surface = CreateMobileSheetContent(heading, _popupViewer, () => DismissIosPopup(), null,
            out _, "OrientedImageryInfoSheet");
        PresentIosSheet(surface, info: true);
        SemanticScreenReader.Default.Announce(heading.Text);
    }

    private void DismissIosPopup(bool closePopup = true)
    {
        if (!_iosSheetIsInfo) return;
        _iosSheetIsInfo = false;
        if (_popupViewer?.Parent is Grid layout) layout.Remove(_popupViewer);
        DismissIosSheet();
        if (closePopup && ViewModel.SelectedImagePopup != null)
            ViewModel.CloseSelectedImagePopupCommand.Execute(null);
    }

    #endregion

    #region Native sheet presentation

    private static void SetIosOptionRowBackground(Border row)
    {
        using var light = UITraitCollection.FromUserInterfaceStyle(UIUserInterfaceStyle.Light);
        using var dark = UITraitCollection.FromUserInterfaceStyle(UIUserInterfaceStyle.Dark);
        row.SetAppThemeColor(BackgroundColorProperty,
            UIColor.SystemGray5.GetResolvedColor(light).ToColor(),
            UIColor.SystemGray5.GetResolvedColor(dark).ToColor());
    }

    private void PresentIosSheet(View content, bool info)
    {
        _iosSheetIsInfo = info;
        if (_iosSheetContent == null)
        {
            _iosSheetContent = new ContentView();
            _iosSheetContent.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#F2F2F7"), Color.FromArgb("#1C1C1E"));
            AddLogicalChild(_iosSheetContent);
            _iosSheetContent.SizeChanged += IosSheetSizeChanged;
        }
        // Swap menu content without replacing the native sheet.
        _iosSheetContent.Content = content;
        UpdateIosPanelSize();
        if (_iosSheet?.SheetPresentationController is { } sheet)
            ConfigureIosDetents(sheet);
        ShowIosSheet();
    }

    private void ShowIosSheet()
    {
        if (_iosSheet != null || _iosSheetClosing || _iosSheetContent == null) return;
        var context = Handler?.MauiContext ??
            throw new InvalidOperationException("The oriented imagery view must have a handler before opening its tools.");
        UIResponder? responder = Handler?.PlatformView as UIView;
        while (responder != null && responder is not UIViewController)
            responder = responder.NextResponder;
        var presenter = responder as UIViewController ??
            throw new InvalidOperationException("The oriented imagery view must belong to a view controller before opening its tools.");
        while (presenter.PresentedViewController is { } presented)
            presenter = presented;
        _iosSheet = _iosSheetContent.ToUIViewController(context);
        _iosSheet.ModalPresentationStyle = UIModalPresentationStyle.PageSheet;
        if (_iosSheet.SheetPresentationController is { } sheet)
        {
            ConfigureIosDetents(sheet);
            sheet.PrefersEdgeAttachedInCompactHeight = true;
            sheet.PrefersGrabberVisible = true;
            var dismissal = new IosSheetDismissal(this);
            // UIKit holds its delegate weakly; retain it until dismissal completes.
            _releaseIosSheetDelegate = dismissal.Dispose;
            sheet.Delegate = dismissal;
        }
        _iosSheetPresenting = true;
        presenter.PresentViewController(_iosSheet, true, () =>
        {
            _iosSheetPresenting = false;
            if (_panelAnchor == null && !_iosSheetIsInfo)
                DismissIosSheet();
        });
    }

    #endregion

    #region Sheet sizing

    private void IosSheetSizeChanged(object? sender, EventArgs e) => UpdateIosPanelSize();

    private void UpdateIosPanelSize()
    {
        if (_iosSheetIsInfo || _iosSheetContent == null || _panelLayout == null || _panelScroll == null) return;
        double width = _iosSheetContent.Width > 0 ? _iosSheetContent.Width :
            (Handler?.PlatformView as UIView)?.Window?.Bounds.Width ?? 0;
        if (width <= 0) return;
        if (_symbolGrid != null)
            LayoutSymbolGrid(Math.Max(0, width - 32));
        double header = _panelLayout.Children.Where(child => child != _panelScroll)
            .Sum(child => child.Measure(width, double.PositiveInfinity).Height);
        double body = _panelScroll.Content?.Measure(width, double.PositiveInfinity).Height ?? 0;
        double height = _panelLayout.Padding.VerticalThickness + header + body;
        if (Math.Abs(_iosToolsHeight - height) < 1) return;
        _iosToolsHeight = height;
        _iosSheet?.SheetPresentationController?.InvalidateDetents();
    }

    private void ConfigureIosDetents(UISheetPresentationController sheet)
    {
        sheet.Detents = _iosSheetIsInfo
            ? new[] { UISheetPresentationControllerDetent.CreateMediumDetent(), UISheetPresentationControllerDetent.CreateLargeDetent() }
            : new[]
            {
                UISheetPresentationControllerDetent.Create("tools", context =>
                    (nfloat)Math.Min(context.MaximumDetentValue * 0.85, Math.Max(100, _iosToolsHeight))),
            };
    }

    #endregion

    #region Dismissal and cleanup

    private void DismissIosSheet()
    {
        // Defer a close during the native opening animation to its completion.
        if (_iosSheetPresenting) return;
        var controller = _iosSheet;
        var content = _iosSheetContent;
        var releaseDelegate = _releaseIosSheetDelegate;
        _iosSheet = null;
        _iosSheetContent = null;
        _iosToolsHeight = 0;
        _releaseIosSheetDelegate = null;
        if (content == null) return;
        content.SizeChanged -= IosSheetSizeChanged;
        void Release()
        {
            content.Content = null;
            content.Handler?.DisconnectHandler();
            RemoveLogicalChild(content);
            releaseDelegate?.Invoke();
            controller?.Dispose();
            _iosSheetClosing = false;
            // A new menu may have been requested during the dismissal animation.
            ShowIosSheet();
        }
        if (controller?.PresentingViewController != null)
        {
            _iosSheetClosing = true;
            controller.DismissViewController(true, Release);
        }
        else
            Release();
    }

    private sealed class IosSheetDismissal(OrientedImageryView owner) : UISheetPresentationControllerDelegate
    {
        public override void DidDismiss(UIPresentationController presentationController)
        {
            if (owner._iosSheetIsInfo)
                owner.DismissIosPopup();
            else
                owner.DismissPanel();
        }
    }

    #endregion
}
#endif
