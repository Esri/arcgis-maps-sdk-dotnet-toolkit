using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Direct3D;
using WinRT;
using Windows.Win32.Graphics.Dxgi.Common;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Image360 : Control
    {
        private static readonly Guid IidDxgiFactory2 = new("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
        private static readonly string VertexShaderSource = """
cbuffer SceneConstants : register(b0)
{
    float4x4 WorldViewProjection;
};

struct VSInput
{
    float3 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);
    output.TexCoord = input.TexCoord;
    return output;
}
""";
        private static readonly string PixelShaderSource = """
Texture2D PanoramaTexture : register(t0);
SamplerState PanoramaSampler : register(s0);

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 main(PSInput input) : SV_TARGET
{
    return PanoramaTexture.Sample(PanoramaSampler, input.TexCoord);
}
""";

        private IDXGISwapChain1? m_swapchain;
        private ID3D11Device? m_d3dDevice;
        private ID3D11DeviceContext? m_d3dContext;
        private IDXGIDevice? m_dxgiDevice;
        private ID3D11RenderTargetView? m_renderTargetView;
        private ID3D11Texture2D? m_depthStencilTexture;
        private ID3D11DepthStencilView? m_depthStencilView;
        private ID3D11VertexShader? m_vertexShader;
        private ID3D11PixelShader? m_pixelShader;
        private ID3D11InputLayout? m_inputLayout;
        private ID3D11Buffer? m_vertexBuffer;
        private ID3D11Buffer? m_indexBuffer;
        private ID3D11Buffer? m_constantBuffer;
        private ID3D11Texture2D? m_panoramaTexture;
        private ID3D11ShaderResourceView? m_panoramaTextureView;
        private ID3D11SamplerState? m_samplerState;
        private ID3D11RasterizerState? m_rasterizerState;
        private D3D11_VIEWPORT m_viewport;

        private async Task InitializeAsyncCore()
        {
            CreateDeviceResources();
            CreateSwapChain();
            CreateShaderResources();
            CreateRasterizerState();
            CreateGeometryResources();
            CreateConstantBuffer();
            await CreateTextureAsync();
            CreateSizeDependentResources();
        }

        private unsafe void CreateDeviceResources()
        {
            if (m_d3dDevice is not null && m_d3dContext is not null)
            {
                return;
            }
            var featureLevels = new ReadOnlySpan<D3D_FEATURE_LEVEL>(new D3D_FEATURE_LEVEL[] {
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1,
                });
            var flags = D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;
            D3D_FEATURE_LEVEL featureLevel;
            var hresult = Windows.Win32.PInvoke.D3D11CreateDevice(
                null,
                D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                default,
                flags,
                featureLevels,
                7,
                out m_d3dDevice,
                out featureLevel,
                out m_d3dContext);


            ThrowIfFailed(hresult, "Failed to create the D3D11 device.");
        }

        private unsafe void CreateSwapChain()
        {
            if (m_swapchain is not null || swapchainPanel is null)
            {
                return;
            }

            DXGI_SWAP_CHAIN_DESC1 swapChainDesc = new()
            {
                Width = (uint)Math.Max(1, Math.Ceiling(swapchainPanel.ActualWidth)),
                Height = (uint)Math.Max(1, Math.Ceiling(swapchainPanel.ActualHeight)),
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                Stereo = new Windows.Win32.Foundation.BOOL(false),
                SampleDesc = new DXGI_SAMPLE_DESC
                {
                    Count = 1,
                    Quality = 0,
                },
                BufferUsage = DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT,
                BufferCount = 2,
                Scaling = 0,
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE,
                Flags = 0,
            };

            ReleaseComObject(ref m_dxgiDevice!);
            m_dxgiDevice = m_d3dDevice!.As<IDXGIDevice>();
            m_dxgiDevice.GetAdapter(out IDXGIAdapter dxgiAdapter);

            Guid factoryGuid = IidDxgiFactory2;
            dxgiAdapter.GetParent(&factoryGuid, out object? factoryObject);
            IDXGIFactory2 dxgiFactory = (IDXGIFactory2)factoryObject!;

            dxgiFactory.CreateSwapChainForComposition(m_d3dDevice!, &swapChainDesc, null, out IDXGISwapChain1 swapchain);
            m_swapchain = swapchain;

            ISwapChainPanelNative panelNative = swapchainPanel.As<ISwapChainPanelNative>();
            panelNative.SetSwapChain(swapchain);

            ReleaseComObject(ref dxgiFactory!);
            ReleaseComObject(ref dxgiAdapter!);
        }

        private unsafe void CreateSizeDependentResources()
        {
            if (m_swapchain is null || m_d3dDevice is null || swapchainPanel is null)
            {
                return;
            }

            uint width = (uint)Math.Max(1, Math.Ceiling(swapchainPanel.ActualWidth));
            uint height = (uint)Math.Max(1, Math.Ceiling(swapchainPanel.ActualHeight));

            ReleaseComObject(ref m_depthStencilView);
            ReleaseComObject(ref m_depthStencilTexture);
            ReleaseComObject(ref m_renderTargetView);

            m_swapchain.ResizeBuffers(2, width, height, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, 0);

            Guid backBufferGuid = typeof(ID3D11Texture2D).GUID;
            m_swapchain.GetBuffer(0, &backBufferGuid, out object? backBufferObject);
            ID3D11Texture2D backBuffer = (ID3D11Texture2D)backBufferObject!;
            m_renderTargetView = CreateRenderTargetViewNative(m_d3dDevice, backBuffer);
            ReleaseComObject(ref backBuffer!);

            D3D11_TEXTURE2D_DESC depthStencilDesc = new()
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT,
                SampleDesc = new DXGI_SAMPLE_DESC
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_DEPTH_STENCIL,
                CPUAccessFlags = 0,
                MiscFlags = 0,
            };

            m_depthStencilTexture = CreateTexture2DNative(m_d3dDevice, &depthStencilDesc, null, "Failed to create the depth stencil texture.");
            m_depthStencilView = CreateDepthStencilViewNative(m_d3dDevice, m_depthStencilTexture);

            m_viewport = new D3D11_VIEWPORT
            {
                TopLeftX = 0,
                TopLeftY = 0,
                Width = width,
                Height = height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
            };
        }

        private unsafe void CreateShaderResources()
        {
            if (m_vertexShader is not null)
            {
                return;
            }

            ID3DBlob vertexShaderBlob = CompileShader(VertexShaderSource, "main", "vs_4_0");
            ID3DBlob pixelShaderBlob = CompileShader(PixelShaderSource, "main", "ps_4_0");

            try
            {
                m_vertexShader = CreateVertexShaderNative(m_d3dDevice!, vertexShaderBlob.GetBufferPointer(), vertexShaderBlob.GetBufferSize());
                m_pixelShader = CreatePixelShaderNative(m_d3dDevice!, pixelShaderBlob.GetBufferPointer(), pixelShaderBlob.GetBufferSize());

                byte[] positionSemantic = Encoding.ASCII.GetBytes("POSITION\0");
                byte[] texCoordSemantic = Encoding.ASCII.GetBytes("TEXCOORD\0");

                fixed (byte* positionSemanticPtr = positionSemantic)
                fixed (byte* texCoordSemanticPtr = texCoordSemantic)
                {
                    D3D11_INPUT_ELEMENT_DESC[] inputElements =
                    [
                        new D3D11_INPUT_ELEMENT_DESC
                        {
                            SemanticName = (Windows.Win32.Foundation.PCSTR)positionSemanticPtr,
                            SemanticIndex = 0,
                            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT,
                            InputSlot = 0,
                            AlignedByteOffset = 0,
                            InputSlotClass = D3D11_INPUT_CLASSIFICATION.D3D11_INPUT_PER_VERTEX_DATA,
                            InstanceDataStepRate = 0,
                        },
                        new D3D11_INPUT_ELEMENT_DESC
                        {
                            SemanticName = (Windows.Win32.Foundation.PCSTR)texCoordSemanticPtr,
                            SemanticIndex = 0,
                            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                            InputSlot = 0,
                            AlignedByteOffset = 12,
                            InputSlotClass = D3D11_INPUT_CLASSIFICATION.D3D11_INPUT_PER_VERTEX_DATA,
                            InstanceDataStepRate = 0,
                        },
                    ];

                    fixed (D3D11_INPUT_ELEMENT_DESC* inputElementsPtr = inputElements)
                    {
                        m_inputLayout = CreateInputLayoutNative(m_d3dDevice!, inputElementsPtr, (uint)inputElements.Length, vertexShaderBlob.GetBufferPointer(), vertexShaderBlob.GetBufferSize());
                    }
                }
            }
            finally
            {
                ReleaseComObject(ref pixelShaderBlob!);
                ReleaseComObject(ref vertexShaderBlob!);
            }
        }

        private unsafe void CreateGeometryResources()
        {
            if (m_vertexBuffer is not null && m_indexBuffer is not null)
            {
                return;
            }

            (VertexPositionTexture[] vertices, ushort[] indices) = CreatePanoramaSphere();
            m_indexCount = (uint)indices.Length;

            D3D11_BUFFER_DESC vertexBufferDesc = new()
            {
                ByteWidth = (uint)(vertices.Length * Marshal.SizeOf<VertexPositionTexture>()),
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER,
                CPUAccessFlags = 0,
                MiscFlags = 0,
                StructureByteStride = 0,
            };

            D3D11_BUFFER_DESC indexBufferDesc = new()
            {
                ByteWidth = (uint)(indices.Length * sizeof(ushort)),
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER,
                CPUAccessFlags = 0,
                MiscFlags = 0,
                StructureByteStride = 0,
            };

            m_vertexBuffer = CreateBufferNative(m_d3dDevice!, &vertexBufferDesc, null, "Failed to create the vertex buffer.");
            m_indexBuffer = CreateBufferNative(m_d3dDevice!, &indexBufferDesc, null, "Failed to create the index buffer.");

            fixed (VertexPositionTexture* vertexData = vertices)
            {
                m_d3dContext!.UpdateSubresource(m_vertexBuffer!, 0, null, vertexData, 0, 0);
            }

            fixed (ushort* indexData = indices)
            {
                m_d3dContext!.UpdateSubresource(m_indexBuffer!, 0, null, indexData, 0, 0);
            }
        }

        private unsafe void CreateRasterizerState()
        {
            if (m_rasterizerState is not null)
            {
                return;
            }

            D3D11_RASTERIZER_DESC rasterizerDesc = new()
            {
                FillMode = D3D11_FILL_MODE.D3D11_FILL_SOLID,
                CullMode = D3D11_CULL_MODE.D3D11_CULL_NONE,
                FrontCounterClockwise = false,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                SlopeScaledDepthBias = 0.0f,
                DepthClipEnable = true,
                ScissorEnable = false,
                MultisampleEnable = false,
                AntialiasedLineEnable = false,
            };

            m_rasterizerState = CreateRasterizerStateNative(m_d3dDevice!, &rasterizerDesc);
        }

        private unsafe void CreateConstantBuffer()
        {
            if (m_constantBuffer is not null)
            {
                return;
            }

            D3D11_BUFFER_DESC constantBufferDesc = new()
            {
                ByteWidth = (uint)Marshal.SizeOf<SceneConstants>(),
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER,
                CPUAccessFlags = 0,
                MiscFlags = 0,
                StructureByteStride = 0,
            };

            m_constantBuffer = CreateBufferNative(m_d3dDevice!, &constantBufferDesc, null, "Failed to create the constant buffer.");
        }

        private async Task CreateTextureAsync(bool requestRender = false)
        {
            if (Source is null)
                return;
            if (m_panoramaTextureView is not null)
            {
                ReleaseComObject(ref m_panoramaTexture);
            }

            ImageData? imageData = await LoadTextureDataAsync();
            CreateTextureResources(imageData);
            if (requestRender)
                Render();
        }

        private unsafe void CreateTextureResources(ImageData? imageData)
        {
            if (imageData is null)
                return;
            D3D11_TEXTURE2D_DESC textureDesc = new()
            {
                Width = imageData.Value.Width,
                Height = imageData.Value.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new DXGI_SAMPLE_DESC
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
                CPUAccessFlags = 0,
                MiscFlags = 0,
            };

            m_panoramaTexture = CreateTexture2DNative(m_d3dDevice!, &textureDesc, null, "Failed to create the panorama texture.");

            fixed (byte* pixelData = imageData.Value.Pixels)
            {
                m_d3dContext!.UpdateSubresource(m_panoramaTexture!, 0, null, pixelData, imageData.Value.RowPitch, 0);
            }

            m_panoramaTextureView = CreateShaderResourceViewNative(m_d3dDevice!, m_panoramaTexture);

            D3D11_SAMPLER_DESC samplerDesc = new()
            {
                Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
                AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
                AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
                AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
                MipLODBias = 0.0f,
                MaxAnisotropy = 1,
                ComparisonFunc = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_NEVER,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue,
            };

            m_samplerState = CreateSamplerStateNative(m_d3dDevice!, &samplerDesc);
        }

        private unsafe void Render()
        {
            if (m_swapchain is null ||
                m_d3dContext is null ||
                m_renderTargetView is null ||
                m_depthStencilView is null ||
                m_vertexShader is null ||
                m_pixelShader is null ||
                m_inputLayout is null ||
                m_vertexBuffer is null ||
                m_indexBuffer is null ||
                m_constantBuffer is null ||
                m_panoramaTextureView is null ||
                m_samplerState is null)
            {
                return;
            }

            UpdateSceneConstants();

            uint stride = (uint)Marshal.SizeOf<VertexPositionTexture>();
            uint offset = 0;
            float[] clearColor = [0.02f, 0.02f, 0.02f, 1.0f];

            m_d3dContext.ClearRenderTargetView(m_renderTargetView, clearColor);

            ID3D11RenderTargetView[] renderTargets = [m_renderTargetView];
            ID3D11Buffer[] vertexBuffers = [m_vertexBuffer];
            ID3D11Buffer[] constantBuffers = [m_constantBuffer];
            ID3D11ShaderResourceView[] shaderResources = [m_panoramaTextureView];
            ID3D11SamplerState[] samplers = [m_samplerState];
            OMSetRenderTargetsNative(m_d3dContext, renderTargets);
            fixed (D3D11_VIEWPORT* viewportPtr = &m_viewport)
            {
                m_d3dContext.RSSetViewports(1, viewportPtr);
            }
            m_d3dContext.RSSetState(m_rasterizerState);
            m_d3dContext.IASetInputLayout(m_inputLayout);
            IASetVertexBuffersNative(m_d3dContext, vertexBuffers, stride, offset);
            m_d3dContext.IASetIndexBuffer(m_indexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
            m_d3dContext.IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
            VSSetShaderNative(m_d3dContext, m_vertexShader);
            VSSetConstantBuffersNative(m_d3dContext, constantBuffers);
            PSSetShaderNative(m_d3dContext, m_pixelShader);
            PSSetShaderResourcesNative(m_d3dContext, shaderResources);
            PSSetSamplersNative(m_d3dContext, samplers);
            m_d3dContext.DrawIndexed(m_indexCount, 0, 0);
            m_swapchain.Present(1, 0);

            m_needsRender = false;
        }

        private unsafe void UpdateSceneConstants()
        {
            if (swapchainPanel is null)
                return;
            float aspectRatio = Math.Max(1.0f, (float)swapchainPanel.ActualWidth) / Math.Max(1.0f, (float)swapchainPanel.ActualHeight);
            Matrix4x4 world = Matrix4x4.CreateRotationY(m_yaw) * Matrix4x4.CreateRotationX(m_pitch);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(m_fieldOfView, aspectRatio, 0.1f, 10.0f);

            SceneConstants constants = new()
            {
                WorldViewProjection = Matrix4x4.Transpose(world * projection),
            };

            m_d3dContext!.UpdateSubresource(m_constantBuffer!, 0, null, &constants, 0, 0);
        }

        private static unsafe void OMSetRenderTargetsNative(ID3D11DeviceContext deviceContext, ID3D11RenderTargetView[] renderTargets)
        {
            IntPtr deviceContextPtr = GetComInterfacePointer(deviceContext, typeof(ID3D11DeviceContext).GUID);
            IntPtr* renderTargetPtrs = stackalloc IntPtr[renderTargets.Length];

            try
            {
                for (int i = 0; i < renderTargets.Length; i++)
                {
                    renderTargetPtrs[i] = GetComInterfacePointer(renderTargets[i], typeof(ID3D11RenderTargetView).GUID);
                }

                IntPtr* vtable = *(IntPtr**)deviceContextPtr;
                var omSetRenderTargets = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, IntPtr, void>)vtable[33];
                omSetRenderTargets(deviceContextPtr, (uint)renderTargets.Length, renderTargetPtrs, IntPtr.Zero);
            }
            finally
            {
                for (int i = 0; i < renderTargets.Length; i++)
                {
                    if (renderTargetPtrs[i] != IntPtr.Zero)
                    {
                        Marshal.Release(renderTargetPtrs[i]);
                    }
                }

                Marshal.Release(deviceContextPtr);
            }
        }

        private static unsafe void IASetVertexBuffersNative(ID3D11DeviceContext deviceContext, ID3D11Buffer[] vertexBuffers, uint stride, uint offset)
        {
            IntPtr deviceContextPtr = GetComInterfacePointer(deviceContext, typeof(ID3D11DeviceContext).GUID);
            IntPtr* bufferPtrs = stackalloc IntPtr[vertexBuffers.Length];

            try
            {
                for (int i = 0; i < vertexBuffers.Length; i++)
                {
                    bufferPtrs[i] = GetComInterfacePointer(vertexBuffers[i], typeof(ID3D11Buffer).GUID);
                }

                IntPtr* vtable = *(IntPtr**)deviceContextPtr;
                var iaSetVertexBuffers = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, IntPtr*, uint*, uint*, void>)vtable[18];
                iaSetVertexBuffers(deviceContextPtr, 0, (uint)vertexBuffers.Length, bufferPtrs, &stride, &offset);
            }
            finally
            {
                for (int i = 0; i < vertexBuffers.Length; i++)
                {
                    if (bufferPtrs[i] != IntPtr.Zero)
                    {
                        Marshal.Release(bufferPtrs[i]);
                    }
                }

                Marshal.Release(deviceContextPtr);
            }
        }

        private static unsafe void VSSetShaderNative(ID3D11DeviceContext deviceContext, ID3D11VertexShader shader)
        {
            IntPtr deviceContextPtr = GetComInterfacePointer(deviceContext, typeof(ID3D11DeviceContext).GUID);
            IntPtr shaderPtr = IntPtr.Zero;

            try
            {
                shaderPtr = GetComInterfacePointer(shader, typeof(ID3D11VertexShader).GUID);
                IntPtr* vtable = *(IntPtr**)deviceContextPtr;
                var vsSetShader = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, uint, void>)vtable[11];
                vsSetShader(deviceContextPtr, shaderPtr, null, 0);
            }
            finally
            {
                if (shaderPtr != IntPtr.Zero)
                {
                    Marshal.Release(shaderPtr);
                }

                Marshal.Release(deviceContextPtr);
            }
        }

        private static unsafe void VSSetConstantBuffersNative(ID3D11DeviceContext deviceContext, ID3D11Buffer[] constantBuffers)
        {
            IntPtr deviceContextPtr = GetComInterfacePointer(deviceContext, typeof(ID3D11DeviceContext).GUID);
            IntPtr* bufferPtrs = stackalloc IntPtr[constantBuffers.Length];

            try
            {
                for (int i = 0; i < constantBuffers.Length; i++)
                {
                    bufferPtrs[i] = GetComInterfacePointer(constantBuffers[i], typeof(ID3D11Buffer).GUID);
                }

                IntPtr* vtable = *(IntPtr**)deviceContextPtr;
                var vsSetConstantBuffers = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, IntPtr*, void>)vtable[7];
                vsSetConstantBuffers(deviceContextPtr, 0, (uint)constantBuffers.Length, bufferPtrs);
            }
            finally
            {
                for (int i = 0; i < constantBuffers.Length; i++)
                {
                    if (bufferPtrs[i] != IntPtr.Zero)
                    {
                        Marshal.Release(bufferPtrs[i]);
                    }
                }

                Marshal.Release(deviceContextPtr);
            }
        }

        private static unsafe void PSSetShaderNative(ID3D11DeviceContext deviceContext, ID3D11PixelShader shader)
        {
            IntPtr deviceContextPtr = GetComInterfacePointer(deviceContext, typeof(ID3D11DeviceContext).GUID);
            IntPtr shaderPtr = IntPtr.Zero;

            try
            {
                shaderPtr = GetComInterfacePointer(shader, typeof(ID3D11PixelShader).GUID);
                IntPtr* vtable = *(IntPtr**)deviceContextPtr;
                var psSetShader = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, uint, void>)vtable[9];
                psSetShader(deviceContextPtr, shaderPtr, null, 0);
            }
            finally
            {
                if (shaderPtr != IntPtr.Zero)
                {
                    Marshal.Release(shaderPtr);
                }

                Marshal.Release(deviceContextPtr);
            }
        }

        private static unsafe void PSSetShaderResourcesNative(ID3D11DeviceContext deviceContext, ID3D11ShaderResourceView[] shaderResources)
        {
            IntPtr deviceContextPtr = GetComInterfacePointer(deviceContext, typeof(ID3D11DeviceContext).GUID);
            IntPtr* resourcePtrs = stackalloc IntPtr[shaderResources.Length];

            try
            {
                for (int i = 0; i < shaderResources.Length; i++)
                {
                    resourcePtrs[i] = GetComInterfacePointer(shaderResources[i], typeof(ID3D11ShaderResourceView).GUID);
                }

                IntPtr* vtable = *(IntPtr**)deviceContextPtr;
                var psSetShaderResources = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, IntPtr*, void>)vtable[8];
                psSetShaderResources(deviceContextPtr, 0, (uint)shaderResources.Length, resourcePtrs);
            }
            finally
            {
                for (int i = 0; i < shaderResources.Length; i++)
                {
                    if (resourcePtrs[i] != IntPtr.Zero)
                    {
                        Marshal.Release(resourcePtrs[i]);
                    }
                }

                Marshal.Release(deviceContextPtr);
            }
        }

        private static unsafe void PSSetSamplersNative(ID3D11DeviceContext deviceContext, ID3D11SamplerState[] samplers)
        {
            IntPtr deviceContextPtr = GetComInterfacePointer(deviceContext, typeof(ID3D11DeviceContext).GUID);
            IntPtr* samplerPtrs = stackalloc IntPtr[samplers.Length];

            try
            {
                for (int i = 0; i < samplers.Length; i++)
                {
                    samplerPtrs[i] = GetComInterfacePointer(samplers[i], typeof(ID3D11SamplerState).GUID);
                }

                IntPtr* vtable = *(IntPtr**)deviceContextPtr;
                var psSetSamplers = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, IntPtr*, void>)vtable[10];
                psSetSamplers(deviceContextPtr, 0, (uint)samplers.Length, samplerPtrs);
            }
            finally
            {
                for (int i = 0; i < samplers.Length; i++)
                {
                    if (samplerPtrs[i] != IntPtr.Zero)
                    {
                        Marshal.Release(samplerPtrs[i]);
                    }
                }

                Marshal.Release(deviceContextPtr);
            }
        }

        private static IntPtr GetComInterfacePointer(object comObject, Guid interfaceId)
        {
            IntPtr unknown = Marshal.GetIUnknownForObject(comObject);

            try
            {
                int hr = Marshal.QueryInterface(unknown, ref interfaceId, out IntPtr interfacePtr);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                return interfacePtr;
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }

        private static T GetComObjectFromInterfacePointer<T>(IntPtr interfacePtr) where T : class
        {
            if (interfacePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create the {typeof(T).Name} COM object.");
            }

            try
            {
                return (T)Marshal.GetObjectForIUnknown(interfacePtr);
            }
            finally
            {
                Marshal.Release(interfacePtr);
            }
        }

        private static unsafe ID3D11Buffer CreateBufferNative(ID3D11Device device, D3D11_BUFFER_DESC* bufferDesc, D3D11_SUBRESOURCE_DATA* initialData, string message)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr bufferPtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createBuffer = (delegate* unmanaged[Stdcall]<IntPtr, D3D11_BUFFER_DESC*, D3D11_SUBRESOURCE_DATA*, IntPtr*, int>)vtable[3];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createBuffer(devicePtr, bufferDesc, initialData, &bufferPtr)), message);
                return GetComObjectFromInterfacePointer<ID3D11Buffer>(bufferPtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11Texture2D CreateTexture2DNative(ID3D11Device device, D3D11_TEXTURE2D_DESC* textureDesc, D3D11_SUBRESOURCE_DATA* initialData, string message)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr texturePtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createTexture2D = (delegate* unmanaged[Stdcall]<IntPtr, D3D11_TEXTURE2D_DESC*, D3D11_SUBRESOURCE_DATA*, IntPtr*, int>)vtable[5];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createTexture2D(devicePtr, textureDesc, initialData, &texturePtr)), message);
                return GetComObjectFromInterfacePointer<ID3D11Texture2D>(texturePtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11RenderTargetView CreateRenderTargetViewNative(ID3D11Device device, ID3D11Texture2D resource)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr resourcePtr = GetComInterfacePointer(resource, typeof(ID3D11Resource).GUID);
            IntPtr renderTargetViewPtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createRenderTargetView = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, D3D11_RENDER_TARGET_VIEW_DESC*, IntPtr*, int>)vtable[9];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createRenderTargetView(devicePtr, resourcePtr, null, &renderTargetViewPtr)), "Failed to create the render target view.");
                return GetComObjectFromInterfacePointer<ID3D11RenderTargetView>(renderTargetViewPtr);
            }
            finally
            {
                Marshal.Release(resourcePtr);
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11DepthStencilView CreateDepthStencilViewNative(ID3D11Device device, ID3D11Texture2D resource)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr resourcePtr = GetComInterfacePointer(resource, typeof(ID3D11Resource).GUID);
            IntPtr depthStencilViewPtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createDepthStencilView = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, D3D11_DEPTH_STENCIL_VIEW_DESC*, IntPtr*, int>)vtable[10];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createDepthStencilView(devicePtr, resourcePtr, null, &depthStencilViewPtr)), "Failed to create the depth stencil view.");
                return GetComObjectFromInterfacePointer<ID3D11DepthStencilView>(depthStencilViewPtr);
            }
            finally
            {
                Marshal.Release(resourcePtr);
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11InputLayout CreateInputLayoutNative(ID3D11Device device, D3D11_INPUT_ELEMENT_DESC* inputElements, uint inputElementCount, void* shaderBytecode, nuint shaderBytecodeLength)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr inputLayoutPtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createInputLayout = (delegate* unmanaged[Stdcall]<IntPtr, D3D11_INPUT_ELEMENT_DESC*, uint, void*, nuint, IntPtr*, int>)vtable[11];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createInputLayout(devicePtr, inputElements, inputElementCount, shaderBytecode, shaderBytecodeLength, &inputLayoutPtr)), "Failed to create the input layout.");
                return GetComObjectFromInterfacePointer<ID3D11InputLayout>(inputLayoutPtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11VertexShader CreateVertexShaderNative(ID3D11Device device, void* shaderBytecode, nuint shaderBytecodeLength)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr vertexShaderPtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createVertexShader = (delegate* unmanaged[Stdcall]<IntPtr, void*, nuint, IntPtr, IntPtr*, int>)vtable[12];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createVertexShader(devicePtr, shaderBytecode, shaderBytecodeLength, IntPtr.Zero, &vertexShaderPtr)), "Failed to create the vertex shader.");
                return GetComObjectFromInterfacePointer<ID3D11VertexShader>(vertexShaderPtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11PixelShader CreatePixelShaderNative(ID3D11Device device, void* shaderBytecode, nuint shaderBytecodeLength)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr pixelShaderPtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createPixelShader = (delegate* unmanaged[Stdcall]<IntPtr, void*, nuint, IntPtr, IntPtr*, int>)vtable[15];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createPixelShader(devicePtr, shaderBytecode, shaderBytecodeLength, IntPtr.Zero, &pixelShaderPtr)), "Failed to create the pixel shader.");
                return GetComObjectFromInterfacePointer<ID3D11PixelShader>(pixelShaderPtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11ShaderResourceView CreateShaderResourceViewNative(ID3D11Device device, ID3D11Texture2D resource)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr resourcePtr = GetComInterfacePointer(resource, typeof(ID3D11Resource).GUID);
            IntPtr shaderResourceViewPtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createShaderResourceView = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, D3D11_SHADER_RESOURCE_VIEW_DESC*, IntPtr*, int>)vtable[7];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createShaderResourceView(devicePtr, resourcePtr, null, &shaderResourceViewPtr)), "Failed to create the shader resource view.");
                return GetComObjectFromInterfacePointer<ID3D11ShaderResourceView>(shaderResourceViewPtr);
            }
            finally
            {
                Marshal.Release(resourcePtr);
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11RasterizerState CreateRasterizerStateNative(ID3D11Device device, D3D11_RASTERIZER_DESC* rasterizerDesc)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr rasterizerStatePtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createRasterizerState = (delegate* unmanaged[Stdcall]<IntPtr, D3D11_RASTERIZER_DESC*, IntPtr*, int>)vtable[22];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createRasterizerState(devicePtr, rasterizerDesc, &rasterizerStatePtr)), "Failed to create the rasterizer state.");
                return GetComObjectFromInterfacePointer<ID3D11RasterizerState>(rasterizerStatePtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private static unsafe ID3D11SamplerState CreateSamplerStateNative(ID3D11Device device, D3D11_SAMPLER_DESC* samplerDesc)
        {
            IntPtr devicePtr = GetComInterfacePointer(device, typeof(ID3D11Device).GUID);
            IntPtr samplerStatePtr = IntPtr.Zero;

            try
            {
                IntPtr* vtable = *(IntPtr**)devicePtr;
                var createSamplerState = (delegate* unmanaged[Stdcall]<IntPtr, D3D11_SAMPLER_DESC*, IntPtr*, int>)vtable[23];
                ThrowIfFailed(new Windows.Win32.Foundation.HRESULT(createSamplerState(devicePtr, samplerDesc, &samplerStatePtr)), "Failed to create the sampler state.");
                return GetComObjectFromInterfacePointer<ID3D11SamplerState>(samplerStatePtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private void RequestRender()
        {
            m_needsRender = true;
        }

        private static (VertexPositionTexture[] Vertices, ushort[] Indices) CreatePanoramaSphere()
        {
            const int size = 30;

            List<VertexPositionTexture> vertices = new(((size * 2) + 1) * (size + 1));
            List<ushort> indices = new((size * 2) * size * 6);

            for (int i = 0; i <= size; i++)
            {
                float phi = MathF.PI * i / size;

                for (int j = 0; j <= (size * 2); j++)
                {
                    float theta = 2.0f * MathF.PI * j / (size * 2);
                    float x = MathF.Sin(phi) * MathF.Cos(theta);
                    float y = MathF.Cos(phi);
                    float z = MathF.Sin(phi) * MathF.Sin(theta);
                    float u = j / (float)(size * 2);
                    float v = phi / MathF.PI;

                    vertices.Add(new VertexPositionTexture(new Vector3(x, y, z), new Vector2(u, v)));
                }
            }

            for (ushort x = 0; x < size; x++)
            {
                for (ushort y = 0; y < (size * 2); y++)
                {
                    ushort v0 = (ushort)(x * ((size * 2) + 1) + y);
                    ushort v1 = (ushort)((x + 1) * ((size * 2) + 1) + y);
                    ushort v2 = (ushort)(x * ((size * 2) + 1) + y + 1);
                    ushort v3 = (ushort)((x + 1) * ((size * 2) + 1) + y + 1);

                    indices.Add(v0);
                    indices.Add(v1);
                    indices.Add(v2);
                    indices.Add(v2);
                    indices.Add(v1);
                    indices.Add(v3);
                }
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        private static unsafe Windows.Win32.Graphics.Direct3D.ID3DBlob CompileShader(string source, string entryPoint, string shaderTarget)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
            byte[] entryPointBytes = Encoding.ASCII.GetBytes(entryPoint + "\0");
            byte[] shaderTargetBytes = Encoding.ASCII.GetBytes(shaderTarget + "\0");

            fixed (byte* sourcePtr = sourceBytes)
            fixed (byte* entryPointPtr = entryPointBytes)
            fixed (byte* shaderTargetPtr = shaderTargetBytes)
            {
                Windows.Win32.Foundation.HRESULT result = Windows.Win32.PInvoke.D3DCompile(
                    sourcePtr,
                    (nuint)sourceBytes.Length,
                    default,
                    (D3D_SHADER_MACRO*)null,
                    null,
                    (Windows.Win32.Foundation.PCSTR)entryPointPtr,
                    (Windows.Win32.Foundation.PCSTR)shaderTargetPtr,
                    0,
                    0,
                    out Windows.Win32.Graphics.Direct3D.ID3DBlob shaderBlob,
                    out Windows.Win32.Graphics.Direct3D.ID3DBlob errorBlob);

                if (result.Value < 0)
                {
                    string errorMessage = Marshal.PtrToStringAnsi((nint)errorBlob.GetBufferPointer(), (int)errorBlob.GetBufferSize()) ?? "Shader compilation failed.";
                    ReleaseComObject(ref errorBlob!);
                    throw new InvalidOperationException(errorMessage);
                }

                ReleaseComObject(ref errorBlob!);
                return shaderBlob;
            }
        }

        private static void ThrowIfFailed(Windows.Win32.Foundation.HRESULT result, string message)
        {
            if (result.Value >= 0)
            {
                return;
            }

            Exception exception = Marshal.GetExceptionForHR(result.Value) ?? new InvalidOperationException(message);
            throw new InvalidOperationException(message, exception);
        }

        private void ReleaseDeviceResources()
        {
            ReleaseComObject(ref m_rasterizerState);
            ReleaseComObject(ref m_samplerState);
            ReleaseComObject(ref m_panoramaTextureView);
            ReleaseComObject(ref m_panoramaTexture);
            ReleaseComObject(ref m_constantBuffer);
            ReleaseComObject(ref m_indexBuffer);
            ReleaseComObject(ref m_vertexBuffer);
            ReleaseComObject(ref m_inputLayout);
            ReleaseComObject(ref m_pixelShader);
            ReleaseComObject(ref m_vertexShader);
            ReleaseComObject(ref m_depthStencilView);
            ReleaseComObject(ref m_depthStencilTexture);
            ReleaseComObject(ref m_renderTargetView);
            ReleaseComObject(ref m_swapchain);
            ReleaseComObject(ref m_d3dContext);
            ReleaseComObject(ref m_dxgiDevice!);
            ReleaseComObject(ref m_d3dDevice);
        }

        private static void ReleaseComObject<T>(ref T? comObject) where T : class
        {
            if (comObject is null)
            {
                return;
            }

            Marshal.ReleaseComObject(comObject);
            comObject = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VertexPositionTexture
        {
            public VertexPositionTexture(Vector3 position, Vector2 textureCoordinate)
            {
                Position = position;
                TextureCoordinate = textureCoordinate;
            }

            public Vector3 Position;

            public Vector2 TextureCoordinate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SceneConstants
        {
            public Matrix4x4 WorldViewProjection;
        }

        private readonly record struct ImageData(byte[] Pixels, uint Width, uint Height)
        {
            public uint RowPitch => Width * 4;
        }

        [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
        internal interface ISwapChainPanelNative
        {
            void SetSwapChain(IDXGISwapChain swapChain);
        }
    }
}
