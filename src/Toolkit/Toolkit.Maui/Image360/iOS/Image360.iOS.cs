#if __IOS__
using Foundation;
using SceneKit;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

internal class Image360 : UIView
{
    private readonly SCNView _sceneView;
    private readonly SCNSphere _sphere;
    private readonly SCNMaterial _sphereMaterial;
    private int _sourceRequestId;

    public Image360() : base()
    {
        _sceneView = new SCNView(Bounds)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
            AllowsCameraControl = true,
            BackgroundColor = UIColor.Black,
            Scene = new SCNScene(),
        };

        _sphere = SCNSphere.Create(10.0f);
        _sphere.SegmentCount = 96;

        _sphereMaterial = new SCNMaterial
        {
            DoubleSided = true,
            LightingModelName = SCNLightingModel.Constant,
        };
        _sphere.FirstMaterial = _sphereMaterial;

        var sphereNode = SCNNode.FromGeometry(_sphere);
        _sceneView.Scene!.RootNode.AddChildNode(sphereNode);

        var cameraNode = new SCNNode
        {
            Camera = new SCNCamera
            {
                FieldOfView = 75.0f,
                ZNear = 0.01,
                ZFar = 100.0,
            },
            Position = SCNVector3.Zero,
        };
        _sceneView.Scene.RootNode.AddChildNode(cameraNode);
        _sceneView.PointOfView = cameraNode;

        AddSubview(_sceneView);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _sceneView?.Dispose();
        _sphere?.Dispose();
        _sphereMaterial?.Dispose();
    }

    private Uri? _source;

    // Image source
    public Uri? Source
    {
        get { return _source; }
        set
        {
            if (_source == value)
            {
                return;
            }

            _source = value;
            _ = UpdateSourceAsync(value, ++_sourceRequestId);
        }
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        _sceneView.Frame = Bounds;
    }

    private async Task UpdateSourceAsync(Uri? source, int requestId)
    {
        UIImage? image = await LoadImageAsync(source).ConfigureAwait(false);

        if (requestId != _sourceRequestId)
        {
            image?.Dispose();
            return;
        }
        InvokeOnMainThread(() =>
        {
            if (requestId != _sourceRequestId)
            {
                image?.Dispose();
                return;
            }

            _sphereMaterial.Diffuse.Contents = image;
        });
    }

    private static Task<UIImage?> LoadImageAsync(Uri? source)
    {
        if (source is null || string.IsNullOrEmpty(source.OriginalString))
        {
            return Task.FromResult<UIImage?>(null);
        }

        if (source.IsFile)
        {
            return Task.FromResult(UIImage.FromFile(source.LocalPath));
        }

        if (!source.IsAbsoluteUri)
        {
            return Task.FromResult(UIImage.FromBundle(source.OriginalString));
        }

        if (source.Scheme == Uri.UriSchemeHttp || source.Scheme == Uri.UriSchemeHttps)
        {
            return LoadRemoteImageAsync(source);
        }

        return Task.FromResult(UIImage.FromBundle(source.LocalPath.TrimStart('/')));
    }

    private static Task<UIImage?> LoadRemoteImageAsync(Uri source)
    {
        TaskCompletionSource<UIImage?> completionSource = new();

        using NSUrl url = NSUrl.FromString(source.AbsoluteUri)!;
        NSUrlSession.SharedSession.CreateDataTask(url, (data, response, error) =>
        {
            if (error is not null || data is null)
            {
                completionSource.TrySetResult(null);
                return;
            }

            completionSource.TrySetResult(UIImage.LoadFromData(data));
        }).Resume();

        return completionSource.Task;
    }
}
#endif