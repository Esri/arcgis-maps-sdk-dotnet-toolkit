#if ANDROID
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Util.Concurrent;
using Microsoft.Maui.ApplicationModel;
namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    internal sealed class BarcodeScanner : Android.Views.ViewGroup
    {
        private const int AUTO_SCAN_DELAY_MS = 1000;

        private readonly ILifecycleOwner lifecycleOwner;
        private readonly PreviewView previewView;
        private readonly LifecycleCameraController cameraController;
        private readonly IExecutor executor;
        private readonly BarcodeFrame barcodeFrame;
        private BarcodeImageAnalyzer barcodeImageAnalyzer;
        private BarcodeInfo? autoScanBarcode;
        private CancellationTokenSource autoScanDelayCancellationToken = new CancellationTokenSource();

        public static async Task<string?> ScanAsync(Android.Content.Context context)
        {
            await Permissions.RequestAsync<Permissions.Camera>();

            var tcs = new TaskCompletionSource<string?>();
            var dialog = new Android.App.Dialog(context);
            var scanner = new BarcodeScanner(context);
            dialog.SetContentView(scanner);
            dialog.Window?.SetLayout(LayoutParams.MatchParent, LayoutParams.MatchParent);
            dialog.Show();
            scanner.BarcodeTapped += (s,e) =>
            {
                tcs.TrySetResult(e);
                dialog.Dismiss();
            };
            dialog.DismissEvent += (s, e) => tcs.TrySetCanceled();
            return await tcs.Task;
        }

        private BarcodeScanner(Android.Content.Context context) : base(context)
        {
            if (context is AndroidX.Lifecycle.ILifecycleOwner owner)
                lifecycleOwner = owner;
            else if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is ILifecycleOwner owner2)
                lifecycleOwner = owner2;
            else 
                throw new NotSupportedException();
            previewView = new PreviewView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
            };
            executor = ContextCompat.GetMainExecutor(context);

            this.AddView(previewView, new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent));
            this.Touch += OnTap;
            cameraController = new LifecycleCameraController(context)
            {
                ImageAnalysisBackpressureStrategy = ImageAnalysis.StrategyKeepOnlyLatest,
                ImageAnalysisResolutionSelector = new AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector.Builder().SetResolutionStrategy(
                    new AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy(
                    new Android.Util.Size(1920, 1080), AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy.FallbackRuleClosestHigherThenLower)).Build(),
            };
            cameraController.BindToLifecycle(lifecycleOwner);
            previewView.Controller = cameraController;
            barcodeImageAnalyzer = new BarcodeImageAnalyzer(OnBarcodeDetected);
            cameraController.SetImageAnalysisAnalyzer(executor, barcodeImageAnalyzer);

            barcodeFrame = new BarcodeFrame(context);
            this.AddView(barcodeFrame, new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent));
            barcodeFrame.BarcodeTapped += (s, e) => BarcodeTapped?.Invoke(this, e);
        }

        public event EventHandler<string>? BarcodeTapped;

        private void OnBarcodeDetected(List<BarcodeInfo> barcodeInfoList)
        {
            barcodeFrame.Barcodes = barcodeInfoList;
            barcodeFrame.Invalidate();
            // set the auto-scan barcode if only one barcode is detected
            if (barcodeInfoList.Count == 1)
            {
                // compare the current auto-scan barcode with the detected barcode to avoid setting
                // the same barcode multiple times
                if (autoScanBarcode?.RawValue != barcodeInfoList.First().RawValue)
                {
                    autoScanDelayCancellationToken?.Cancel();
                    // once the auto-scan barcode is set, the delay routine will start
                    autoScanBarcode = barcodeInfoList.First();
                    autoScanDelayCancellationToken = new CancellationTokenSource();
                    Task.Delay(AUTO_SCAN_DELAY_MS, autoScanDelayCancellationToken.Token).ContinueWith((t) =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            BarcodeTapped?.Invoke(this, autoScanBarcode.RawValue);
                        }
                    });
                }
            }
            else
            {
                autoScanDelayCancellationToken?.Cancel();
                autoScanBarcode = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                autoScanDelayCancellationToken?.Dispose();
                cameraController?.Dispose();
                executor?.Dispose();
                previewView?.Dispose();
                barcodeImageAnalyzer?.Dispose();
                barcodeFrame?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnTap(object? sender, TouchEventArgs e)
        {
            if (cameraController is null)
                return;

            var action = new FocusMeteringAction.Builder(previewView.MeteringPointFactory.CreatePoint(e.Event?.GetX() ?? 0, e.Event?.GetY() ?? 0))
                .SetAutoCancelDuration(3, Java.Util.Concurrent.TimeUnit.Seconds!).Build();
            cameraController.CameraControl?.StartFocusAndMetering(action);
        }

        protected override void OnDetachedFromWindow()
        {
            autoScanDelayCancellationToken?.Cancel();
            executor?.Dispose();
            cameraController?.Unbind();
            barcodeImageAnalyzer?.Dispose();
            base.OnDetachedFromWindow();
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            previewView.Layout(l, t, r,  b);
            barcodeFrame.Layout(l, t, r, b);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            previewView.Measure(widthMeasureSpec, heightMeasureSpec);
            barcodeFrame.Measure(widthMeasureSpec, heightMeasureSpec);
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
        }

        /// <summary>
        /// Display the detected barcodes on top of the camera preview.
        /// </summary>
        private sealed class BarcodeFrame : Android.Views.View
        {
            private readonly Android.Graphics.Paint barcodePaint;
            private readonly Android.Graphics.Paint barcodeTextPaint;

            public BarcodeFrame(Android.Content.Context context) : base(context)
            {
                barcodePaint = new Android.Graphics.Paint() { Color = Android.Graphics.Color.Blue, Alpha = 77 };
                barcodeTextPaint = new Android.Graphics.Paint() { Color = Android.Graphics.Color.White, TextSize = 24f };
                Touch += BarcodeFrame_Touch;
            }

            private void BarcodeFrame_Touch(object? sender, TouchEventArgs e)
            {
                if (Barcodes is null || e.Event is null) return;
                foreach(var barcode in Barcodes)
                {
                    if (barcode.BoundingBox.Contains(e.Event.GetX(), e.Event.GetY()))
                    {
                        BarcodeTapped?.Invoke(this, barcode.RawValue);
                        break;
                    }
                }
            }

            public event EventHandler<string>? BarcodeTapped;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    barcodePaint?.Dispose();
                    barcodeTextPaint?.Dispose();
                }
                base.Dispose(disposing);
            }

            public override void Draw(Android.Graphics.Canvas canvas)
            {
                base.Draw(canvas);
                if (Barcodes is null) return;
                foreach (var barcode in Barcodes)
                {
                    canvas.DrawRoundRect(barcode.BoundingBox, 15, 15, barcodePaint);
                    canvas.DrawText(barcode.RawValue, barcode.BoundingBox.Left + 2, barcode.BoundingBox.Top + barcodeTextPaint.TextSize + 2, barcodeTextPaint);
                }
            }

            public IEnumerable<BarcodeInfo>? Barcodes { get; set; }
        }
    }
}
#endif