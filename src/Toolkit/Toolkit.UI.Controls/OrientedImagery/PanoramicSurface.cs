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

#if WPF || WINDOWS_XAML || (MAUI && WINDOWS)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramaCameraState;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

// Direct3D 11 renderer for the panoramic viewport. It draws into a render target from the platform present layer: a
// swap-chain back buffer on WinUI, or an offscreen texture on WPF. CsWin32 overloads throw on failed HRESULTs.
internal sealed unsafe partial class PanoramicSurface
{
    private const string VertexShaderSource = """
        cbuffer SceneConstants : register(b0)
        {
            float4x4 WorldViewProjection;
        };
        struct VSInput { float3 Position : POSITION; float2 TexCoord : TEXCOORD0; };
        struct VSOutput { float4 Position : SV_POSITION; float2 TexCoord : TEXCOORD0; };
        VSOutput main(VSInput input)
        {
            VSOutput output;
            output.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);
            output.TexCoord = input.TexCoord;
            return output;
        }
        """;

    private const string PixelShaderSource = """
        Texture2D PanoramaTexture : register(t0);
        SamplerState PanoramaSampler : register(s0);
        struct PSInput { float4 Position : SV_POSITION; float2 TexCoord : TEXCOORD0; };
        float4 main(PSInput input) : SV_TARGET
        {
            return PanoramaTexture.Sample(PanoramaSampler, input.TexCoord);
        }
        """;

    // Markers are pre-projected to NDC on the CPU, so the vertex shader is a pass-through.
    private const string MarkerVertexShaderSource = """
        struct VSInput { float2 Position : POSITION; float2 TexCoord : TEXCOORD0; };
        struct VSOutput { float4 Position : SV_POSITION; float2 TexCoord : TEXCOORD0; };
        VSOutput main(VSInput input)
        {
            VSOutput output;
            output.Position = float4(input.Position, 0.0f, 1.0f);
            output.TexCoord = input.TexCoord;
            return output;
        }
        """;

    private ID3D11Device* _device;
    private ID3D11DeviceContext* _context;
    private ID3D11VertexShader* _vertexShader;
    private ID3D11PixelShader* _pixelShader;
    private ID3D11InputLayout* _inputLayout;
    private ID3D11Buffer* _vertexBuffer;
    private ID3D11Buffer* _indexBuffer;
    private ID3D11Buffer* _constantBuffer;
    private ID3D11Texture2D* _panoramaTexture;
    private ID3D11ShaderResourceView* _panoramaTextureView;
    private ID3D11SamplerState* _samplerState;
    private ID3D11RasterizerState* _rasterizerState;
    private uint _indexCount;
    private float _clearR = 0.02f, _clearG = 0.02f, _clearB = 0.02f, _clearA = 1f;

    // The file behind the texture. A device created or rebuilt without the texture decodes it again, so no CPU copy
    // of the pixels is kept.
    private string? _texturePath;
    private bool _usingWarp;

    private bool _needsRender = true;
    private bool _deviceLost;

    private static readonly TimeSpan RecoveryRetryInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan RecoveryReportDelay = TimeSpan.FromSeconds(10);
    private long _recoveryStarted;
    private long _lastRecoveryAttempt;
    private bool _recoveryReported;

    private bool _keyboardNavigating;
    private long _keyboardTimestamp;

    private ID3D11VertexShader* _markerVertexShader;
    private ID3D11InputLayout* _markerInputLayout;
    private ID3D11Buffer* _markerIndexBuffer;
    private ID3D11Buffer* _markerVertexBuffer;
    private uint _markerVertexCapacity;
    private ID3D11BlendState* _markerBlendState;
    private ID3D11SamplerState* _markerSampler;

    private readonly List<(nint Texture, nint View)> _swatchTextures = [];
    private readonly List<(nint View, PanoramaMarker Marker)> _gpuMarkers = [];
    private readonly List<nint> _visibleViews = [];
    private IReadOnlyList<PanoramaMarker> _markers = []; // uploaded whenever a device is created

    private PanoramaCameraState _camera = PanoramaCameraState.Initial;

