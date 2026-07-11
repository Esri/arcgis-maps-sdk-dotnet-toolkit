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

#if __ANDROID__ || (MAUI && WINDOWS)
using System;
using System.Collections.Generic;
using Microsoft.Maui.Handlers;
#if __ANDROID__
using PlatformPanoramicSurface = Esri.ArcGISRuntime.Toolkit.Maui.Primitives.PanoramicSurface;
#else
using PlatformPanoramicSurface = Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramicSurface;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// MAUI virtual view for the platform panorama surface (GLES TextureView on Android; the Windows heads' D3D11
// SwapChainPanel surface on MAUI-Windows), hosted by OrientedImagePanoramicDisplay. It mirrors the surface API
// the display consumes (camera, texture, markers, events) and forwards to the platform view once the handler
// connects, stashing content set before that (the platform view only exists while attached to a window).
// On re-attach after a disconnect (the platform surface and its GPU resources were disposed), it raises
// DeviceRecreated so the display re-supplies the panorama - the same recovery contract the platform surfaces
// use for in-place device/context loss.
internal sealed class PanoramicSurfaceView : Microsoft.Maui.Controls.View
{
    private PlatformPanoramicSurface? _platform;

    // Stash for state set before the platform view exists; camera is write-through (kept here, pushed on attach).
    private IReadOnlyList<PlatformPanoramicSurface.MarkerSwatch>? _pendingMarkers;
    private (float R, float G, float B, float A)? _pendingClearColor;
    private float _yaw;
    private float _pitch;
    private float _fieldOfView = MathF.PI / 2f;
    private bool _everHadTexture;
#if __ANDROID__
    private Android.Graphics.Bitmap? _pendingBitmap;
#else
    private (byte[] Bgra, uint Width, uint Height)? _pendingFrame;
#endif

    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    public event Action? DeviceRecreated;

    public event Action? CameraChanged;

    public float Yaw
    {
        get => _platform?.Yaw ?? _yaw;
        set
        {
            _yaw = value;
            if (_platform is not null)
                _platform.Yaw = value;
        }
    }

    public float Pitch
    {
        get => _platform?.Pitch ?? _pitch;
        set
        {
            _pitch = value;
            if (_platform is not null)
                _platform.Pitch = value;
        }
    }

    public float FieldOfView
    {
        get => _platform?.FieldOfView ?? _fieldOfView;
        set
        {
            _fieldOfView = value;
            if (_platform is not null)
                _platform.FieldOfView = value;
        }
    }

    // The platform view's size in its own screen units: physical pixels on Android, DIPs on Windows.
    // Tap coordinates and hit-testing use the same unit, so the display's math stays consistent.
    public double ActualWidth => _platform?.ActualWidth ?? 0;

    public double ActualHeight => _platform?.ActualHeight ?? 0;

#if __ANDROID__
    public void SetTexture(Android.Graphics.Bitmap bitmap)
    {
        _everHadTexture = true;
        if (_platform is not null)
        {
            _pendingBitmap = null;
            _platform.SetTexture(bitmap);
        }
        else
        {
            _pendingBitmap?.Recycle();
            _pendingBitmap = bitmap;
        }
    }
#else
    public void SetTexture(byte[] bgra, uint width, uint height)
    {
        _everHadTexture = true;
        if (_platform is not null)
        {
            _pendingFrame = null;
            _platform.SetTexture(bgra, width, height);
        }
        else
        {
            _pendingFrame = (bgra, width, height);
        }
    }
#endif

    public void ClearTexture()
    {
        _everHadTexture = false;
#if __ANDROID__
        _pendingBitmap?.Recycle();
        _pendingBitmap = null;
#else
        _pendingFrame = null;
#endif
        _platform?.ClearTexture();
    }

