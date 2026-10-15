#if ANDROID
using AndroidX.Activity;
using Microsoft.Maui.Platform;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private WeakReference<ToolsBackCallback>? _backCallback;

    partial void AttachPlatformPresentation() => AttachBackNavigation();

    partial void DetachPlatformPresentation() => DetachBackNavigation();

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
            callback.Enabled = _connected && (_panelAnchor != null || ViewModel.SelectedImagePopup != null);
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