    // A WARP texture cannot be shared with the hardware D3D9Ex device of the WPF present layer.
    internal bool IsUsingWarp => _usingWarp;

    internal PanoramaCameraState Camera
    {
        get => _camera;
        set
        {
            if (_camera == value)
                return;

            _camera = value;
            ViewChanged?.Invoke();
        }
    }

    // Raised on camera and size changes; the footprint's horizontal field of view follows the view's aspect ratio.
    internal event Action? ViewChanged;

    // Raised when the pixels per DIP change.
    internal event Action? ScaleChanged;

    // Carries the position in element DIPs.
    internal event Action<double, double>? SurfaceTapped;

    // Device, present, and render failures outside the load path, which the viewport reports as Error.
    internal event Action<Exception>? RenderFailed;

    internal bool IsDeviceInitialized => _device is not null;

    internal bool IsDeviceRemoved
    {
        get
        {
            if (_device is null)
                return false;

            try
            {
                // The CsWin32 overload throws the removal reason.
                _device->GetDeviceRemovedReason();
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    internal ID3D11Device* Device => _device;

    internal ID3D11DeviceContext* Context => _context;

    // Present-layer hooks. EnsureResources also creates the device.
    private partial void EnsureResources();

    private partial void ReleasePresentResources();

    private partial void HookRendering();

    private partial bool IsReadyToRender();

    private partial void RenderFrame();

    private bool TryStartKeyboardNavigation(VIRTUAL_KEY virtualKey)
    {
        NavigationKeys key = virtualKey switch
        {
            VIRTUAL_KEY.VK_LEFT => NavigationKeys.Left,
            VIRTUAL_KEY.VK_RIGHT => NavigationKeys.Right,
            VIRTUAL_KEY.VK_UP => NavigationKeys.Up,
            VIRTUAL_KEY.VK_DOWN => NavigationKeys.Down,
            VIRTUAL_KEY.VK_OEM_PLUS or VIRTUAL_KEY.VK_ADD => NavigationKeys.ZoomIn,
            VIRTUAL_KEY.VK_OEM_MINUS or VIRTUAL_KEY.VK_SUBTRACT => NavigationKeys.ZoomOut,
            _ => NavigationKeys.None,
        };

        if (key == NavigationKeys.None || !GetHeldNavigationKeys().HasFlag(key))
            return false;

        StartKeyboardNavigation();
        return true;
    }

    private static bool IsKeyDown(VIRTUAL_KEY key) => PInvoke.GetKeyState((int)key) < 0;

    private static NavigationKeys GetHeldNavigationKeys()
    {
        NavigationKeys keys = NavigationKeys.None;
        if (!IsKeyDown(VIRTUAL_KEY.VK_CONTROL) && !IsKeyDown(VIRTUAL_KEY.VK_MENU) && !IsKeyDown(VIRTUAL_KEY.VK_SHIFT))
        {
            if (IsKeyDown(VIRTUAL_KEY.VK_LEFT))
                keys |= NavigationKeys.Left;
            if (IsKeyDown(VIRTUAL_KEY.VK_RIGHT))
                keys |= NavigationKeys.Right;
            if (IsKeyDown(VIRTUAL_KEY.VK_UP))
                keys |= NavigationKeys.Up;
            if (IsKeyDown(VIRTUAL_KEY.VK_DOWN))
                keys |= NavigationKeys.Down;
        }

        if (IsKeyDown(VIRTUAL_KEY.VK_OEM_PLUS) || IsKeyDown(VIRTUAL_KEY.VK_ADD))
            keys |= NavigationKeys.ZoomIn;
        if (IsKeyDown(VIRTUAL_KEY.VK_OEM_MINUS) || IsKeyDown(VIRTUAL_KEY.VK_SUBTRACT))
            keys |= NavigationKeys.ZoomOut;

        return keys;
    }

    internal void RequestRender() => _needsRender = true;

    // Held keys are polled every tick, so they combine and move the view smoothly rather than at the key repeat rate.
    private void StartKeyboardNavigation()
    {
        if (_keyboardNavigating)
            return;

        _keyboardNavigating = true;
        _keyboardTimestamp = Stopwatch.GetTimestamp();
    }

    // After focus leaves, key releases may never arrive, so the polled key state can go stale.
    private void StopKeyboardNavigation() => _keyboardNavigating = false;

    private void StepKeyboardNavigation()
    {
        if (!_keyboardNavigating)
            return;

        NavigationKeys keys = GetHeldNavigationKeys();
        if (keys == NavigationKeys.None)
        {
            _keyboardNavigating = false;
            return;
        }

        long now = Stopwatch.GetTimestamp();
        double seconds = Math.Min(Stopwatch.GetElapsedTime(_keyboardTimestamp, now).TotalSeconds, PanoramaCameraState.MaxKeyboardStepSeconds);
        _keyboardTimestamp = now;

        Camera = Camera.Navigate(keys, seconds, ActualHeight);
        RequestRender();
    }

    // A removed device (TDR, driver update, RDP, or sleep) is rebuilt on later ticks, not reported as an error.
    private void NotifyDeviceLost()
    {
        if (_deviceLost)
            return;

        _deviceLost = true;
        _needsRender = true;
        _recoveryStarted = Stopwatch.GetTimestamp();
        _lastRecoveryAttempt = 0; // the first attempt is immediate
        _recoveryReported = false;
    }

    // Guards XAML lifecycle handlers, which run outside the render tick's error handling.
    private void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (IsDeviceRemoved)
                NotifyDeviceLost();
            else
                RenderFailed?.Invoke(ex);
        }
    }

    // Hook the tick first, so a device removed during creation still recovers.
    private void HandleLoaded()
    {
        HookRendering();
        EnsureResources();
        RequestRender();
    }

    private void RenderTick()
    {
        StepKeyboardNavigation();
        if (_deviceLost && !TryRecoverDevice())
            return;

        if (!_needsRender || !IsReadyToRender())
            return;

        Exception? failure = null;
        try
        {
            RenderFrame();
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        if (IsDeviceRemoved)
        {
            NotifyDeviceLost();
            return;
        }

        _needsRender = false; // so a persistent failure is reported once, not every frame
        if (failure is not null)
            RenderFailed?.Invoke(failure);
    }

    // Retries while the GPU is unavailable. After a reported failure, setting the footprint again retries the image.
    private bool TryRecoverDevice()
    {
        long now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(_lastRecoveryAttempt, now) < RecoveryRetryInterval)
            return false;

        _lastRecoveryAttempt = now;
        Exception? failure = null;
        try
        {
            ReleasePresentResources();
            ReleaseDeviceResources();
            EnsureResources();
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        if (failure is null && IsDeviceInitialized)
        {
            _deviceLost = false;
            _needsRender = true;
            return true;
        }

        // EnsureResources declines without throwing while the surface has no size, which is not a failure.
        if (failure is not null && !_recoveryReported && Stopwatch.GetElapsedTime(_recoveryStarted, now) >= RecoveryReportDelay)
        {
            _recoveryReported = true;
            RenderFailed?.Invoke(failure);
        }

        return false;
    }

    internal void Initialize()
    {
        if (_device is not null)
            return;

        CreateDeviceResources();
        CreatePipeline(VertexShaderSource, DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT, 12, out _vertexShader, out _inputLayout);
        ID3DBlob* pixelBlob = CompileShader(PixelShaderSource, "ps_4_0");
        try
        {
            ID3D11PixelShader* pixelShader;
            _device->CreatePixelShader(pixelBlob->GetBufferPointer(), pixelBlob->GetBufferSize(), null, &pixelShader);
            _pixelShader = pixelShader;
        }
        finally
        {
            Release(ref pixelBlob);
        }

        CreateGeometryResources();
        _constantBuffer = CreateBuffer((uint)sizeof(Matrix4x4), D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER, null);
        CreateRasterizerState();
        _samplerState = CreateSampler(D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP); // wrap u across the seam
        CreateMarkerResources();
        BuildGpuMarkers(_markers);
        if (_texturePath is string path)
            _ = RestoreTextureAsync(path);
    }

    // Takes the decoded equirectangular image and the file it came from. Without a device yet, Initialize decodes the
    // file again.
    internal void SetTexture(PanoramaFrame frame, string path)
    {
        _texturePath = path;
        if (_device is not null)
            UploadTexture(frame);
    }

    private void UploadTexture(PanoramaFrame frame)
    {
        Release(ref _panoramaTextureView);
        Release(ref _panoramaTexture);
        CreateBgraTexture(frame.Bgra, (uint)frame.Width, (uint)frame.Height, out _panoramaTexture, out _panoramaTextureView);
    }

    private static D3D11_TEXTURE2D_DESC CreateBgraTextureDescription(uint width, uint height, D3D11_BIND_FLAG bindFlags,
        D3D11_USAGE usage = D3D11_USAGE.D3D11_USAGE_DEFAULT, D3D11_RESOURCE_MISC_FLAG miscFlags = default) => new()
    {
        Width = width,
        Height = height,
        MipLevels = 1,
        ArraySize = 1,
        Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
        SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
        Usage = usage,
        BindFlags = bindFlags,
        MiscFlags = miscFlags,
    };

    private void CreateBgraTexture(ReadOnlySpan<byte> bgra, uint width, uint height, out ID3D11Texture2D* texture, out ID3D11ShaderResourceView* view)
    {
        D3D11_TEXTURE2D_DESC desc = CreateBgraTextureDescription(width, height, D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE);

        fixed (byte* pixels = bgra)
        {
            D3D11_SUBRESOURCE_DATA initialData = new() { pSysMem = pixels, SysMemPitch = width * 4 };
            ID3D11Texture2D* tex;
            _device->CreateTexture2D(&desc, &initialData, &tex);
            texture = tex;
        }

        ID3D11ShaderResourceView* srv;
        try
        {
            _device->CreateShaderResourceView((ID3D11Resource*)texture, (D3D11_SHADER_RESOURCE_VIEW_DESC*)null, &srv);
        }
        catch
        {
            // The caller never receives the texture on failure, so release it here.
            Release(ref texture);
            throw;
        }

        view = srv;
    }

    internal void ClearTexture()
    {
        _texturePath = null;

        // The context references the bound view, so unbind it to free the texture while the backdrop is blank.
        if (_context is not null)
        {
            ID3D11ShaderResourceView* nullSrv = null;
            _context->PSSetShaderResources(0, 1, &nullSrv);
        }

        Release(ref _panoramaTextureView);
        Release(ref _panoramaTexture);
    }

    // Takes straight-alpha components in 0..1.
    internal void SetClearColor(float r, float g, float b, float a)
    {
#if WPF
        // WPF composites premultiplied BGRA. The WinUI swap chain ignores alpha, so it uses the color unchanged.
        r *= a;
        g *= a;
        b *= a;
#endif
        _clearR = r;
        _clearG = g;
        _clearB = b;
        _clearA = a;
    }

    // Size in device pixels. Without a texture it still clears, so the last frame does not stay on screen.
    internal void RenderScene(ID3D11RenderTargetView* target, uint width, uint height)
    {
        if (_context is null || target is null)
            return;

        float* clear = stackalloc float[4] { _clearR, _clearG, _clearB, _clearA };
        _context->ClearRenderTargetView(target, clear);
        if (_vertexShader is null || _pixelShader is null || _inputLayout is null || _vertexBuffer is null ||
            _indexBuffer is null || _constantBuffer is null || _panoramaTextureView is null || _samplerState is null)
            return;

        float aspectRatio = MathF.Max(1f, width) / MathF.Max(1f, height);
        PanoramaCameraState camera = Camera;
        Matrix4x4 worldViewProjection = camera.GetWorldViewProjection(aspectRatio);
        Matrix4x4 transposed = Matrix4x4.Transpose(worldViewProjection);
        _context->UpdateSubresource((ID3D11Resource*)_constantBuffer, 0, (D3D11_BOX*)null, &transposed, 0, 0);

        D3D11_VIEWPORT viewport = new() { TopLeftX = 0, TopLeftY = 0, Width = width, Height = height, MinDepth = 0f, MaxDepth = 1f };
        _context->RSSetViewports(1, &viewport);
        _context->RSSetState(_rasterizerState);
        _context->OMSetRenderTargets(1, &target, null);
        _context->OMSetBlendState((ID3D11BlendState*)null, (float*)null, 0xffffffff);

        uint stride = 5 * sizeof(float);
        uint offset = 0;
        ID3D11Buffer* vertexBuffer = _vertexBuffer;
        ID3D11Buffer* constantBuffer = _constantBuffer;
        ID3D11ShaderResourceView* srv = _panoramaTextureView;
        ID3D11SamplerState* sampler = _samplerState;

        _context->IASetInputLayout(_inputLayout);
        _context->IASetVertexBuffers(0, 1, &vertexBuffer, &stride, &offset);
        _context->IASetIndexBuffer(_indexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
        _context->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        _context->VSSetShader(_vertexShader, null, 0);
        _context->VSSetConstantBuffers(0, 1, &constantBuffer);
        _context->PSSetShader(_pixelShader, null, 0);
        _context->PSSetShaderResources(0, 1, &srv);
        _context->PSSetSamplers(0, 1, &sampler);
        _context->DrawIndexed(_indexCount, 0, 0);

        DrawMarkers(width, height, camera);
    }

    private void CreateDeviceResources()
    {
        ReadOnlySpan<D3D_FEATURE_LEVEL> featureLevels =
        [
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
        ];

        ID3D11Device* device;
        ID3D11DeviceContext* context;
        const D3D11_CREATE_DEVICE_FLAG flags = D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;

        // Reset per creation, so a rebuild after the GPU returns leaves WARP.
        _usingWarp = false;

        // Hardware first; WARP keeps the panorama rendering under Remote Desktop or without a compatible GPU.
        HRESULT hr = PInvoke.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, default, flags, featureLevels, PInvoke.D3D11_SDK_VERSION, &device, out _, &context);
        if (hr.Failed)
        {
            PInvoke.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP, default, flags, featureLevels, PInvoke.D3D11_SDK_VERSION, &device, out _, &context).ThrowOnFailure();
            _usingWarp = true;
        }

        _device = device;
        _context = context;
    }

    private void CreatePipeline(string vertexSource, DXGI_FORMAT positionFormat, uint texCoordOffset,
        out ID3D11VertexShader* vertexShader, out ID3D11InputLayout* inputLayout)
    {
        ID3DBlob* vertexBlob = CompileShader(vertexSource, "vs_4_0");
        try
        {
            ID3D11VertexShader* vs;
            _device->CreateVertexShader(vertexBlob->GetBufferPointer(), vertexBlob->GetBufferSize(), null, &vs);
            vertexShader = vs;

            byte[] position = Encoding.ASCII.GetBytes("POSITION\0");
            byte[] texCoord = Encoding.ASCII.GetBytes("TEXCOORD\0");
            fixed (byte* positionPtr = position)
            fixed (byte* texCoordPtr = texCoord)
            {
                D3D11_INPUT_ELEMENT_DESC* elements = stackalloc D3D11_INPUT_ELEMENT_DESC[2];
                elements[0] = new D3D11_INPUT_ELEMENT_DESC
                {
                    SemanticName = new PCSTR(positionPtr),
                    Format = positionFormat,
                    AlignedByteOffset = 0,
                    InputSlotClass = D3D11_INPUT_CLASSIFICATION.D3D11_INPUT_PER_VERTEX_DATA,
                };
                elements[1] = new D3D11_INPUT_ELEMENT_DESC
                {
                    SemanticName = new PCSTR(texCoordPtr),
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                    AlignedByteOffset = texCoordOffset,
                    InputSlotClass = D3D11_INPUT_CLASSIFICATION.D3D11_INPUT_PER_VERTEX_DATA,
                };

                ID3D11InputLayout* layout;
                _device->CreateInputLayout(elements, 2, vertexBlob->GetBufferPointer(), vertexBlob->GetBufferSize(), &layout);
                inputLayout = layout;
            }
        }
        finally
        {
            Release(ref vertexBlob);
        }
    }

    private ID3D11Buffer* CreateBuffer(uint byteWidth, D3D11_BIND_FLAG bindFlags, void* initialData,
        D3D11_USAGE usage = D3D11_USAGE.D3D11_USAGE_DEFAULT, D3D11_CPU_ACCESS_FLAG cpuAccess = default)
    {
        D3D11_BUFFER_DESC desc = new() { ByteWidth = byteWidth, Usage = usage, BindFlags = bindFlags, CPUAccessFlags = cpuAccess };
        D3D11_SUBRESOURCE_DATA data = new() { pSysMem = initialData };
        ID3D11Buffer* buffer;
        _device->CreateBuffer(&desc, initialData is null ? null : &data, &buffer);
        return buffer;
    }

    private ID3D11SamplerState* CreateSampler(D3D11_TEXTURE_ADDRESS_MODE addressU)
    {
        D3D11_SAMPLER_DESC desc = new()
        {
            Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
            AddressU = addressU,
            AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
            AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
            ComparisonFunc = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_NEVER,
            MinLOD = 0f,
            MaxLOD = float.MaxValue,
            MaxAnisotropy = 1,
        };
        ID3D11SamplerState* state;
        _device->CreateSamplerState(&desc, &state);
        return state;
    }

    private void CreateGeometryResources()
    {
        (float[] vertices, short[] indices) = PanoramaCameraState.CreateSphereMesh();
        _indexCount = (uint)indices.Length;
        fixed (float* vertexData = vertices)
            _vertexBuffer = CreateBuffer((uint)(vertices.Length * sizeof(float)), D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER, vertexData);
        fixed (short* indexData = indices)
            _indexBuffer = CreateBuffer((uint)(indices.Length * sizeof(short)), D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER, indexData);
    }

    private void CreateRasterizerState()
    {
        D3D11_RASTERIZER_DESC desc = new()
        {
            FillMode = D3D11_FILL_MODE.D3D11_FILL_SOLID,
            CullMode = D3D11_CULL_MODE.D3D11_CULL_NONE,
            FrontCounterClockwise = false,
            DepthClipEnable = true,
        };
        ID3D11RasterizerState* state;
        _device->CreateRasterizerState(&desc, &state);
        _rasterizerState = state;
    }

    internal void SetMarkers(IReadOnlyList<PanoramaMarker> markers)
    {
        _markers = markers;
        if (_device is not null)
            BuildGpuMarkers(markers);
    }

    private void CreateMarkerResources()
    {
        CreatePipeline(MarkerVertexShaderSource, DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT, 8, out _markerVertexShader, out _markerInputLayout);

        ushort[] indices = [0, 1, 2, 2, 1, 3];
        fixed (ushort* indexData = indices)
            _markerIndexBuffer = CreateBuffer((uint)(indices.Length * sizeof(ushort)), D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER, indexData);

        // Source-over for the premultiplied swatches from RuntimeImage.
        D3D11_BLEND_DESC blendDesc = default;
        D3D11_RENDER_TARGET_BLEND_DESC rtBlend = new()
        {
            BlendEnable = true,
            SrcBlend = D3D11_BLEND.D3D11_BLEND_ONE,
            DestBlend = D3D11_BLEND.D3D11_BLEND_INV_SRC_ALPHA,
            BlendOp = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
            SrcBlendAlpha = D3D11_BLEND.D3D11_BLEND_ONE,
            DestBlendAlpha = D3D11_BLEND.D3D11_BLEND_INV_SRC_ALPHA,
            BlendOpAlpha = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
            RenderTargetWriteMask = (byte)D3D11_COLOR_WRITE_ENABLE.D3D11_COLOR_WRITE_ENABLE_ALL,
        };
        blendDesc.RenderTarget._0 = rtBlend;
        ID3D11BlendState* blendState;
        _device->CreateBlendState(&blendDesc, &blendState);
        _markerBlendState = blendState;
        _markerSampler = CreateSampler(D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP);
    }

    // Markers sharing a swatch share its texture: the viewport gives them the same pixel array.
    private void BuildGpuMarkers(IReadOnlyList<PanoramaMarker> markers)
    {
        ReleaseGpuMarkers();
        var views = new Dictionary<byte[], nint>(ReferenceEqualityComparer.Instance);
        foreach (PanoramaMarker marker in markers)
        {
            // A marker that cannot be uploaded is skipped, so it never fails the panorama.
            if (!marker.IsValid)
                continue;

            if (!views.TryGetValue(marker.Bgra, out nint view))
            {
                try
                {
                    CreateBgraTexture(marker.Bgra, (uint)marker.Width, (uint)marker.Height,
                        out ID3D11Texture2D* texture, out ID3D11ShaderResourceView* srv);
                    view = (nint)srv;
                    _swatchTextures.Add(((nint)texture, view));
                }
                catch (Exception)
                {
                    continue;
                }

                views.Add(marker.Bgra, view);
            }

            _gpuMarkers.Add((view, marker));
        }

        if (_gpuMarkers.Count == 0)
            return;

        uint requiredVertices = (uint)_gpuMarkers.Count * 4;
        if (_markerVertexBuffer is null || _markerVertexCapacity < requiredVertices)
        {
            Release(ref _markerVertexBuffer);
            _markerVertexBuffer = CreateBuffer(requiredVertices * (uint)sizeof(MarkerVertex), D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER, null,
                D3D11_USAGE.D3D11_USAGE_DYNAMIC, D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
            _markerVertexCapacity = requiredVertices;
        }
    }

    private void DrawMarkers(uint width, uint height, PanoramaCameraState camera)
    {
        if (_gpuMarkers.Count == 0 || _markerVertexBuffer is null || _markerVertexShader is null || _pixelShader is null ||
            _markerInputLayout is null || _markerIndexBuffer is null || _markerBlendState is null || _markerSampler is null)
            return;

        D3D11_MAPPED_SUBRESOURCE mapped;
        _context->Map((ID3D11Resource*)_markerVertexBuffer, 0, D3D11_MAP.D3D11_MAP_WRITE_DISCARD, 0, &mapped);
        var vertices = (MarkerVertex*)mapped.pData;
        _visibleViews.Clear();
        foreach ((nint view, PanoramaMarker marker) in _gpuMarkers)
        {
            if (!camera.TryGetMarkerQuad(marker, width, height, out var quad))
                continue;

            int b = _visibleViews.Count * 4;
            vertices[b + 0] = new MarkerVertex(new Vector2(quad.Left, quad.Top), new Vector2(0f, 0f));
            vertices[b + 1] = new MarkerVertex(new Vector2(quad.Right, quad.Top), new Vector2(1f, 0f));
            vertices[b + 2] = new MarkerVertex(new Vector2(quad.Left, quad.Bottom), new Vector2(0f, 1f));
            vertices[b + 3] = new MarkerVertex(new Vector2(quad.Right, quad.Bottom), new Vector2(1f, 1f));
            _visibleViews.Add(view);
        }

        _context->Unmap((ID3D11Resource*)_markerVertexBuffer, 0);
        if (_visibleViews.Count == 0)
            return;

        uint stride = (uint)sizeof(MarkerVertex);
        uint offset = 0;
        ID3D11Buffer* vertexBuffer = _markerVertexBuffer;
        ID3D11SamplerState* sampler = _markerSampler;
        float* blendFactor = stackalloc float[4] { 1f, 1f, 1f, 1f };

        _context->IASetInputLayout(_markerInputLayout);
        _context->IASetVertexBuffers(0, 1, &vertexBuffer, &stride, &offset);
        _context->IASetIndexBuffer(_markerIndexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
        _context->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        _context->VSSetShader(_markerVertexShader, null, 0);
        _context->PSSetShader(_pixelShader, null, 0);
        _context->PSSetSamplers(0, 1, &sampler);
        _context->OMSetBlendState(_markerBlendState, blendFactor, 0xffffffff);

        for (int i = 0; i < _visibleViews.Count; i++)
        {
            ID3D11ShaderResourceView* srv = (ID3D11ShaderResourceView*)_visibleViews[i];
            _context->PSSetShaderResources(0, 1, &srv);
            _context->DrawIndexed(6, 0, i * 4);
        }

        _context->OMSetBlendState((ID3D11BlendState*)null, (float*)null, 0xffffffff);
    }

    private void ReleaseGpuMarkers()
    {
        foreach ((nint texture, nint view) in _swatchTextures)
        {
            _ = ((IUnknown*)view)->Release();
            _ = ((IUnknown*)texture)->Release();
        }

        _swatchTextures.Clear();
        _gpuMarkers.Clear();
        _visibleViews.Clear();
    }

    private void ReleaseMarkerResources()
    {
        ReleaseGpuMarkers();
        Release(ref _markerVertexBuffer);
        Release(ref _markerIndexBuffer);
        Release(ref _markerInputLayout);
        Release(ref _markerVertexShader);
        Release(ref _markerBlendState);
        Release(ref _markerSampler);
        _markerVertexCapacity = 0;
    }

    private ID3DBlob* CompileShader(string source, string target)
    {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
        byte[] entryPointBytes = Encoding.ASCII.GetBytes("main\0");
        byte[] targetBytes = Encoding.ASCII.GetBytes(target + "\0");

        fixed (byte* sourcePtr = sourceBytes)
        fixed (byte* entryPointPtr = entryPointBytes)
        fixed (byte* targetPtr = targetBytes)
        {
            ID3DBlob* code;
            ID3DBlob* errors;
            HRESULT hr = PInvoke.D3DCompile(
                sourcePtr,
                (nuint)sourceBytes.Length,
                default,
                null,
                null,
                new PCSTR(entryPointPtr),
                new PCSTR(targetPtr),
                0,
                0,
                &code,
                &errors);

            if (hr.Failed)
            {
                string message = "Shader compilation failed.";
                if (errors is not null)
                {
                    message = new string((sbyte*)errors->GetBufferPointer(), 0, (int)errors->GetBufferSize());
                    errors->Release();
                }

                throw new InvalidOperationException(message);
            }

            if (errors is not null)
                errors->Release();

            return code;
        }
    }

    private static void Release<T>(ref T* p)
        where T : unmanaged
    {
        if (p is not null)
        {
            _ = ((IUnknown*)p)->Release();
            p = null;
        }
    }

    internal void ReleaseDeviceResources()
    {
        ReleaseMarkerResources();
        Release(ref _rasterizerState);
        Release(ref _samplerState);
        Release(ref _panoramaTextureView);
        Release(ref _panoramaTexture);
        Release(ref _constantBuffer);
        Release(ref _indexBuffer);
        Release(ref _vertexBuffer);
        Release(ref _inputLayout);
        Release(ref _pixelShader);
        Release(ref _vertexShader);
        Release(ref _context);
        Release(ref _device);
    }

    private readonly record struct MarkerVertex(Vector2 Position, Vector2 TexCoord);
}

// An async method can't be declared in the unsafe part of the class.
internal sealed partial class PanoramicSurface
{
    // Decodes the file behind the texture again for a device that was created or rebuilt without it.
    private async Task RestoreTextureAsync(string path)
    {
        try
        {
            PanoramaFrame frame = await PanoramaFrame.DecodeAsync(path, CancellationToken.None);
            if (IsDeviceInitialized && path == _texturePath)
            {
                UploadTexture(frame);
                RequestRender();
            }
        }
        catch (Exception ex)
        {
            if (path == _texturePath)
                RenderFailed?.Invoke(ex);
        }
    }
}

// Tightly packed BGRA8. Each present layer adds its DecodeAsync.
internal readonly partial record struct PanoramaFrame(byte[] Bgra, int Width, int Height) : IDisposable
{
    // Managed memory only, so there is nothing to release early.
    public void Dispose()
    {
    }
}
#endif