    public void SetMarkers(IReadOnlyList<PlatformPanoramicSurface.MarkerSwatch> swatches)
    {
        if (_platform is not null)
        {
            _pendingMarkers = null;
            _platform.SetMarkers(swatches);
        }
        else
        {
            _pendingMarkers = swatches;
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
        platform.DeviceRecreated += OnPlatformDeviceRecreated;
        platform.CameraChanged += OnPlatformCameraChanged;

        platform.Yaw = _yaw;
        platform.Pitch = _pitch;
        platform.FieldOfView = _fieldOfView;
        if (_pendingClearColor is (float r, float g, float b, float a))
            platform.SetClearColor(r, g, b, a);

        bool applied = false;
#if __ANDROID__
        if (_pendingBitmap is not null)
        {
            Android.Graphics.Bitmap bitmap = _pendingBitmap;
            _pendingBitmap = null;
            platform.SetTexture(bitmap);
            applied = true;
        }
#else
        if (_pendingFrame is ({ } bgra, uint width, uint height))
        {
            _pendingFrame = null;
            platform.SetTexture(bgra, width, height);
            applied = true;
        }
#endif
        if (!applied && _everHadTexture)
        {
            // Reconnected with no stashed content: the previous platform surface (and its texture) is gone.
            DeviceRecreated?.Invoke();
        }

        if (_pendingMarkers is not null)
        {
            IReadOnlyList<PlatformPanoramicSurface.MarkerSwatch> markers = _pendingMarkers;
            _pendingMarkers = null;
            platform.SetMarkers(markers);
        }

        platform.RequestRender();
    }

    internal void DetachPlatformSurface()
    {
        if (_platform is not null)
        {
            // Keep the last camera so a re-attach resumes the same view.
            _yaw = _platform.Yaw;
            _pitch = _platform.Pitch;
            _fieldOfView = _platform.FieldOfView;
            _platform.SurfaceTapped -= OnPlatformTapped;
            _platform.RenderFailed -= OnPlatformRenderFailed;
            _platform.DeviceRecreated -= OnPlatformDeviceRecreated;
            _platform.CameraChanged -= OnPlatformCameraChanged;
        }

        _platform = null;
    }

    private void OnPlatformTapped(double x, double y) => SurfaceTapped?.Invoke(x, y);

    private void OnPlatformRenderFailed(Exception ex) => RenderFailed?.Invoke(ex);

    private void OnPlatformDeviceRecreated() => DeviceRecreated?.Invoke();

    private void OnPlatformCameraChanged()
    {
        _yaw = _platform?.Yaw ?? _yaw;
        _pitch = _platform?.Pitch ?? _pitch;
        _fieldOfView = _platform?.FieldOfView ?? _fieldOfView;
        CameraChanged?.Invoke();
    }
}

// Maps the virtual view to the platform panorama surface. Registered by UseArcGISToolkit.
internal sealed class PanoramicSurfaceViewHandler : ViewHandler<PanoramicSurfaceView, PlatformPanoramicSurface>
{
    public static readonly IPropertyMapper<PanoramicSurfaceView, PanoramicSurfaceViewHandler> Mapper =
        new PropertyMapper<PanoramicSurfaceView, PanoramicSurfaceViewHandler>(ViewMapper)
        {
#if !__ANDROID__
            // WinUI SwapChainPanel rejects the Background property (even ClearValue throws), and the base
            // ViewMapper maps it on connect - which would unwind out of the host's set_Content and leave the
            // display unhosted. The surface paints its own backdrop (SetClearColor).
            [nameof(Microsoft.Maui.IView.Background)] = MapBackgroundNoOp,
#endif
        };

#if !__ANDROID__
    private static void MapBackgroundNoOp(PanoramicSurfaceViewHandler handler, PanoramicSurfaceView view)
    {
        // Intentionally empty: not supported on SwapChainPanel.
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
#endif
        // On Windows the surface releases its device resources from its own Unloaded handler.
        base.DisconnectHandler(platformView);
    }
}
#endif
