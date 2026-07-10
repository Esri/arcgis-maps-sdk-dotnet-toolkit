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

#if WPF
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;
using static Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramaCameraState;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

// WPF present layer for the shared D3D11 core (PanoramicSurface.cs). Renders on demand on the UI thread.
//
// Hardware path:
// Render into a shared D3D11 render target, mirror it to a D3D9Ex surface, and present via a WPF D3DImage.
//
// Software path (Remote Desktop / no GPU):
// Render into a render target, read back through a staging texture, and blit into a WriteableBitmap.
//
// The D3D11 side uses CsWin32. The legacy Direct3D9 COM API is NOT in the Win32 metadata,
// so the few D3D9Ex calls for the bridge dispatch through their documented vtable slots in the nested D3D9 helper.
internal sealed unsafe partial class PanoramicSurface : System.Windows.Controls.Image
{
    // D3D9Ex create flags: HARDWARE_VERTEXPROCESSING | MULTITHREADED | FPU_PRESERVE.
    private const uint D3D9CreateFlags = 0x40 | 0x4 | 0x2;
    private const uint D3D9SdkVersion = 32; // D3D_SDK_VERSION
    private const uint D3DDevTypeHal = 1;
    private const uint D3DSwapEffectDiscard = 1;
    private const uint D3DFmtUnknown = 0;
    private const uint D3DFmtA8R8G8B8 = 21;
    private const uint D3DPoolDefault = 0;
    private const uint D3DUsageRenderTarget = 0x1;

    private D3DImage? _d3dImage;
    private nint _d3d9;        // IDirect3D9Ex*
    private nint _device9;     // IDirect3DDevice9Ex*
    private nint _texture9;    // IDirect3DTexture9*
    private nint _surface9;    // IDirect3DSurface9*
    private ID3D11Texture2D* _sharedTexture;
    private ID3D11RenderTargetView* _sharedView;

    // Software present path (Remote Desktop / no-GPU / WARP):
    // render into a D3D11 render target, copy to a CPU-readable staging texture, and blit into a WriteableBitmap,
    // bypassing the D3D9Ex<->D3DImage shared-surface bridge (which cannot share a WARP texture and is unavailable under RDP).
    private bool _useSoftware;
    private ID3D11Texture2D* _renderTarget;
    private ID3D11RenderTargetView* _renderTargetView;
    private ID3D11Texture2D* _stagingTexture;
    private WriteableBitmap? _writeableBitmap;

    private bool _renderHooked;
    private bool _needsRender = true;
    private bool _deviceLost;
    private bool _deviceEverCreated; // distinguishes a reload (device existed before) from the first load
    private Point _lastMousePosition;
    private Point _mouseDownPosition;
    private bool _wasDragging; // the previous move sample had the left button pressed (so a delta is meaningful)
    private uint _surfaceWidth;
    private uint _surfaceHeight;

    public PanoramicSurface()
    {
        Focusable = true;
        Stretch = Stretch.Fill;
        UseLayoutRounding = true; // Avoids blur at fractional DPI by snapping the element to whole device pixels.

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    public void RequestRender() => _needsRender = true;

    protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
    {
        // An Image with no Source measures to (0,0). Report the available size so the parent allots real space.
        double width = double.IsInfinity(constraint.Width) ? 0d : constraint.Width;
        double height = double.IsInfinity(constraint.Height) ? 0d : constraint.Height;
        return new System.Windows.Size(width, height);
    }

    protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize)
    {
        // The base Image.ArrangeOverride sizes RenderSize to the Source's natural no-Source size (0,0),
        // which collapses the surface, so EnsureResources never sees a non-zero ActualWidth and the device is never created.
        // Fill the arranged slot instead; once Source is the D3DImage, Stretch.Fill draws it over this same area.
        return arrangeSize;
    }

