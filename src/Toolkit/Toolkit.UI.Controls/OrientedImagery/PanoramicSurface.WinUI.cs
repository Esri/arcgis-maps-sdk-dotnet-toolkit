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

#if WINDOWS_XAML
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.System;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;
using WinRT;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

// WinUI present layer: hosts the shared D3D11 core (PanoramicSurface.cs) in a SwapChainPanel, owning the DXGI
// composition swap chain and an on-demand render pump. The core renders the panorama into the swap-chain back buffer.
internal sealed unsafe partial class PanoramicSurface : SwapChainPanel
{
    private const float MinPitch = -(MathF.PI / 2f) + 0.01f;
    private const float MaxPitch = (MathF.PI / 2f) - 0.01f;
    private const float MinFieldOfView = 50f * MathF.PI / 180f;
    private const float MaxFieldOfView = 120f * MathF.PI / 180f;
    private const float MouseRotationScale = 0.0035f;
    private const float KeyboardRotationDelta = MathF.PI / 90f;

    private IDXGISwapChain1* _swapchain;
    private ID3D11RenderTargetView* _backBufferView;
    private bool _renderHooked;
    private bool _needsRender = true;
    private bool _deviceLost;
    private bool _deviceEverCreated; // distinguishes a reload (device existed before) from the first load

    public PanoramicSurface()
    {
        // Allow pan and pinch-zoom. Don't use ManipulationModes.All because it includes the Inertia flags.
        ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Scale;
        IsTabStop = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        CompositionScaleChanged += OnCompositionScaleChanged;
        PointerWheelChanged += OnPointerWheelChanged;
        ManipulationDelta += OnManipulationDelta;
        KeyDown += OnKeyDown;
        Tapped += OnTapped;
    }

    private void OnCompositionScaleChanged(SwapChainPanel sender, object args)
    {
        Safe(() =>
        {
            if (_swapchain is null)
                EnsureResources();
            else
                CreateSizeDependentResources();

            RequestRender();
        });
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        Windows.Foundation.Point position = e.GetPosition(this);
        SurfaceTapped?.Invoke(position.X, position.Y);
    }

    // Re-renders on the next composition tick (on-demand: only when the camera, texture, or size changed).
    public void RequestRender() => _needsRender = true;

