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

#if WINDOWS_XAML || (MAUI && WINDOWS)
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.System;
#if MAUI
// The WinUI head supplies these as project-level usings; the MAUI head does not.
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
#endif
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;
using WinRT;
using static Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramaCameraState;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

// WinUI present layer: hosts the shared D3D11 core (PanoramicSurface.cs) in a SwapChainPanel, owning the DXGI
// composition swap chain and an on-demand render pump. The core renders the panorama into the swap-chain back buffer.
internal sealed unsafe partial class PanoramicSurface : SwapChainPanel
{
    private IDXGISwapChain1* _swapchain;
    private ID3D11RenderTargetView* _backBufferView;
    private bool _renderHooked;

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
        LostFocus += (_, _) => StopKeyboardNavigation();

        // Take focus when pressed, and mark the release handled: an unhandled left-button release bubbles to the focusable
        // ScrollViewer at the root of the window, which takes the focus back.
        // See https://github.com/microsoft/microsoft-ui-xaml/issues/6330.
        PointerPressed += (_, _) => Focus(FocusState.Pointer);
        PointerReleased += (_, e) => e.Handled = true;
        Tapped += OnTapped;
    }

    // The display rasterizes markers at this scale. Before the first composition, it is the XAML root's.
    internal double PixelsPerDip => CompositionScaleX > 0 ? CompositionScaleX : XamlRoot?.RasterizationScale ?? 1;

    private void OnCompositionScaleChanged(SwapChainPanel sender, object args)
    {
        Safe(EnsureOrResize);
        ScaleChanged?.Invoke();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Safe(EnsureOrResize);
        ViewChanged?.Invoke();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Safe(HandleLoaded);

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnhookRendering();
        ReleasePresentResources();
        ReleaseDeviceResources();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        Windows.Foundation.Point position = e.GetPosition(this);
        SurfaceTapped?.Invoke(position.X, position.Y);
    }

    private void EnsureOrResize()
    {
        if (_swapchain is null)
            EnsureResources();
        else
            CreateSizeDependentResources();

        RequestRender();
    }

    private (uint Width, uint Height) GetPixelSize()
    {
        // CompositionScaleX/Y is the DIP-to-pixel factor for a SwapChainPanel (it folds in parent transforms;
        // XamlRoot.RasterizationScale does not). The buffer is sized in pixels and mapped back to DIPs by ApplySwapChainScale.
        float scaleX = CompositionScaleX <= 0 ? 1f : CompositionScaleX;
        float scaleY = CompositionScaleY <= 0 ? 1f : CompositionScaleY;
        uint width = (uint)Math.Max(1, Math.Ceiling(ActualWidth * scaleX));
        uint height = (uint)Math.Max(1, Math.Ceiling(ActualHeight * scaleY));
        return (width, height);
    }

    private partial void EnsureResources()
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

        IDXGIDevice* dxgiDevice = null;
        IDXGIAdapter* adapter = null;
        IDXGIFactory2* factory = null;
        try
        {
            Guid dxgiDeviceIid = IDXGIDevice.IID_Guid;
            ((IUnknown*)Device)->QueryInterface(&dxgiDeviceIid, (void**)&dxgiDevice).ThrowOnFailure();
            dxgiDevice->GetAdapter(&adapter);
            Guid factoryIid = IDXGIFactory2.IID_Guid;
            adapter->GetParent(&factoryIid, (void**)&factory);

            IDXGISwapChain1* swapchain;
            factory->CreateSwapChainForComposition((IUnknown*)Device, &desc, null, &swapchain);
            _swapchain = swapchain;

            // Bind the swap chain to this SwapChainPanel via the COM interop interface (AOT-safe ComWrappers).
            ISwapChainPanelNative panelNative = this.As<ISwapChainPanelNative>();
            panelNative.SetSwapChain((nint)swapchain);
        }
        finally
        {
            Release(ref factory);
            Release(ref adapter);
            Release(ref dxgiDevice);
        }
    }

    private void CreateSizeDependentResources()
    {
        if (_swapchain is null || Device is null)
            return;

        (uint width, uint height) = GetPixelSize();

        // ResizeBuffers fails while any back-buffer reference is alive, including the RTV still bound on the context
        // from the last frame. Unbind it, then release the view.
        if (Context is not null)
            Context->OMSetRenderTargets(0, (ID3D11RenderTargetView**)null, null);

        Release(ref _backBufferView);

        // ResizeBuffers throws on a device-loss HRESULT; the caller's Safe routes that to recovery.
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
            Release(ref backBuffer);
        }

        ApplySwapChainScale();
    }

    // A composition swap chain maps its pixel buffer 1:1 into DIP space, so at >100% DPI the image is zoomed and
    // anchored top-left. The inverse-scale transform shows it at the correct DIP size.
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

    private partial void HookRendering()
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

    private void OnRendering(object? sender, object e) => RenderTick();

    private partial bool IsReadyToRender() => _swapchain is not null && _backBufferView is not null;

    private partial bool PresentResourcesReady() => _swapchain is not null;

    private partial void RenderFrame()
    {
        (uint width, uint height) = GetPixelSize();
        RenderScene(_backBufferView, width, height);
        _swapchain->Present(1, 0);
    }

    private partial void ReleasePresentResources()
    {
        Release(ref _backBufferView);
        Release(ref _swapchain);
    }

    // TODO: Set e.Handled, so a ScrollViewer around the panorama doesn't also scroll. Test on Windows.
    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        int delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        FieldOfView = Math.Clamp(FieldOfView + (delta > 0 ? -WheelZoomStep : WheelZoomStep), MinFieldOfView, MaxFieldOfView);
        RequestRender();
    }

    private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        float scale = DragRotationScale(FieldOfView, ActualHeight);
        Yaw -= (float)e.Delta.Translation.X * scale;
        Pitch = Math.Clamp(Pitch - ((float)e.Delta.Translation.Y * scale), MinPitch, MaxPitch);
        if (e.Delta.Scale != 0 && e.Delta.Scale != 1f)
            FieldOfView = Math.Clamp(FieldOfView / e.Delta.Scale, MinFieldOfView, MaxFieldOfView);

        RequestRender();
    }

    // The main-keyboard plus and minus keys, VK_OEM_PLUS and VK_OEM_MINUS, have no named VirtualKey values.
    private const VirtualKey OemPlus = (VirtualKey)0xBB;
    private const VirtualKey OemMinus = (VirtualKey)0xBD;

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        NavigationKeys key = e.Key switch
        {
            VirtualKey.Left => NavigationKeys.Left,
            VirtualKey.Right => NavigationKeys.Right,
            VirtualKey.Up => NavigationKeys.Up,
            VirtualKey.Down => NavigationKeys.Down,
            VirtualKey.Add or OemPlus => NavigationKeys.ZoomIn,
            VirtualKey.Subtract or OemMinus => NavigationKeys.ZoomOut,
            _ => NavigationKeys.None,
        };

        // Arrows pressed with a modifier don't count as held (see GetHeldNavigationKeys) and are left to the app.
        if (key == NavigationKeys.None || !GetHeldNavigationKeys().HasFlag(key))
            return;

        // Handled, so arrow keys do not also move focus to a neighboring control.
        e.Handled = true;
        StartKeyboardNavigation();
    }

    // As in the SDK MapView, arrows count only without Ctrl, Alt, or Shift, and plus and minus count with any
    // modifier, since Shift types "+" on the main keyboard.
    private partial NavigationKeys GetHeldNavigationKeys()
    {
        NavigationKeys keys = NavigationKeys.None;
        if (!IsKeyDown(VirtualKey.Control) && !IsKeyDown(VirtualKey.Menu) && !IsKeyDown(VirtualKey.Shift))
        {
            if (IsKeyDown(VirtualKey.Left))
                keys |= NavigationKeys.Left;
            if (IsKeyDown(VirtualKey.Right))
                keys |= NavigationKeys.Right;
            if (IsKeyDown(VirtualKey.Up))
                keys |= NavigationKeys.Up;
            if (IsKeyDown(VirtualKey.Down))
                keys |= NavigationKeys.Down;
        }

        if (IsKeyDown(OemPlus) || IsKeyDown(VirtualKey.Add))
            keys |= NavigationKeys.ZoomIn;
        if (IsKeyDown(OemMinus) || IsKeyDown(VirtualKey.Subtract))
            keys |= NavigationKeys.ZoomOut;

        return keys;
    }

    private static bool IsKeyDown(VirtualKey key) =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
}

// SwapChainPanel <-> DXGI swap chain binding (windows.ui.xaml.media.dxinterop.h)
[GeneratedComInterface]
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
internal partial interface ISwapChainPanelNative
{
    void SetSwapChain(nint swapChain);
}
#endif