    private (uint Width, uint Height) GetPixelSize()
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        uint width = (uint)Math.Max(1, Math.Ceiling(ActualWidth * dpi.DpiScaleX));
        uint height = (uint)Math.Max(1, Math.Ceiling(ActualHeight * dpi.DpiScaleY));
        return (width, height);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Safe(() =>
        {
            bool isReload = _deviceEverCreated;
            EnsureResources();
            HookRendering();
            RequestRender();
            if (IsDeviceInitialized)
            {
                _deviceEverCreated = true;
            }

            // On a RELOAD (RDP reconnect / display change tears down and rebuilds the visual tree, releasing the device
            // on Unloaded) the fresh device has no content and the stash was consumed, so ask the display to re-supply.
            // On the FIRST load the normal load path supplies it. Raising here would force a redundant second decode,
            // so gate on a prior device existing.
            if (isReload && IsDeviceInitialized && !HasTexture)
            {
                DeviceRecreated?.Invoke();
            }
        });
    }

    // Runs a present-layer step in a XAML event handler (outside the load path's try/catch)
    // routing any failure to so the hosting display surfaces it as Error instead of failing silently.
    private void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            RenderFailed?.Invoke(ex);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnhookRendering();
        ReleaseSurfaces();
        ReleaseDevice9();
        ReleaseDeviceResources();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Safe(() =>
        {
            EnsureResources();
            RequestRender();
        });
    }

    private void EnsureResources()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
            return;

        Initialize(); // core: create the D3D11 device and device resources

        _useSoftware = IsUsingWarp || ShouldUseSoftware();

        (uint width, uint height) = GetPixelSize();
        bool presentReady = _useSoftware ? _writeableBitmap is not null : _surface9 != 0;
        if (width == _surfaceWidth && height == _surfaceHeight && presentReady)
            return;

        if (_useSoftware)
        {
            CreateSoftwareSurfaces(width, height);
            return;
        }

        try
        {
            EnsureDevice9();
            CreateSurfaces(width, height);
        }
        catch
        {
            // The hardware bridge can fail at runtime (e.g. adapter mismatch, RDP).
            // Fall back to the software path rather than leaving the surface blank.
            ReleaseSurfaces();
            ReleaseDevice9();
            _useSoftware = true;
            CreateSoftwareSurfaces(width, height);
        }
    }

    private bool ShouldUseSoftware()
    {
        if ((RenderCapability.Tier >> 16) < 2)
            return true;

        if (RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly)
            return true;

        if (PresentationSource.FromVisual(this) is HwndSource hwnd && hwnd.CompositionTarget?.RenderMode == RenderMode.SoftwareOnly)
            return true;

        const int SM_REMOTESESSION = 0x1000;
        return GetSystemMetrics(SM_REMOTESESSION) != 0;
    }

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    private void CreateSoftwareSurfaces(uint width, uint height)
    {
        if (Device is null)
            return;

        ReleaseSurfaces();

        D3D11_TEXTURE2D_DESC targetDesc = new()
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET,
        };
        ID3D11Texture2D* target;
        Device->CreateTexture2D(&targetDesc, (D3D11_SUBRESOURCE_DATA*)null, &target);
        _renderTarget = target;

        ID3D11RenderTargetView* targetView;
        Device->CreateRenderTargetView((ID3D11Resource*)target, (D3D11_RENDER_TARGET_VIEW_DESC*)null, &targetView);
        _renderTargetView = targetView;

        D3D11_TEXTURE2D_DESC stagingDesc = targetDesc;
        stagingDesc.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
        stagingDesc.BindFlags = 0;
        stagingDesc.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
        ID3D11Texture2D* staging;
        Device->CreateTexture2D(&stagingDesc, (D3D11_SUBRESOURCE_DATA*)null, &staging);
        _stagingTexture = staging;

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        _writeableBitmap = new WriteableBitmap((int)width, (int)height, 96.0 * dpi.DpiScaleX, 96.0 * dpi.DpiScaleY, PixelFormats.Pbgra32, null);
        Source = _writeableBitmap;

        _surfaceWidth = width;
        _surfaceHeight = height;
    }

    private void EnsureDevice9()
    {
        if (_device9 != 0)
            return;

        ThrowIfFailed(D3D9.Direct3DCreate9Ex(D3D9SdkVersion, out _d3d9), "Direct3DCreate9Ex");

        nint focusWindow = D3D9.GetDesktopWindow();
        D3DPRESENT_PARAMETERS pp = new()
        {
            BackBufferWidth = 1,
            BackBufferHeight = 1,
            BackBufferFormat = D3DFmtUnknown,
            SwapEffect = D3DSwapEffectDiscard,
            hDeviceWindow = focusWindow,
            Windowed = 1,
        };
        ThrowIfFailed(D3D9.CreateDeviceEx(_d3d9, 0, D3DDevTypeHal, focusWindow, D3D9CreateFlags, &pp, out _device9), "CreateDeviceEx");
    }

    private void CreateSurfaces(uint width, uint height)
    {
        if (Device is null || _device9 == 0)
            return;

        ReleaseSurfaces();

        // D3D11 shared render target the core draws into (CsWin32).
        D3D11_TEXTURE2D_DESC desc = new()
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
            MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED,
        };

        ID3D11Texture2D* sharedTexture;
        Device->CreateTexture2D(&desc, (D3D11_SUBRESOURCE_DATA*)null, &sharedTexture);
        _sharedTexture = sharedTexture;

        ID3D11RenderTargetView* sharedView;
        Device->CreateRenderTargetView((ID3D11Resource*)sharedTexture, (D3D11_RENDER_TARGET_VIEW_DESC*)null, &sharedView);
        _sharedView = sharedView;

        HANDLE sharedHandle = GetSharedHandle(sharedTexture);

        // Open the same surface on the D3D9Ex device and point the D3DImage at it.
        ThrowIfFailed(D3D9.CreateTexture(_device9, width, height, D3DUsageRenderTarget, D3DFmtA8R8G8B8, D3DPoolDefault, out _texture9, sharedHandle), "CreateTexture(shared)");
        ThrowIfFailed(D3D9.GetSurfaceLevel(_texture9, 0, out _surface9), "GetSurfaceLevel");

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        _d3dImage = new D3DImage(96.0 * dpi.DpiScaleX, 96.0 * dpi.DpiScaleY);
        _d3dImage.IsFrontBufferAvailableChanged += OnFrontBufferAvailableChanged;
        _d3dImage.Lock();
        _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _surface9);
        _d3dImage.Unlock();
        Source = _d3dImage;

        _surfaceWidth = width;
        _surfaceHeight = height;
    }

    private static HANDLE GetSharedHandle(ID3D11Texture2D* texture)
    {
        IDXGIResource* resource;
        Guid riid = IDXGIResource.IID_Guid;
        ((IUnknown*)texture)->QueryInterface(&riid, (void**)&resource).ThrowOnFailure();
        try
        {
            HANDLE handle;
            resource->GetSharedHandle(&handle);
            return handle;
        }
        finally
        {
            _ = ((IUnknown*)resource)->Release();
        }
    }

    private void HookRendering()
    {
        if (!_renderHooked)
        {
            CompositionTarget.Rendering += OnRendering;
            _renderHooked = true;
        }
    }

    private void UnhookRendering()
    {
        if (_renderHooked)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderHooked = false;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_deviceLost && !TryRecoverDevice())
            return; // GPU still unavailable, retry next tick.

        // A missing texture is not a blocker. RenderHardware/RenderSoftware clear to a blank backdrop
        // (so a footprint change / failed load blanks the buffer instead of leaving the previous frame on screen).
        bool ready = Context is not null && (_useSoftware
            ? _writeableBitmap is not null && _renderTargetView is not null
            : _d3dImage is not null && _sharedView is not null && _d3dImage.IsFrontBufferAvailable);
        if (!_needsRender || !ready)
            return;

        try
        {
            if (_useSoftware)
                RenderSoftware();
            else
                RenderHardware();
        }
        catch (Exception ex)
        {
            // A device-removed during render is recoverable (rebuilt next tick, re-supplied via DeviceRecreated).
            // Do not surface it as Error. Only genuine render failures go to RenderFailed.
            if (IsDeviceRemoved)
            {
                _deviceLost = true;
                _needsRender = true;
                return;
            }

            RenderFailed?.Invoke(ex);
            return;
        }

        if (IsDeviceRemoved)
        {
            _deviceLost = true;
            _needsRender = true;
        }
    }

    // Rebuilds the device + all resources after a device-lost.
    // Returns false to retry on the next tick while the GPU is still unavailable.
    // DeviceRecreated then asks the display to re-supply the texture and markers.
    // The camera (plain properties on the surface) is preserved.
    private bool TryRecoverDevice()
    {
        try
        {
            ReleaseSurfaces();
            ReleaseDevice9();
            ReleaseDeviceResources();
            EnsureResources();
        }
        catch
        {
            return false;
        }

        if (!IsDeviceInitialized)
            return false;

        _deviceLost = false;
        _needsRender = true;
        DeviceRecreated?.Invoke(); // the display re-supplies the texture and markers (they were on the old device)
        return true;
    }

    private void RenderHardware()
    {
        if (HasTexture)
            RenderScene(_sharedView, _surfaceWidth, _surfaceHeight);
        else
            ClearTarget(_sharedView); // blank backdrop (no texture)

        Context->Flush(); // ensure D3D11 writes complete before WPF copies the D3D9 surface

        _d3dImage!.Lock();
        _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
        _d3dImage.Unlock();
        _needsRender = false;
    }

    private void RenderSoftware()
    {
        if (HasTexture)
            RenderScene(_renderTargetView, _surfaceWidth, _surfaceHeight);
        else
            ClearTarget(_renderTargetView); // blank backdrop (no texture)

        Context->CopyResource((ID3D11Resource*)_stagingTexture, (ID3D11Resource*)_renderTarget);

        D3D11_MAPPED_SUBRESOURCE mapped;
        Context->Map((ID3D11Resource*)_stagingTexture, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped);
        try
        {
            _writeableBitmap!.Lock();
            byte* src = (byte*)mapped.pData;
            byte* dst = (byte*)_writeableBitmap.BackBuffer;
            int dstStride = _writeableBitmap.BackBufferStride;
            uint rowBytes = _surfaceWidth * 4;
            for (uint y = 0; y < _surfaceHeight; y++)
            {
                Buffer.MemoryCopy(src + (y * mapped.RowPitch), dst + (y * dstStride), dstStride, rowBytes);
            }

            _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, (int)_surfaceWidth, (int)_surfaceHeight));
            _writeableBitmap.Unlock();
        }
        finally
        {
            Context->Unmap((ID3D11Resource*)_stagingTexture, 0);
        }

        _needsRender = false;
    }

    private void OnFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_d3dImage?.IsFrontBufferAvailable == true)
        {
            // Front buffer came back (e.g. RDP reconnect / display change).
            // Trigger the unified rebuild on the next render tick,
            // which also re-creates the D3D11 device if it was removed (not just the D3D9 surfaces).
            _deviceLost = true;
            RequestRender();
        }
    }

    private void ReleaseSurfaces()
    {
        if (_d3dImage is not null)
        {
            _d3dImage.IsFrontBufferAvailableChanged -= OnFrontBufferAvailableChanged;
            _d3dImage.Lock();
            _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            _d3dImage.Unlock();
            _d3dImage = null;
        }

        D3D9.Release(ref _surface9);
        D3D9.Release(ref _texture9);
        Release(ref _sharedView);
        Release(ref _sharedTexture);

        _writeableBitmap = null;
        Release(ref _renderTargetView);
        Release(ref _renderTarget);
        Release(ref _stagingTexture);

        _surfaceWidth = 0;
        _surfaceHeight = 0;
    }

    private void ReleaseDevice9()
    {
        D3D9.Release(ref _device9);
        D3D9.Release(ref _d3d9);
    }

    private static void ThrowIfFailed(int hr, string what)
    {
        if (hr < 0)
            throw new InvalidOperationException($"{what} failed (HRESULT 0x{hr:X8}).");
    }

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _mouseDownPosition = e.GetPosition(this);

        // Clear the drag anchor at the start of each gesture so the first pressed move re-seeds _lastMousePosition
        // instead of chaining a delta off the previous gesture (touch/RDP may deliver no hover move between gestures).
        _wasDragging = false;
        Focus();
        CaptureMouse();
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        // Drive the drag purely from move events + button state, never from the down event:
        // under Remote Desktop the down can be missed or input promoted, which would leave a stale anchor and a huge first delta.
        // _lastMousePosition tracks EVERY move (including button-up hover), so a delta is only ever applied
        // between two consecutive pressed moves. The anchor is always the immediately-preceding sample, so there is no first-frame jump.
        Point position = e.GetPosition(this);
        bool pressed = e.LeftButton == MouseButtonState.Pressed;
        if (pressed && _wasDragging)
        {
            float scale = DragRotationScale(FieldOfView, ActualHeight);
            Yaw -= (float)(position.X - _lastMousePosition.X) * scale;
            Pitch = Math.Clamp(Pitch - ((float)(position.Y - _lastMousePosition.Y) * scale), MinPitch, MaxPitch);
            RequestRender();
        }

        _lastMousePosition = position;
        _wasDragging = pressed;
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        ReleaseMouseCapture();
        _wasDragging = false; // reset in case the next gesture's button-down is missed or promoted (RDP)

        // A press-release with negligible movement is a tap (not a drag).
        Point position = e.GetPosition(this);
        if (Math.Abs(position.X - _mouseDownPosition.X) < 3 && Math.Abs(position.Y - _mouseDownPosition.Y) < 3)
            SurfaceTapped?.Invoke(position.X, position.Y);
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        FieldOfView = Math.Clamp(FieldOfView + (e.Delta > 0 ? -0.1f : 0.1f), MinFieldOfView, MaxFieldOfView);
        RequestRender();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Left:
                Yaw += KeyboardRotationDelta;
               break;
            case Key.Right:
                Yaw -= KeyboardRotationDelta;
                break;
            case Key.Up:
                Pitch = Math.Clamp(Pitch + KeyboardRotationDelta, MinPitch, MaxPitch);
                break;
            case Key.Down:
                Pitch = Math.Clamp(Pitch - KeyboardRotationDelta, MinPitch, MaxPitch);
                break;
            case Key.OemPlus or Key.Add:
                FieldOfView = Math.Clamp(FieldOfView * 0.9f, MinFieldOfView, MaxFieldOfView);
                break;
            case Key.OemMinus or Key.Subtract:
                FieldOfView = Math.Clamp(FieldOfView * 1.1f, MinFieldOfView, MaxFieldOfView);
                break;
            default: return;
        }

        RequestRender();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3DPRESENT_PARAMETERS
    {
        public uint BackBufferWidth;
        public uint BackBufferHeight;
        public uint BackBufferFormat;
        public uint BackBufferCount;
        public uint MultiSampleType;
        public uint MultiSampleQuality;
        public uint SwapEffect;
        public nint hDeviceWindow;
        public int Windowed;
        public int EnableAutoDepthStencil;
        public uint AutoDepthStencilFormat;
        public uint Flags;
        public uint FullScreen_RefreshRateInHz;
        public uint PresentationInterval;
    }

    // Direct3D9Ex interop based on COM vtable slots (d3d9.h):
    // - IUnknown::Release=2
    // - IDirect3D9Ex::CreateDeviceEx=20
    // - IDirect3DDevice9::CreateTexture=23
    // - IDirect3DTexture9::GetSurfaceLevel=18
    private static unsafe partial class D3D9
    {
        [LibraryImport("d3d9.dll")]
        internal static partial int Direct3DCreate9Ex(uint sdkVersion, out nint d3d9ex);

        [LibraryImport("user32.dll")]
        internal static partial nint GetDesktopWindow();

        internal static int CreateDeviceEx(nint self, uint adapter, uint deviceType, nint focusWindow, uint behaviorFlags, void* presentParams, out nint device)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, uint, nint, uint, void*, void*, nint*, int>)(*(void***)self)[20];
            nint result;
            int hr = fn(self, adapter, deviceType, focusWindow, behaviorFlags, presentParams, null, &result);
            device = result;
            return hr;
        }

        internal static int CreateTexture(nint device, uint width, uint height, uint usage, uint format, uint pool, out nint texture, nint sharedHandle)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, uint, uint, nint*, nint*, int>)(*(void***)device)[23];
            nint result;
            nint handle = sharedHandle;
            int hr = fn(device, width, height, 1, usage, format, pool, &result, &handle);
            texture = result;
            return hr;
        }

        internal static int GetSurfaceLevel(nint texture, uint level, out nint surface)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)(*(void***)texture)[18];
            nint result;
            int hr = fn(texture, level, &result);
            surface = result;
            return hr;
        }

        internal static void Release(ref nint comObject)
        {
            if (comObject != 0)
            {
                var fn = (delegate* unmanaged[Stdcall]<nint, uint>)(*(void***)comObject)[2];
                _ = fn(comObject);
                comObject = 0;
            }
        }
    }
}
#endif