    // Runs a lifecycle step (device/swap-chain/resource creation) in a XAML event handler, outside the render loop's
    // own try/catch, routing any failure to RenderFailed so the hosting display surfaces it as Error.
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Safe(() =>
        {
            bool isReload = _deviceEverCreated;
            EnsureResources();
            HookRendering();
            RequestRender();
            if (IsDeviceInitialized)
                _deviceEverCreated = true;

            // On a RELOAD the fresh device has no content and the stash was consumed, so ask the display to re-supply.
            // On the FIRST load the normal load path supplies it. Raising here would force a redundant second decode.
            if (isReload && IsDeviceInitialized && !HasTexture)
                DeviceRecreated?.Invoke();
        });
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnhookRendering();
        ReleaseSwapChain();
        ReleaseDeviceResources();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Safe(() =>
        {
            if (_swapchain is null)
                EnsureResources();
            else
                CreateSizeDependentResources();

            RequestRender();
        });
    }

    private (uint Width, uint Height) GetPixelSize()
    {
        // For a SwapChainPanel the authoritative DIP->physical-pixel factor is CompositionScaleX/Y
        // (it folds in any parent ScaleTransform), NOT XamlRoot.RasterizationScale.
        // The back buffer is sized in physical pixels and the swap chain is then scaled back to DIPs
        // via SetMatrixTransform (see ApplySwapChainScale) so it isn't displayed 1:1-in-DIP
        // (which would zoom and top-left-anchor the image at >100% DPI and appear off-center).
        float scaleX = CompositionScaleX <= 0 ? 1f : CompositionScaleX;
        float scaleY = CompositionScaleY <= 0 ? 1f : CompositionScaleY;
        uint width = (uint)Math.Max(1, Math.Ceiling(ActualWidth * scaleX));
        uint height = (uint)Math.Max(1, Math.Ceiling(ActualHeight * scaleY));
        return (width, height);
    }

    private void EnsureResources()
    {
        if (XamlRoot is null || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        Initialize(); // create the D3D device and device resources (core)
        if (_swapchain is null)
            CreateSwapChain();

        CreateSizeDependentResources();
    }

    private void CreateSwapChain()
    {
        if (Device is null)
            return;

        (uint width, uint height) = GetPixelSize();
        DXGI_SWAP_CHAIN_DESC1 desc = new()
        {
            Width = width,
            Height = height,
            Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
            Stereo = false,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            BufferUsage = DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = 2,
            Scaling = DXGI_SCALING.DXGI_SCALING_STRETCH,
            SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
            AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE,
            Flags = 0,
        };

        IDXGIDevice* dxgiDevice;
        Guid dxgiDeviceIid = IDXGIDevice.IID_Guid;
        ((IUnknown*)Device)->QueryInterface(&dxgiDeviceIid, (void**)&dxgiDevice).ThrowOnFailure();
        try
        {
            IDXGIAdapter* adapter;
            dxgiDevice->GetAdapter(&adapter);
            try
            {
                IDXGIFactory2* factory;
                Guid factoryIid = IDXGIFactory2.IID_Guid;
                adapter->GetParent(&factoryIid, (void**)&factory);
                try
                {
                    IDXGISwapChain1* swapchain;
                    factory->CreateSwapChainForComposition((IUnknown*)Device, &desc, null, &swapchain);
                    _swapchain = swapchain;

                    // Bind the swap chain to this SwapChainPanel via the COM interop interface (AOT-safe ComWrappers).
                    ISwapChainPanelNative panelNative = this.As<ISwapChainPanelNative>();
                    panelNative.SetSwapChain((nint)swapchain);
                }
                finally
                {
                    factory->Release();
                }
            }
            finally
            {
                adapter->Release();
            }
        }
        finally
        {
            dxgiDevice->Release();
        }
    }

    private void CreateSizeDependentResources()
    {
        if (_swapchain is null || Device is null)
            return;

        (uint width, uint height) = GetPixelSize();

        // ResizeBuffers requires that ALL references to the back buffers be released first, including the indirect
        // reference held by the device context while the back-buffer RTV is bound as a render target (it stays bound
        // across frames from RenderScene's OMSetRenderTargets). Unbind it, then release our view, before resizing.
        if (Context is not null)
            Context->OMSetRenderTargets(0, (ID3D11RenderTargetView**)null, null);

        if (_backBufferView is not null)
        {
            _ = ((IUnknown*)_backBufferView)->Release();
            _backBufferView = null;
        }

        _swapchain->ResizeBuffers(2, width, height, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, 0);

        ID3D11Texture2D* backBuffer;
        Guid texIid = ID3D11Texture2D.IID_Guid;
        _swapchain->GetBuffer(0, &texIid, (void**)&backBuffer);
        try
        {
            ID3D11RenderTargetView* view;
            Device->CreateRenderTargetView((ID3D11Resource*)backBuffer, (D3D11_RENDER_TARGET_VIEW_DESC*)null, &view);
            _backBufferView = view;
        }
        finally
        {
            _ = ((IUnknown*)backBuffer)->Release();
        }

        ApplySwapChainScale();
    }

    // A composition swap chain maps its (physical-pixel) back buffer 1:1 into the panel's DIP space unless a transform
    // is set. Without this, content is zoomed by the DPI factor and anchored at the top-left at >100% DPI, so the view
    // center sits up-and-left. The inverse-scale transform displays the physical-pixel buffer at the correct DIP size.
    private void ApplySwapChainScale()
    {
        if (_swapchain is null)
            return;

        IDXGISwapChain2* swapchain2;
        Guid iid = IDXGISwapChain2.IID_Guid;
        if (((IUnknown*)_swapchain)->QueryInterface(&iid, (void**)&swapchain2).Failed)
            return;

        try
        {
            float scaleX = CompositionScaleX <= 0 ? 1f : CompositionScaleX;
            float scaleY = CompositionScaleY <= 0 ? 1f : CompositionScaleY;
            DXGI_MATRIX_3X2_F transform = new() { _11 = 1f / scaleX, _22 = 1f / scaleY };
            swapchain2->SetMatrixTransform(&transform);
        }
        finally
        {
            _ = ((IUnknown*)swapchain2)->Release();
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

    private void OnRendering(object? sender, object e)
    {
        if (_deviceLost && !TryRecoverDevice())
            return; // GPU still unavailable (e.g. mid RDP-reconnect); retry next tick.

        if (!_needsRender || _swapchain is null || _backBufferView is null)
            return;

        (uint width, uint height) = GetPixelSize();
        try
        {
            if (HasTexture)
                RenderScene(_backBufferView, width, height);
            else
                ClearTarget(_backBufferView); // present a blank backdrop (no texture) instead of leaving a stale frame

            _swapchain->Present(1, 0);
        }
        catch (Exception ex)
        {
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
            return;
        }

        _needsRender = false;
    }

    // Rebuilds the device + all resources after a device-lost. Returns false to retry on the next tick
    // while the GPU is still unavailable. DeviceRecreated then asks the display to re-supply the texture + markers;
    // the camera (plain properties on the surface) is preserved.
    private bool TryRecoverDevice()
    {
        try
        {
            ReleaseSwapChain();
            ReleaseDeviceResources();
            EnsureResources();
        }
        catch
        {
            return false;
        }

        if (!IsDeviceInitialized || _swapchain is null)
            return false;

        _deviceLost = false;
        _needsRender = true;
        DeviceRecreated?.Invoke(); // the display re-supplies the texture + markers (they were on the old device)
        return true;
    }

    private void ReleaseSwapChain()
    {
        if (_backBufferView is not null)
        {
            _ = ((IUnknown*)_backBufferView)->Release();
            _backBufferView = null;
        }

        if (_swapchain is not null)
        {
            _ = ((IUnknown*)_swapchain)->Release();
            _swapchain = null;
        }
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        int delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        FieldOfView = Math.Clamp(FieldOfView + (delta > 0 ? -0.1f : 0.1f), MinFieldOfView, MaxFieldOfView);
        RequestRender();
    }

    private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        Yaw -= (float)e.Delta.Translation.X * MouseRotationScale;
        Pitch = Math.Clamp(Pitch - ((float)e.Delta.Translation.Y * MouseRotationScale), MinPitch, MaxPitch);
        if (e.Delta.Scale != 0 && e.Delta.Scale != 1f)
            FieldOfView = Math.Clamp(FieldOfView / e.Delta.Scale, MinFieldOfView, MaxFieldOfView);

        RequestRender();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Left:
                Yaw += KeyboardRotationDelta;
                break;
            case VirtualKey.Right:
                Yaw -= KeyboardRotationDelta;
                break;
            case VirtualKey.Up:
                Pitch = Math.Clamp(Pitch + KeyboardRotationDelta, MinPitch, MaxPitch);
                break;
            case VirtualKey.Down:
                Pitch = Math.Clamp(Pitch - KeyboardRotationDelta, MinPitch, MaxPitch);
                break;
            case VirtualKey.Add:
                FieldOfView = Math.Clamp(FieldOfView * 0.9f, MinFieldOfView, MaxFieldOfView);
                break;
            case VirtualKey.Subtract:
                FieldOfView = Math.Clamp(FieldOfView * 1.1f, MinFieldOfView, MaxFieldOfView);
                break;
            default: return;
        }

        RequestRender();
    }
}

// SwapChainPanel <-> DXGI swap chain binding (windows.ui.xaml.media.dxinterop.h)
[GeneratedComInterface]
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
internal partial interface ISwapChainPanelNative
{
    void SetSwapChain(nint swapChain);
}
#endif
