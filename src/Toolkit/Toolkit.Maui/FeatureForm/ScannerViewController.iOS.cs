#if __IOS__
using AVFoundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    // Port of https://github.com/Esri/arcgis-maps-sdk-swift-toolkit/blob/81552fcff71e638a2c306ead6406a8a0c93ef334/Sources/ArcGISToolkit/Common/CodeScanner.swift
    internal sealed class ScannerViewController : UIViewController, IAVCaptureMetadataOutputObjectsDelegate
    {
        private AVCaptureSession? captureSession;

        private UISelectionFeedbackGenerator feedbackGenerator = new UISelectionFeedbackGenerator();
        private AVCaptureVideoPreviewLayer? previewLayer;
        /// The color of a code overlay before it's been targeted for auto-scan.
        private UIColor normalOverlayColor = UIColor.White.ColorWithAlpha(0.25f);
        /// The number of consecutive hits required to trigger an automatic scan.
        private const int requiredTargetHits = 25;
        private CoreAnimation.CAShapeLayer? reticleLayer;
        /// The number of consecutive target hits. See also `requiredTargetHits`.
        private int targetHits = 0;
        /// The string value of the targeted code.
        private string? targetStringValue;
        private TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
        private List<MetadataObjectLayer> metadataObjectOverlayLayers = new List<MetadataObjectLayer>(0);

        private ScannerViewController() : base()
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                feedbackGenerator?.Dispose();
                previewLayer?.Dispose();
                normalOverlayColor?.Dispose();
                reticleLayer?.Dispose();
                captureSession?.Dispose();
            }
        }

        public static Task<string> ScanAsync()
        {
            var scanner = new ScannerViewController();
            var currentViewController = Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController();
            currentViewController!.ShowDetailViewController(scanner, null);
            return scanner.tcs.Task;
        }
        private class MetadataObjectLayer : CoreAnimation.CAShapeLayer
        {

            public AVMetadataObject? MetadataObject { get; set; }
            public string? StringValue => (MetadataObject as AVMetadataMachineReadableCodeObject)?.StringValue;
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            UpdateReticleAndAutoFocus();
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
            var success = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
            if (success != true)
                return;
            captureSession = new AVCaptureSession();
            // Use AVCaptureDevice to scan a barcode
            AVCaptureDevice? videoCaptureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
            if (videoCaptureDevice is null) return;
            var videoInput = new AVCaptureDeviceInput(videoCaptureDevice, out var error);
            if (captureSession.CanAddInput(videoInput))
                captureSession.AddInput(videoInput);
            else return;
            var metadataOutput = new AVCaptureMetadataOutput();
            if (captureSession.CanAddOutput(metadataOutput))
            {
                captureSession.AddOutput(metadataOutput);
                metadataOutput.SetDelegate(this, CoreFoundation.DispatchQueue.MainQueue);
                metadataOutput.MetadataObjectTypes =
                    // Barcodes
                    AVMetadataObjectType.CodabarCode |
                    AVMetadataObjectType.Code39Code |
                    AVMetadataObjectType.Code39Mod43Code |
                    AVMetadataObjectType.Code93Code |
                    AVMetadataObjectType.Code128Code |
                    AVMetadataObjectType.EAN8Code |
                    AVMetadataObjectType.EAN13Code |
                    AVMetadataObjectType.GS1DataBarCode |
                    AVMetadataObjectType.GS1DataBarExpandedCode |
                    AVMetadataObjectType.GS1DataBarLimitedCode |
                    AVMetadataObjectType.Interleaved2of5Code |
                    AVMetadataObjectType.ITF14Code |
                    AVMetadataObjectType.UPCECode |
                    // 2D codes
                    AVMetadataObjectType.AztecCode |
                    AVMetadataObjectType.DataMatrixCode |
                    AVMetadataObjectType.MicroPdf417Code |
                    AVMetadataObjectType.MicroQRCode |
                    AVMetadataObjectType.PDF417Code |
                    AVMetadataObjectType.QRCode;
            }
            else return;

            previewLayer = new AVCaptureVideoPreviewLayer(session: captureSession);
            previewLayer.Frame = View!.Layer.Bounds;
            previewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
            View.Layer.AddSublayer(previewLayer);
            UpdateReticleAndAutoFocus();

            captureSession.StartRunning();
        }

        void IAVCaptureMetadataOutputObjectsDelegate.DidOutputMetadataObjects(AVCaptureMetadataOutput captureOutput, AVMetadataObject[] metadataObjects, AVCaptureConnection connection)
        {
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                RemoveMetadataObjectOverlayLayers();
                List<MetadataObjectLayer> metadataObjectOverlayLayers = new List<MetadataObjectLayer>();
                foreach (var metadataObject in metadataObjects)
                {
                    var overlayLayer = CreateMetadataObjectOverlayWithMetadataObject(metadataObject);
                    metadataObjectOverlayLayers.Add(overlayLayer);
                }
                AddMetadataObjectOverlayLayersToVideoPreviewView(metadataObjectOverlayLayers);
                CheckTargetHits();
            });
        }

        private void AddMetadataObjectOverlayLayersToVideoPreviewView(List<MetadataObjectLayer> metadataObjectOverlayLayers)
        {
            CoreAnimation.CATransaction.Begin();
            CoreAnimation.CATransaction.DisableActions = true;
            foreach (var metadataObjectOverlayLayer in metadataObjectOverlayLayers)
            {
                previewLayer?.AddSublayer(metadataObjectOverlayLayer);
            }
            CoreAnimation.CATransaction.Commit();
            this.metadataObjectOverlayLayers = metadataObjectOverlayLayers;
        }

        private CoreGraphics.CGPath BarcodeOverlayPathWithCorners(CoreGraphics.CGPoint[]? corners)
        {
            var path = new CoreGraphics.CGPath();
            if (corners is not null && corners.Length > 0)
            {
                path.MoveToPoint(corners[0]);
                for (int i = 1; i < corners.Length; i++)
                {
                    path.AddLineToPoint(corners[i]);
                }
                path.CloseSubpath();
            }
            return path;
        }

        private static UIColor InterpolatedWith(UIColor thisColor, UIColor otherColor, nfloat percent)
        {
            thisColor.GetRGBA(out var r1, out var g1, out var b1, out var a1);
            otherColor.GetRGBA(out var r2, out var g2, out var b2, out var a2);
            nfloat c1 = 1 - percent;
            return new UIColor(
                red: c1 * r1 + percent * r2,
                green: c1 * g1 + percent * g2,
                blue: c1 * b1 + percent * b2,
                alpha: c1 * a1 + percent * a2
            );
        }

        /// Checks if the reticle intersects with any of the current overlays. When a code with a consistent string
        /// value intersects with the reticle for the `requiredTargetHits` count, it is auto-scanned.
        private void CheckTargetHits()
        {
            var reticleWasContainedInAnOverlay = false;
            foreach (var overlayLayer in metadataObjectOverlayLayers)
            {
                if (overlayLayer.Path!.ContainsPoint(reticleLayer!.Position, true))
                {
                    reticleWasContainedInAnOverlay = true;
                    var stringValue = targetStringValue;
                    if (stringValue == overlayLayer.StringValue)
                    {
                        targetHits += 1;
                        overlayLayer.FillColor = InterpolatedWith(normalOverlayColor, UIColor.Tint, targetHits / (nfloat)requiredTargetHits).CGColor;
                        if (targetHits >= requiredTargetHits)
                        {
                            tcs.TrySetResult(stringValue!);
                            DismissViewController(true, null);
                            if (OperatingSystem.IsIOSVersionAtLeast(17, 5))
                            {
                                if (overlayLayer.MetadataObject is not null)
                                    feedbackGenerator.SelectionChanged(overlayLayer.MetadataObject.Bounds.Location);
                            }
                            targetHits = 0;
                        }
                    }
                    else
                    {
                        targetStringValue = overlayLayer.StringValue;
                        targetHits = 0;
                    }
                }
            }
            if (!reticleWasContainedInAnOverlay)
            {
                targetStringValue = null;
                targetHits = 0;
            }
        }

        private MetadataObjectLayer CreateMetadataObjectOverlayWithMetadataObject(AVMetadataObject metadataObject)
        {
            var transformedMetadataObject = previewLayer!.GetTransformedMetadataObject(metadataObject);
            var metadataObjectOverlayLayer = new MetadataObjectLayer()
            {
                MetadataObject = transformedMetadataObject,
                LineJoin = CoreAnimation.CAShapeLayer.JoinRound,
                LineWidth = 2.5f,
                FillColor = normalOverlayColor.CGColor,
                StrokeColor = UIColor.Tint.CGColor
            };
            if (transformedMetadataObject is not AVMetadataMachineReadableCodeObject barcodeMetadataObject)
                return metadataObjectOverlayLayer;

            var barcodeOverlayPath = BarcodeOverlayPathWithCorners(barcodeMetadataObject.Corners);
            metadataObjectOverlayLayer.Path = barcodeOverlayPath;
            var fontSize = 12;
            if (!string.IsNullOrEmpty(barcodeMetadataObject.StringValue))
            {
                var barcodeOverlayBoundingBox = barcodeOverlayPath.BoundingBox;
                var minimumTextLayerHeight = fontSize + 4;
                var textLayerHeight = barcodeOverlayBoundingBox.Height < minimumTextLayerHeight ? minimumTextLayerHeight : barcodeOverlayBoundingBox.Height;

                var textLayer = new CoreAnimation.CATextLayer
                {
                    TextAlignmentMode = CoreAnimation.CATextLayerAlignmentMode.Center,
                    Bounds = new CoreGraphics.CGRect(0, 0, barcodeOverlayBoundingBox.Width, textLayerHeight),
                    ContentsScale = UIScreen.MainScreen.Scale,
                    WeakFont = UIFont.SystemFontOfSize(fontSize),
                    Position = new CoreGraphics.CGPoint(barcodeOverlayBoundingBox.Left + barcodeOverlayBoundingBox.Width / 2, barcodeOverlayBoundingBox.Top + barcodeOverlayBoundingBox.Height / 2),
                    AttributedString = new Foundation.NSAttributedString(barcodeMetadataObject.StringValue!,
                        new UIStringAttributes()
                        {
                            Font = UIFont.SystemFontOfSize(fontSize),
                            ForegroundColor = UIColor.White
                        }
                    ),
                    Wrapped = true,
                    Transform = previewLayer.Transform
                };
                metadataObjectOverlayLayer.AddSublayer(textLayer);

            }
            return metadataObjectOverlayLayer;
        }

        private void RemoveMetadataObjectOverlayLayers()
        {
            foreach (var sublayer in metadataObjectOverlayLayers)
            {
                sublayer.RemoveFromSuperLayer();
            }
            metadataObjectOverlayLayers = new List<MetadataObjectLayer>();
        }
        /// Focus on and adjust exposure at the point of interest.
        private void UpdateAutoFocus(CoreGraphics.CGPoint point)
        {
            if (previewLayer is null) return;
            var convertedPoint = previewLayer.CaptureDevicePointOfInterestForPoint(point);
            var device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
            if (device == null) return;

            try
            {
                device.LockForConfiguration(out var error);
                if (error is not null) return;
                if (device.FocusPointOfInterestSupported && device.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
                {
                    device.FocusPointOfInterest = convertedPoint;
                    device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                }
                if (device.ExposurePointOfInterestSupported && device.IsExposureModeSupported(AVCaptureExposureMode.AutoExpose))
                {
                    device.ExposurePointOfInterest = convertedPoint;
                    device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                }
                device.UnlockForConfiguration();
            }
            catch
            {
                // Handle error
            }
        }
        private void UpdateReticle(CoreGraphics.CGPoint point)
        {
            if (previewLayer is null) return;
            this.reticleLayer?.RemoveFromSuperLayer();

            var reticleLayer = new CoreAnimation.CAShapeLayer();
            var radius = 5.0f;
            reticleLayer.Path = UIBezierPath.FromRoundedRect(new CoreGraphics.CGRect(0, 0, 2.0 * radius, 2.0 * radius), radius).CGPath;
            reticleLayer.Frame = new CoreGraphics.CGRect(
                new CoreGraphics.CGPoint(point.X - radius, point.Y - radius),
                new CoreGraphics.CGSize(radius * 2, radius * 2)
            );
            reticleLayer.FillColor = UIColor.Tint.CGColor;
            reticleLayer.ZPosition = float.MaxValue;
            previewLayer.AddSublayer(reticleLayer);
            this.reticleLayer = reticleLayer;
        }

        private void UpdateReticleAndAutoFocus()
        {
            var pointOfInterest = new CoreGraphics.CGPoint(
                x: View!.Frame.X + View.Frame.Width / 2,
                y: View.Frame.Y + View.Frame.Height / 2
            );
            UpdateAutoFocus(pointOfInterest);
            UpdateReticle(pointOfInterest);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            if (captureSession?.Running == false)
            {
                captureSession.StartRunning();
            }
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            if (captureSession?.Running == true)
            {
                captureSession.StopRunning();
            }
            tcs.TrySetCanceled();
        }
        public override bool PrefersStatusBarHidden() => true;

        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations() => UIInterfaceOrientationMask.Portrait;
    }
}
#endif