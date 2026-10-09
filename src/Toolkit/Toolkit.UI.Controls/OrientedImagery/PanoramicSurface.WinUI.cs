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
#if MAUI
// The MAUI head lacks the WinUI head's global usings for these.
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
#endif
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using WinRT;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

internal sealed unsafe partial class PanoramicSurface : SwapChainPanel
{
    private IDXGISwapChain1* _swapchain;
    private ID3D11RenderTargetView* _backBufferView;
    private bool _renderHooked;

    public PanoramicSurface()
    {
        // ManipulationModes.All would add inertia.
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

        // An unhandled left-button release bubbles to the window's root ScrollViewer, which
        // takes focus back. See https://github.com/microsoft/microsoft-ui-xaml/issues/6330.
        PointerPressed += (_, _) => Focus(FocusState.Pointer);
        PointerReleased += (_, e) => e.Handled = true;
        Tapped += OnTapped;
    }

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
        // CompositionScale, unlike XamlRoot.RasterizationScale, includes parent transforms.
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

        Initialize();
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

        // ResizeBuffers fails while any back-buffer reference is alive, even the RTV bound by the last frame.
        if (Context is not null)
            Context->OMSetRenderTargets(0, (ID3D11RenderTargetView**)null, null);

        Release(ref _backBufferView);

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

    // A composition swap chain maps pixels 1:1 to DIPs, so above 100% scale it shows zoomed unless inverse-scaled.
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

    private partial void RenderFrame()
    {
        (uint width, uint height) = GetPixelSize();
        RenderScene(_backBufferView, width, height);
        _swapchain->Present(1, 0);
    }

    private partial void ReleasePresentResources()
    {
        // The panel keeps the swap chain, and through it the device, alive until the binding is cleared.
        if (_swapchain is not null)
            this.As<ISwapChainPanelNative>().SetSwapChain(0);

        Release(ref _backBufferView);
        Release(ref _swapchain);
    }

    // Handled so an enclosing ScrollViewer does not scroll too. Precision touchpads send fractions of a 120 notch.
    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        Microsoft.UI.Input.PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsHorizontalMouseWheel)
            return;

        Camera = Camera.ZoomWheel(properties.MouseWheelDelta / 120f);
        RequestRender();
        e.Handled = true;
    }

    private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        PanoramaCameraState camera = Camera.Drag((float)e.Delta.Translation.X, (float)e.Delta.Translation.Y, ActualHeight);
        if (e.Delta.Scale != 0 && e.Delta.Scale != 1f)
            camera = camera.Zoom(e.Delta.Scale);

        Camera = camera;
        RequestRender();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (TryStartKeyboardNavigation((VIRTUAL_KEY)e.Key))
            e.Handled = true;
    }
}

// The decoder is here because C# doesn't allow async methods in the unsafe PanoramicSurface.
internal readonly partial record struct PanoramaFrame
{
    // Applies EXIF orientation to JPEGs only, as the SDK does.
    internal static async Task<PanoramaFrame> DecodeAsync(string path, CancellationToken token)
    {
        // The SDK owns the downloaded file, so open it shared, which StorageFile can't.
        using IRandomAccessStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).AsRandomAccessStream();
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
        token.ThrowIfCancellationRequested();
        bool orientJpeg = decoder.DecoderInformation.CodecId == BitmapDecoder.JpegDecoderId;
        PixelDataProvider pixels = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            new BitmapTransform(),
            orientJpeg ? ExifOrientationMode.RespectExifOrientation : ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);
        return new PanoramaFrame(pixels.DetachPixelData(),
            (int)(orientJpeg ? decoder.OrientedPixelWidth : decoder.PixelWidth),
            (int)(orientJpeg ? decoder.OrientedPixelHeight : decoder.PixelHeight));
    }
}

// SwapChainPanel's native interface, from microsoft.ui.xaml.media.dxinterop.h.
[GeneratedComInterface]
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
internal partial interface ISwapChainPanelNative
{
    void SetSwapChain(nint swapChain);
}
#endif
