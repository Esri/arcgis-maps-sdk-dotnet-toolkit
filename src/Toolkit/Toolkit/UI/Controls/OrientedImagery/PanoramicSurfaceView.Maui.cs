// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if __ANDROID__ || __IOS__ || (MAUI && WINDOWS)
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Microsoft.Maui.Handlers;
#if __ANDROID__ || __IOS__
using PlatformPanoramicSurface = Esri.ArcGISRuntime.Toolkit.Maui.Primitives.PanoramicSurface;
#else
using PlatformPanoramicSurface = Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramicSurface;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// Forwards to the platform surface once the handler connects, and stashes state set before then.
internal sealed class PanoramicSurfaceView : Microsoft.Maui.Controls.View
{
    private PlatformPanoramicSurface? _platform;
    private IReadOnlyList<PanoramaMarker> _markers = [];
    private (float R, float G, float B, float A)? _pendingClearColor;
    private PanoramaCameraState _camera = PanoramaCameraState.Initial;
    private string? _texturePath; // decoded again for a platform surface attached without the texture

    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    public event Action? ViewChanged;

    public event Action? ScaleChanged;

    public PanoramaCameraState Camera
    {
        get => _platform?.Camera ?? _camera;
        set
        {
            _camera = value;
            if (_platform is not null)
                _platform.Camera = value;
        }
    }

    // In DIPs, like tap positions.
    public double ActualWidth => _platform?.ActualWidth ?? 0;

    public double ActualHeight => _platform?.ActualHeight ?? 0;

    public double PixelsPerDip => _platform?.PixelsPerDip ?? 1;

    // Without a platform surface, the frame is dropped and the file decoded again when one attaches.
    public void SetTexture(PanoramaFrame frame, string path)
    {
        _texturePath = path;
        if (_platform is { } platform)
            platform.SetTexture(frame, path);
        else
            frame.Dispose();
    }

    public void ClearTexture()
    {
        _texturePath = null;
        _platform?.ClearTexture();
    }

    public void SetMarkers(IReadOnlyList<PanoramaMarker> markers)
    {
        _markers = markers;
        _platform?.SetMarkers(markers);
    }

    public void SetClearColor(float r, float g, float b, float a)
    {
        _pendingClearColor = (r, g, b, a);
        _platform?.SetClearColor(r, g, b, a);
    }

    public void RequestRender() => _platform?.RequestRender();

    internal void AttachPlatformSurface(PlatformPanoramicSurface platform)
    {
        _platform = platform;
        platform.SurfaceTapped += OnPlatformTapped;
        platform.RenderFailed += OnPlatformRenderFailed;
        platform.ViewChanged += OnPlatformViewChanged;
        platform.ScaleChanged += OnPlatformScaleChanged;

        platform.Camera = _camera;
        if (_pendingClearColor is (float r, float g, float b, float a))
            platform.SetClearColor(r, g, b, a);
        if (_texturePath is string path)
            _ = RestoreTextureAsync(platform, path);
        platform.SetMarkers(_markers);

        // Markers rasterized before attach used a scale of 1.
        ScaleChanged?.Invoke();
        platform.RequestRender();
    }

    // The texture went with the previous platform surface, or none was attached when it was set.
    private async Task RestoreTextureAsync(PlatformPanoramicSurface platform, string path)
    {
        try
        {
            PanoramaFrame frame = await PanoramaFrame.DecodeAsync(path, CancellationToken.None);
            if (ReferenceEquals(platform, _platform) && path == _texturePath)
                platform.SetTexture(frame, path);
            else
                frame.Dispose();
        }
        catch (Exception ex)
        {
            if (path == _texturePath)
                RenderFailed?.Invoke(ex);
        }
    }

    internal void DetachPlatformSurface()
    {
        if (_platform is not null)
        {
            // Keep the last camera so a re-attach resumes the same view.
            _camera = _platform.Camera;
            _platform.SurfaceTapped -= OnPlatformTapped;
            _platform.RenderFailed -= OnPlatformRenderFailed;
            _platform.ViewChanged -= OnPlatformViewChanged;
            _platform.ScaleChanged -= OnPlatformScaleChanged;
        }

        _platform = null;
    }

    private void OnPlatformTapped(double x, double y) => SurfaceTapped?.Invoke(x, y);

    private void OnPlatformRenderFailed(Exception ex) => RenderFailed?.Invoke(ex);

    private void OnPlatformViewChanged() => ViewChanged?.Invoke();

    private void OnPlatformScaleChanged() => ScaleChanged?.Invoke();
}

internal sealed class PanoramicSurfaceViewHandler : ViewHandler<PanoramicSurfaceView, PlatformPanoramicSurface>
{
    public static readonly IPropertyMapper<PanoramicSurfaceView, PanoramicSurfaceViewHandler> Mapper =
        new PropertyMapper<PanoramicSurfaceView, PanoramicSurfaceViewHandler>(ViewMapper)
        {
#if WINDOWS
            // SwapChainPanel throws on any Background change, even ClearValue, and the base ViewMapper sets it on
            // connect, which would leave the panoramic viewport unhosted. The surface paints its own backdrop.
            [nameof(Microsoft.Maui.IView.Background)] = MapBackgroundNoOp,
#endif
        };

#if WINDOWS
    private static void MapBackgroundNoOp(PanoramicSurfaceViewHandler handler, PanoramicSurfaceView view)
    {
    }
#endif

    public PanoramicSurfaceViewHandler()
        : base(Mapper)
    {
    }

#if __ANDROID__
    protected override PlatformPanoramicSurface CreatePlatformView() => new(Context);
#else
    protected override PlatformPanoramicSurface CreatePlatformView() => new();
#endif

    protected override void ConnectHandler(PlatformPanoramicSurface platformView)
    {
        base.ConnectHandler(platformView);
        VirtualView.AttachPlatformSurface(platformView);
    }

    protected override void DisconnectHandler(PlatformPanoramicSurface platformView)
    {
        VirtualView.DetachPlatformSurface();
#if __ANDROID__
        platformView.Dispose(); // stops the render thread and releases EGL
#elif __IOS__
        // Releases only the textures. UIKit can still call the view during teardown.
        platformView.ReleaseResources();
#else
        // The surface releases its device resources on Unloaded.
#endif
        base.DisconnectHandler(platformView);
    }
}
#endif
