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
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Microsoft.Maui.Handlers;
using PanoramaFrame = Esri.ArcGISRuntime.Toolkit.Maui.OrientedImagePanoramicDisplay.PanoramaFrame;
#if __ANDROID__ || __IOS__
using PlatformPanoramicSurface = Esri.ArcGISRuntime.Toolkit.Maui.Primitives.PanoramicSurface;
#else
using PlatformPanoramicSurface = Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramicSurface;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// MAUI view over the platform panorama surface. It forwards to the platform view once the handler connects, and stashes
// anything set before then.
internal sealed class PanoramicSurfaceView : Microsoft.Maui.Controls.View
{
    private PlatformPanoramicSurface? _platform;

    // Stash for state set before the platform view exists; camera is write-through (kept here, pushed on attach).
    private IReadOnlyList<PanoramaMarker>? _pendingMarkers;
    private (float R, float G, float B, float A)? _pendingClearColor;
    private PanoramaCameraState _camera = PanoramaCameraState.Initial;
    private bool _everHadTexture;
    private PanoramaFrame? _pendingFrame;

    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    public event Action? DeviceLost;

    public event Action? DeviceRecreated;

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

    // 1 until the platform view exists. Attaching one raises ScaleChanged.
    public double PixelsPerDip => _platform?.PixelsPerDip ?? 1;

    public void SetTexture(PanoramaFrame frame)
    {
        _everHadTexture = true;
        DiscardPendingFrame();
        if (_platform is { } platform)
            ApplyTexture(platform, frame);
        else
            _pendingFrame = frame;
    }

    private void DiscardPendingFrame()
    {
        if (_pendingFrame is { } frame)
            frame.Discard();
        _pendingFrame = null;
    }

    private static void ApplyTexture(PlatformPanoramicSurface platform, PanoramaFrame frame)
    {
#if __ANDROID__
        platform.SetTexture(frame.Bitmap);
#elif __IOS__
        platform.SetTexture(frame.Texture);
#else
        platform.SetTexture(frame.Bgra, (uint)frame.Width, (uint)frame.Height);
#endif
    }

    public void ClearTexture()
    {
        _everHadTexture = false;
        DiscardPendingFrame();
        _platform?.ClearTexture();
    }

    public void SetMarkers(IReadOnlyList<PanoramaMarker> markers)
    {
        if (_platform is not null)
        {
            _pendingMarkers = null;
            _platform.SetMarkers(markers);
        }
        else
        {
            _pendingMarkers = markers;
        }
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
        platform.DeviceLost += OnPlatformDeviceLost;
        platform.DeviceRecreated += OnPlatformDeviceRecreated;
        platform.ViewChanged += OnPlatformViewChanged;
        platform.ScaleChanged += OnPlatformScaleChanged;

        platform.Camera = _camera;
        if (_pendingClearColor is (float r, float g, float b, float a))
            platform.SetClearColor(r, g, b, a);

        if (_pendingFrame is { } frame)
        {
            // Clearing the stash prevents teardown from discarding a texture handed to the surface.
            _pendingFrame = null;
            ApplyTexture(platform, frame);
        }
        else if (_everHadTexture)
        {
            // Reconnected with no stashed content: the previous platform surface (and its texture) is gone.
            DeviceRecreated?.Invoke();
        }

        if (_pendingMarkers is not null)
        {
            IReadOnlyList<PanoramaMarker> markers = _pendingMarkers;
            _pendingMarkers = null;
            platform.SetMarkers(markers);
        }

        // Markers rasterized before now used a scale of 1.
        ScaleChanged?.Invoke();
        platform.RequestRender();
    }

    internal void DetachPlatformSurface()
    {
        if (_platform is not null)
        {
            // Keep the last camera so a re-attach resumes the same view.
            _camera = _platform.Camera;
            _platform.SurfaceTapped -= OnPlatformTapped;
            _platform.RenderFailed -= OnPlatformRenderFailed;
            _platform.DeviceLost -= OnPlatformDeviceLost;
            _platform.DeviceRecreated -= OnPlatformDeviceRecreated;
            _platform.ViewChanged -= OnPlatformViewChanged;
            _platform.ScaleChanged -= OnPlatformScaleChanged;
        }

        _platform = null;
    }

    private void OnPlatformTapped(double x, double y) => SurfaceTapped?.Invoke(x, y);

    private void OnPlatformRenderFailed(Exception ex) => RenderFailed?.Invoke(ex);

    private void OnPlatformDeviceLost() => DeviceLost?.Invoke();

    private void OnPlatformDeviceRecreated() => DeviceRecreated?.Invoke();

    private void OnPlatformViewChanged() => ViewChanged?.Invoke();

    private void OnPlatformScaleChanged() => ScaleChanged?.Invoke();
}

// Maps the virtual view to the platform panorama surface. Registered by UseArcGISToolkit.
internal sealed class PanoramicSurfaceViewHandler : ViewHandler<PanoramicSurfaceView, PlatformPanoramicSurface>
{
    public static readonly IPropertyMapper<PanoramicSurfaceView, PanoramicSurfaceViewHandler> Mapper =
        new PropertyMapper<PanoramicSurfaceView, PanoramicSurfaceViewHandler>(ViewMapper)
        {
#if WINDOWS
            // SwapChainPanel rejects Background (even ClearValue throws) and the base ViewMapper sets it on connect, which
            // would unwind out of the host's set_Content and leave the display unhosted. The surface paints its own backdrop.
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
        // Releases the textures, not the view, which UIKit can still call during teardown.
        platformView.ReleaseResources();
#else
        // On Windows the surface releases its device resources from its own Unloaded handler.
#endif
        base.DisconnectHandler(platformView);
    }
}
#endif
