#if __IOS__ && !MACCATALYST
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private static readonly Color IosAccent = Color.FromArgb("#007AFF");

    partial void DetachPlatformPresentation() => DismissPanel();

    private sealed class IosPanelMaterial : ContentView, IDisposable
    {
        private UIVisualEffectView? _material;
        private NSObject? _transparencyObserver;
        private bool _disposed;

        internal IosPanelMaterial()
        {
            InputTransparent = true;
            Loaded += MaterialLoaded;
            Unloaded += MaterialUnloaded;
            AutomationProperties.SetIsInAccessibleTree(this, false);
        }

        protected override void OnHandlerChanging(HandlerChangingEventArgs args)
        {
            Release();
            base.OnHandlerChanging(args);
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (IsLoaded) Attach();
        }

        private void MaterialLoaded(object? sender, EventArgs e) => Attach();

        private void MaterialUnloaded(object? sender, EventArgs e) => Release();

        private void Attach()
        {
            if (_disposed || _material != null || Handler?.PlatformView is not UIView view) return;
            _material = new UIVisualEffectView
            {
                Frame = view.Bounds,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
                UserInteractionEnabled = false,
                IsAccessibilityElement = false,
                ClipsToBounds = true,
            };
            _material.Layer.CornerRadius = 16;
            view.InsertSubview(_material, 0);
            UpdateMaterial();
            _transparencyObserver = UIApplication.Notifications.ObserveReduceTransparencyStatusDidChange(
                (_, _) => Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(UpdateMaterial));
        }

        private void UpdateMaterial()
        {
            if (_material == null) return;
            if (UIAccessibility.IsReduceTransparencyEnabled)
            {
                _material.Effect = null;
                _material.BackgroundColor = UIColor.SecondarySystemBackground;
            }
            else
            {
                using var effect = UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial);
                _material.BackgroundColor = UIColor.Clear;
                _material.Effect = effect;
            }
        }

        private void Release()
        {
            _transparencyObserver?.Dispose();
            _transparencyObserver = null;
            _material?.RemoveFromSuperview();
            _material?.Dispose();
            _material = null;
        }

        public void Dispose()
        {
            _disposed = true;
            Loaded -= MaterialLoaded;
            Unloaded -= MaterialUnloaded;
            Release();
        }
    }
}
#endif
