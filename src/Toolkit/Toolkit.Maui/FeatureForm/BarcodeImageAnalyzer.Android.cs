#if ANDROID
using Android.Gms.Tasks;
using Android.Graphics;
using Android.Runtime;
using AndroidX.Camera.Core;
using Microsoft.Maui.Graphics.Platform;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using RectF = Android.Graphics.RectF;
using Size = Android.Util.Size;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{

    internal sealed class BarcodeInfo
    {
        public RectF BoundingBox { get; }
        public string RawValue { get; }
        public long LastSeenFrame { get; }

        public BarcodeInfo(RectF boundingBox, string rawValue, long lastSeenFrame)
        {
            BoundingBox = boundingBox;
            RawValue = rawValue;
            LastSeenFrame = lastSeenFrame;
        }
    }

    internal sealed class BarcodeImageAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private readonly Action<List<BarcodeInfo>> _onSuccess;
        private Matrix? _sensorToTargetMatrix;
        private long _frames;
        private readonly Dictionary<string, BarcodeInfo> _barcodeMap = new();
        private readonly IBarcodeScanner _barcodeScanner;

        public BarcodeImageAnalyzer(Action<List<BarcodeInfo>> onSuccess)
        {
            _onSuccess = onSuccess;
            _barcodeScanner = BarcodeScanning
                                .GetClient(new BarcodeScannerOptions.Builder()
                                             .EnableAllPotentialBarcodes()
                                             .Build());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _barcodeScanner.Dispose();
        }

        Size ImageAnalysis.IAnalyzer.DefaultTargetResolution => new(1920, 1080);

        int ImageAnalysis.IAnalyzer.TargetCoordinateSystem => ImageAnalysis.CoordinateSystemViewReferenced;

        void ImageAnalysis.IAnalyzer.Analyze(IImageProxy image)
        {
            var mediaImage = image.Image;
            if (mediaImage != null)
            {
                _barcodeScanner.Process(mediaImage, image.ImageInfo.RotationDegrees)
                    .AddOnSuccessListener(new OnSuccessListener(barcodes =>
                    {
                        _frames = (_frames + 1) % long.MaxValue;
                        ProcessBarcodes(barcodes, image);
                    }))
                    .AddOnFailureListener(new OnFailureListener(e =>
                    {
                        image.Close();
                    }));
            }
            else
            {
                image.Close();
            }
        }

        void ImageAnalysis.IAnalyzer.UpdateTransform(global::Android.Graphics.Matrix? matrix)
        {
            _sensorToTargetMatrix = matrix;
        }

        private void ProcessBarcodes(IList<Barcode> barcodes, IImageProxy image)
        {
            using (image)
            {
                foreach (var barcode in barcodes)
                {
                    // filter out barcodes that do not have a raw value or the raw value is empty
                    if (string.IsNullOrEmpty(barcode.RawValue))
                    {
                        continue;
                    }

                    // Get the bounding box of the barcode and convert it to the view coordinate system.
                    var boundingBox = barcode.BoundingBox;
                    if (boundingBox is not null)
                    {
                        var rect = new RectF(boundingBox.Left, boundingBox.Top, boundingBox.Right, boundingBox.Bottom);
                        var status = GetTransformationMatrix(image)?.MapRect(rect, boundingBox.AsRectF());
                        if (status == true)
                        {
                            _barcodeMap[barcode.RawValue] = new BarcodeInfo(rect, barcode.RawValue, _frames);
                        }
                    }
                }
                image.Close();

                var keysToRemove = _barcodeMap.Values
                    .Where(info => info.LastSeenFrame < _frames - 4)
                    .Select(info => info.RawValue)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _barcodeMap.Remove(key);
                }
                _onSuccess(_barcodeMap.Values.ToList());
            }
        }

        private Matrix? GetTransformationMatrix(IImageProxy imageProxy)
        {
            var analysisToTarget = new Matrix();
            if (_sensorToTargetMatrix == null)
            {
                imageProxy.Close();
                return null;
            }

            var sensorToAnalysis = new Matrix(imageProxy.ImageInfo.SensorToBufferTransformMatrix);
            var sourceRect = new RectF(0, 0, imageProxy.Width, imageProxy.Height);
            var bufferRect = TransformUtils.RotateRect(sourceRect, imageProxy.ImageInfo.RotationDegrees);
            var analysisToMlKitRotation = TransformUtils.GetRectToRect(sourceRect, bufferRect, imageProxy.ImageInfo.RotationDegrees);

            sensorToAnalysis.PostConcat(analysisToMlKitRotation);
            sensorToAnalysis.Invert(analysisToTarget);
            analysisToTarget.PostConcat(_sensorToTargetMatrix);

            return analysisToTarget;
        }

        internal sealed class OnSuccessListener : Java.Lang.Object, IOnSuccessListener
        {
            private readonly Action<IList<Barcode>> _onSuccessAction;

            public OnSuccessListener(Action<IList<Barcode>> onSuccessAction)
            {
                _onSuccessAction = onSuccessAction;
            }

            public void OnSuccess(Java.Lang.Object? result)
            {
                _onSuccessAction.Invoke(result.JavaCast<JavaList<Barcode>>()!);
            }
        }

        internal sealed class OnFailureListener : Java.Lang.Object, IOnFailureListener
        {
            private readonly Action<Exception> _onFailureAction;

            public OnFailureListener(Action<Exception> onFailureAction)
            {
                _onFailureAction = onFailureAction;
            }

            public void OnFailure(Java.Lang.Exception e)
            {
                _onFailureAction.Invoke(e);
            }
        }
    }
}
#endif