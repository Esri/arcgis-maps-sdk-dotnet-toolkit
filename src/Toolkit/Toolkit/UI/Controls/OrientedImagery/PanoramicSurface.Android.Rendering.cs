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

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Android.Graphics;
using Android.Opengl;
using Android.Views;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Java.Nio;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// Rendering, on the render thread: the EGL context, created once, the window surface, which follows the
// SurfaceTexture, and the GL scene. The SurfaceTexture callbacks run on the UI thread and post to the render thread.
internal sealed partial class PanoramicSurface : TextureView.ISurfaceTextureListener
{
    private const float FocusRingDips = 3f;

    // GL_CULL_FACE as a glEnable/glDisable capability. Mono.Android has no constant for it (GlCullFace is the method),
    // and GlCullFaceMode (0x0B45) is the glGet enum: passing that to glDisable is GL_INVALID_ENUM.
    private const int GlCullFaceCapability = 0x0B44;

    // EGL and GL objects - owned by the render thread after creation, which disposes them in TearDownEgl and
    // DeleteSceneResources; Dispose enqueues that and joins rather than racing a possibly-wedged thread.
#pragma warning disable CA2213
    private EGLDisplay? _eglDisplay;
    private EGLContext? _eglContext;
    private EGLSurface? _eglSurface;
    private FloatBuffer? _sphereVertices;
    private ShortBuffer? _sphereIndices;
    private FloatBuffer? _markerQuadBuffer;
    private FloatBuffer? _markerQuadUvBuffer;
#pragma warning restore CA2213
    private EGLConfig? _eglConfig;
    private SurfaceTexture? _surfaceTexture;
    private bool _contextEverCreated;
    private bool _contextIsEs3;
    private int _maxTextureSize;

    // GL scene state - render thread only.
    private int _program;
    private int _aPosition;
    private int _aTexCoord;
    private int _uMvp;
    private int _uTexture;
    private int _textureId;
    private int _sphereIndexCount;
    private readonly float[] _mvp = new float[16];
    private readonly float[] _markerQuad = new float[4 * 3];
    private readonly float[] _markerQuadUv = new float[4 * 2];
    private readonly List<(int TextureId, PanoramaMarker Marker)> _glMarkers = new();

    // One texture per swatch, shared by the markers drawn with it. The panoramic viewport hands every marker of a
    // swatch the same pixel array, so the array identifies the swatch.
    private readonly Dictionary<byte[], int> _swatchTextures = new(ReferenceEqualityComparer.Instance);

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
    {
        _surfaceTexture = surface;
        _viewportWidth = width;
        _viewportHeight = height;
        PostToRenderThread(() =>
        {
            // Read before CreateWindowSurface sets the flag. Otherwise the very first surface looks like a recreation
            // and starts a second decode while the first is in flight.
            bool hadContext = _contextEverCreated;
            EnsureEglContext();
            CreateWindowSurface(surface);
            bool recreated;
            lock (_pendingLock)
            {
                recreated = hadContext && !_hasTexture && _pendingBitmap is null;
            }

            _surfaceReady = true;
            ConsumePendingBitmap();
            ConsumePendingMarkers();
            DrawCore();

            // A recreated context with nothing to show: ask the panoramic viewport to re-supply
            // (the DeviceRecreated contract).
            if (recreated)
                Post(() => DeviceRecreated?.Invoke());
        });
    }

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;

        // Some EGL drivers, such as the emulator's, take the next buffer while swapping. The frame after a resize would
        // then draw into an old-size buffer and look stretched. A new window surface gets buffers at the new size.
        PostToRenderThread(() =>
        {
            if (!_surfaceReady)
                return;

            CreateWindowSurface(surface);
            DrawCore();
        });
    }

    bool TextureView.ISurfaceTextureListener.OnSurfaceTextureDestroyed(SurfaceTexture surface)
    {
        // Stop drawing now. The render thread destroys the EGL surface and then releases the SurfaceTexture. The
        // context stays, so the panorama texture survives backgrounding.
        _surfaceReady = false;
        _surfaceTexture = null;
        bool queued = PostToRenderThread(() =>
        {
            DestroyWindowSurface();
            surface.Release();
        });

        // After Dispose nothing runs on the render thread and the EGL surface is gone, so TextureView releases the
        // SurfaceTexture itself.
        return !queued;
    }

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureUpdated(SurfaceTexture surface)
    {
    }

    private void EnsureEglContext()
    {
        if (_eglContext is not null)
            return;

        EGLDisplay? display = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
        if (display is null || display == EGL14.EglNoDisplay)
            throw new InvalidOperationException("Unable to get the default EGL display.");

        int[] version = new int[2];
        if (!EGL14.EglInitialize(display, version, 0, version, 1))
            throw new InvalidOperationException($"Unable to initialize EGL (0x{EGL14.EglGetError():X}).");

        _eglDisplay = display;

        // RGBA8888, no depth/stencil (a single inside-facing sphere needs neither).
        int[] configAttribs =
        {
            EGL14.EglRedSize, 8,
            EGL14.EglGreenSize, 8,
            EGL14.EglBlueSize, 8,
            EGL14.EglAlphaSize, 8,
            EGL14.EglRenderableType, EGL14.EglOpenglEs2Bit,
            EGL14.EglNone,
        };
        var configs = new EGLConfig[1];
        int[] numConfigs = new int[1];
        if (!EGL14.EglChooseConfig(display, configAttribs, 0, configs, 0, 1, numConfigs, 0) || numConfigs[0] == 0)
            throw new InvalidOperationException("No suitable EGL config found.");

        _eglConfig = configs[0];

        // Create an ES2 context first and read GL_VERSION before upgrading to ES3. An ES3 context created outright can
        // pass creation and then fail eglMakeCurrent with EGL_BAD_MATCH on some devices.
        int[] es2Attribs = { EGL14.EglContextClientVersion, 2, EGL14.EglNone };
        EGLContext? context = EGL14.EglCreateContext(display, _eglConfig, EGL14.EglNoContext, es2Attribs, 0);
        if (context is null || context == EGL14.EglNoContext)
            throw new InvalidOperationException("Unable to create an OpenGL ES 2 context.");

        // Probe the actual GL version with a throwaway pbuffer surface (no window surface exists yet).
        int[] pbufferAttribs = { EGL14.EglWidth, 1, EGL14.EglHeight, 1, EGL14.EglNone };
        EGLSurface? probe = EGL14.EglCreatePbufferSurface(display, _eglConfig, pbufferAttribs, 0);
        string? glVersion = null;
        if (probe is not null && probe != EGL14.EglNoSurface)
        {
            if (EGL14.EglMakeCurrent(display, probe, probe, context))
            {
                glVersion = GLES20.GlGetString(GLES20.GlVersion);
                EGL14.EglMakeCurrent(display, EGL14.EglNoSurface, EGL14.EglNoSurface, EGL14.EglNoContext);
            }

            EGL14.EglDestroySurface(display, probe);
        }

        if (glVersion is not null && glVersion.StartsWith("OpenGL ES 3", StringComparison.Ordinal))
        {
            // ES3 makes non-power-of-two textures with REPEAT wrap core (needed for the u seam on NPOT panoramas).
            int[] es3Attribs = { EGL14.EglContextClientVersion, 3, EGL14.EglNone };
            EGLContext? es3 = EGL14.EglCreateContext(display, _eglConfig, EGL14.EglNoContext, es3Attribs, 0);
            if (es3 is not null && es3 != EGL14.EglNoContext)
            {
                EGL14.EglDestroyContext(display, context);
                context = es3;
                _contextIsEs3 = true;
            }
        }

        _eglContext = context;
    }

    private void CreateWindowSurface(SurfaceTexture surface)
    {
        DestroyWindowSurface();
        int[] surfaceAttribs = { EGL14.EglNone };
        EGLSurface? eglSurface = EGL14.EglCreateWindowSurface(_eglDisplay, _eglConfig, surface, surfaceAttribs, 0);
        if (eglSurface is null || eglSurface == EGL14.EglNoSurface)
            throw new InvalidOperationException($"Unable to create the EGL window surface (0x{EGL14.EglGetError():X}).");

        _eglSurface = eglSurface;
        if (!EGL14.EglMakeCurrent(_eglDisplay, eglSurface, eglSurface, _eglContext))
            throw new InvalidOperationException($"eglMakeCurrent failed (0x{EGL14.EglGetError():X}).");

        if (_program == 0)
            CreateSceneResources();

        _contextEverCreated = true;
    }

    private void DestroyWindowSurface()
    {
        if (_eglSurface is not null && _eglSurface != EGL14.EglNoSurface)
        {
            EGL14.EglMakeCurrent(_eglDisplay, EGL14.EglNoSurface, EGL14.EglNoSurface, EGL14.EglNoContext);
            EGL14.EglDestroySurface(_eglDisplay, _eglSurface);
            _eglSurface.Dispose();
        }

        _eglSurface = null;
    }

    // Full teardown. Runs on the render thread, which owns the GL/EGL objects and their JNI wrappers.
    private void TearDownEgl()
    {
        DeleteSceneResources();
        DestroyWindowSurface();
        if (_eglDisplay is not null && _eglDisplay != EGL14.EglNoDisplay)
        {
            if (_eglContext is not null && _eglContext != EGL14.EglNoContext)
                EGL14.EglDestroyContext(_eglDisplay, _eglContext);

            EGL14.EglReleaseThread();
            // Android ref-counts the EGLDisplay: every eglInitialize needs a matching eglTerminate.
            EGL14.EglTerminate(_eglDisplay);
        }

        _eglContext?.Dispose();
        _eglDisplay?.Dispose();
        _eglConfig?.Dispose();
        _eglContext = null;
        _eglDisplay = null;
        _eglConfig = null;
        _contextIsEs3 = false;
        _hasTexture = false;
        _program = 0;
        _textureId = 0;
    }

    // EGL_CONTEXT_LOST: rebuild on the spot; the panoramic viewport re-supplies content through DeviceRecreated.
    private void RecoverFromContextLoss()
    {
        Post(() => DeviceLost?.Invoke());
        SurfaceTexture? surface = _surfaceTexture;
        TearDownEgl();
        if (surface is null || !_surfaceReady)
            return;

        EnsureEglContext();
        CreateWindowSurface(surface);
        Post(() => DeviceRecreated?.Invoke());
    }

    private const string VertexShaderSource = """
        uniform mat4 u_MVP;
        attribute vec3 a_Position;
        attribute vec2 a_TexCoord;
        varying vec2 v_TexCoord;
        void main()
        {
            v_TexCoord = a_TexCoord;
            gl_Position = u_MVP * vec4(a_Position, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        precision mediump float;
        uniform sampler2D u_Texture;
        varying vec2 v_TexCoord;
        void main()
        {
            gl_FragColor = texture2D(u_Texture, v_TexCoord);
        }
        """;

    private void CreateSceneResources()
    {
        _program = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _aPosition = GLES20.GlGetAttribLocation(_program, "a_Position");
        _aTexCoord = GLES20.GlGetAttribLocation(_program, "a_TexCoord");
        _uMvp = GLES20.GlGetUniformLocation(_program, "u_MVP");
        _uTexture = GLES20.GlGetUniformLocation(_program, "u_Texture");

        GLES20.GlDisable(GlCullFaceCapability); // inside-facing sphere
        GLES20.GlDisable(GLES20.GlDepthTest);   // single sphere + overlay quads; painter's order suffices

        int[] maxTexture = new int[1];
        GLES20.GlGetIntegerv(GLES20.GlMaxTextureSize, maxTexture, 0);
        _maxTextureSize = maxTexture[0];

        CreateSphereMesh();
        _markerQuadBuffer = CreateFloatBuffer(_markerQuad.Length);
        _markerQuadUvBuffer = CreateFloatBuffer(_markerQuadUv.Length);

        // Leave setup with a clean error state, so an upload check doesn't see errors left over from setup.
        DrainGlErrors();
    }

    private void DeleteSceneResources()
    {
        DeleteTexture();
        DeleteMarkerTextures();
        _sphereVertices?.Dispose();
        _sphereIndices?.Dispose();
        _markerQuadBuffer?.Dispose();
        _markerQuadUvBuffer?.Dispose();
        _sphereVertices = null;
        _sphereIndices = null;
        _markerQuadBuffer = null;
        _markerQuadUvBuffer = null;
    }

    private void CreateSphereMesh()
    {
        (float[] vertices, short[] indices) = PanoramaCameraState.CreateSphereMesh();
        _sphereVertices = ToFloatBuffer(vertices);
        _sphereIndices = ToShortBuffer(indices);
        _sphereIndexCount = indices.Length;
    }

    // Creates a linearly filtered 2D texture with the given horizontal wrap mode and leaves it bound.
    private static int CreateTexture(int wrapS)
    {
        int[] ids = new int[1];
        GLES20.GlGenTextures(1, ids, 0);
        GLES20.GlBindTexture(GLES20.GlTexture2d, ids[0]);
        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, GLES20.GlLinear);
        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, GLES20.GlLinear);
        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, wrapS);
        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);
        return ids[0];
    }

    private void ConsumePendingBitmap()
    {
        // Without a context and surface yet, the stash stays for OnSurfaceTextureAvailable to consume.
        if (_eglContext is null || _eglSurface is null)
            return;

        Bitmap? bitmap;
        lock (_pendingLock)
        {
            bitmap = _pendingBitmap;
            _pendingBitmap = null;
        }

        if (bitmap is null)
            return;

        try
        {
            // The decode already applied the memory budget; enforce the device texture cap here (rare).
            if (_maxTextureSize > 0 && (bitmap.Width > _maxTextureSize || bitmap.Height > _maxTextureSize))
            {
                float scale = Math.Min(_maxTextureSize / (float)bitmap.Width, _maxTextureSize / (float)bitmap.Height);
                Bitmap scaled = Bitmap.CreateScaledBitmap(bitmap, Math.Max(1, (int)(bitmap.Width * scale)), Math.Max(1, (int)(bitmap.Height * scale)), true)!;
                bitmap.Recycle();
                bitmap = scaled;
            }

            DeleteTexture();
            DrainGlErrors(); // the post-upload check must only see the upload's own errors

            // ES2 treats a non-power-of-two texture with REPEAT as incomplete (samples black), so ES2-only devices take
            // CLAMP_TO_EDGE for NPOT panoramas: a hairline seam beats a black sphere.
            bool repeatSafe = _contextIsEs3 || (BitOperations.IsPow2(bitmap.Width) && BitOperations.IsPow2(bitmap.Height));
            _textureId = CreateTexture(repeatSafe ? GLES20.GlRepeat : GLES20.GlClampToEdge);
            GLUtils.TexImage2D(GLES20.GlTexture2d, 0, bitmap, 0);
            ThrowOnGlError("panorama texture upload");
            GLES20.GlBindTexture(GLES20.GlTexture2d, 0);
            _hasTexture = true;
        }
        finally
        {
            bitmap.Recycle(); // no retained CPU copy (DeviceRecreated re-supplies on loss)
        }

        DrawCore();
    }

    // Uploads only the swatches the previous set didn't use, and deletes the ones this set doesn't.
    private void ConsumePendingMarkers()
    {
        if (_eglContext is null || _eglSurface is null)
            return;

        PanoramaMarker[]? markers;
        lock (_pendingLock)
        {
            markers = _pendingMarkers;
            _pendingMarkers = null;
        }

        if (markers is null)
            return;

        _glMarkers.Clear();
        var usedSwatches = new HashSet<byte[]>(ReferenceEqualityComparer.Instance);
        DrainGlErrors(); // the per-swatch checks must only see their own upload's errors
        foreach (PanoramaMarker marker in markers)
        {
            // A marker that can't be uploaded is skipped. It never fails the panorama.
            if (!marker.IsValid)
                continue;

            if (!_swatchTextures.TryGetValue(marker.Bgra, out int textureId))
            {
                textureId = UploadSwatch(marker);
                if (textureId == 0)
                    continue;

                _swatchTextures.Add(marker.Bgra, textureId);
            }

            usedSwatches.Add(marker.Bgra);
            _glMarkers.Add((textureId, marker));
        }

        DeleteSwatchTextures(swatch => !usedSwatches.Contains(swatch));
        DrawCore();
    }

    // Uploads a marker's swatch and returns the texture's name, or 0 when the GPU rejects it.
    private static int UploadSwatch(PanoramaMarker marker)
    {
        // Swatches arrive as BGRA, and GLES 2 has no BGRA format without an extension, so they are swapped to RGBA
        // on the CPU. Swatches are tiny.
        byte[] bgra = marker.Bgra;
        byte[] rgba = new byte[bgra.Length];
        for (int i = 0; i + 3 < bgra.Length; i += 4)
        {
            rgba[i] = bgra[i + 2];
            rgba[i + 1] = bgra[i + 1];
            rgba[i + 2] = bgra[i];
            rgba[i + 3] = bgra[i + 3];
        }

        int textureId = CreateTexture(GLES20.GlClampToEdge);
        using (ByteBuffer buffer = ByteBuffer.AllocateDirect(rgba.Length))
        {
            buffer.Put(rgba);
            buffer.Position(0);
            GLES20.GlTexImage2D(GLES20.GlTexture2d, 0, GLES20.GlRgba, marker.Width, marker.Height, 0, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);
        }

        bool uploaded = GLES20.GlGetError() == GLES20.GlNoError;
        DrainGlErrors();
        GLES20.GlBindTexture(GLES20.GlTexture2d, 0);
        if (uploaded)
            return textureId;

        GLES20.GlDeleteTextures(1, new[] { textureId }, 0);
        return 0;
    }

    private void DeleteTexture()
    {
        if (_textureId != 0)
        {
            GLES20.GlDeleteTextures(1, new[] { _textureId }, 0);
            _textureId = 0;
        }

        _hasTexture = false;
    }

    private void DeleteMarkerTextures()
    {
        DeleteSwatchTextures(_ => true);
        _glMarkers.Clear();
    }

    // Deletes the chosen textures in one GL call. Dictionary allows removing the current entry while enumerating.
    private void DeleteSwatchTextures(Func<byte[], bool> shouldDelete)
    {
        var textureIds = new List<int>();
        foreach ((byte[] swatch, int textureId) in _swatchTextures)
        {
            if (!shouldDelete(swatch))
                continue;

            textureIds.Add(textureId);
            _swatchTextures.Remove(swatch);
        }

        if (textureIds.Count > 0)
            GLES20.GlDeleteTextures(textureIds.Count, textureIds.ToArray(), 0);
    }

    private void DrawCore()
    {
        if (!_surfaceReady || _eglContext is null || _eglSurface is null || _eglSurface == EGL14.EglNoSurface)
            return;

        if (!EGL14.EglMakeCurrent(_eglDisplay, _eglSurface, _eglSurface, _eglContext))
        {
            HandleEglFailure("eglMakeCurrent");
            return;
        }

        int width = _viewportWidth;
        int height = _viewportHeight;
        if (width <= 0 || height <= 0)
            return;

        GLES20.GlViewport(0, 0, width, height);
        GLES20.GlClearColor(_clearR, _clearG, _clearB, _clearA);
        GLES20.GlClear(GLES20.GlColorBufferBit);

        if (_hasTexture && _program != 0)
        {
            PanoramaCameraState camera = Camera;

            GLES20.GlUseProgram(_program);

            // Row-major System.Numerics bytes read column-major by GLSL are the transpose the row-vector math needs, so
            // upload with transpose = false.
            WriteMatrix(camera.GetWorldViewProjection(width / (float)Math.Max(1, height)), _mvp);
            GLES20.GlUniformMatrix4fv(_uMvp, 1, false, _mvp, 0);

            GLES20.GlActiveTexture(GLES20.GlTexture0);
            GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
            GLES20.GlUniform1i(_uTexture, 0);

            _sphereVertices!.Position(0);
            GLES20.GlEnableVertexAttribArray(_aPosition);
            GLES20.GlVertexAttribPointer(_aPosition, 3, GLES20.GlFloat, false, 5 * sizeof(float), _sphereVertices);
            _sphereVertices.Position(3);
            GLES20.GlEnableVertexAttribArray(_aTexCoord);
            GLES20.GlVertexAttribPointer(_aTexCoord, 2, GLES20.GlFloat, false, 5 * sizeof(float), _sphereVertices);
            _sphereIndices!.Position(0);
            GLES20.GlDrawElements(GLES20.GlTriangles, _sphereIndexCount, GLES20.GlUnsignedShort, _sphereIndices);

            DrawMarkers(width, height, camera);

            GLES20.GlDisableVertexAttribArray(_aPosition);
            GLES20.GlDisableVertexAttribArray(_aTexCoord);
        }

        if (_showFocusRing)
            DrawFocusRing(width, height);

        if (!EGL14.EglSwapBuffers(_eglDisplay, _eglSurface))
            HandleEglFailure("eglSwapBuffers");
    }

    // A band along each edge, painted with scissored clears.
    private void DrawFocusRing(int width, int height)
    {
        int band = Math.Min((int)MathF.Round(FocusRingDips * _density), Math.Min(width, height) / 2);
        GLES20.GlEnable(GLES20.GlScissorTest);
        GLES20.GlClearColor(_focusRingR, _focusRingG, _focusRingB, 1f);
        ClearRect(0, 0, width, band);
        ClearRect(0, height - band, width, band);
        ClearRect(0, 0, band, height);
        ClearRect(width - band, 0, band, height);
        GLES20.GlDisable(GLES20.GlScissorTest);

        static void ClearRect(int x, int y, int w, int h)
        {
            GLES20.GlScissor(x, y, w, h);
            GLES20.GlClear(GLES20.GlColorBufferBit);
        }
    }

    // Screen-aligned alpha-blended quads sized to the swatch and shifted by the symbol offset, projected with the shared
    // camera math, drawn with an identity MVP.
    private void DrawMarkers(int width, int height, PanoramaCameraState camera)
    {
        if (_glMarkers.Count == 0 || _markerQuadBuffer is null || _markerQuadUvBuffer is null)
            return;

        Matrix4x4 identity = Matrix4x4.Identity;
        WriteMatrix(identity, _mvp);
        GLES20.GlUniformMatrix4fv(_uMvp, 1, false, _mvp, 0);

        GLES20.GlEnable(GLES20.GlBlend);
        GLES20.GlBlendFunc(GLES20.GlOne, GLES20.GlOneMinusSrcAlpha); // source-over for premultiplied swatches

        foreach ((int textureId, PanoramaMarker marker) in _glMarkers)
        {
            if (!camera.TryGetMarkerQuad(marker, width, height, out var quad))
                continue;

            // Two triangles as a strip: top-left, bottom-left, top-right, bottom-right.
            SetQuadVertex(0, quad.Left, quad.Top, 0f, 0f);
            SetQuadVertex(1, quad.Left, quad.Bottom, 0f, 1f);
            SetQuadVertex(2, quad.Right, quad.Top, 1f, 0f);
            SetQuadVertex(3, quad.Right, quad.Bottom, 1f, 1f);
            _markerQuadBuffer.Position(0);
            _markerQuadBuffer.Put(_markerQuad);
            _markerQuadBuffer.Position(0);
            _markerQuadUvBuffer.Position(0);
            _markerQuadUvBuffer.Put(_markerQuadUv);
            _markerQuadUvBuffer.Position(0);

            GLES20.GlBindTexture(GLES20.GlTexture2d, textureId);
            GLES20.GlVertexAttribPointer(_aPosition, 3, GLES20.GlFloat, false, 0, _markerQuadBuffer);
            GLES20.GlVertexAttribPointer(_aTexCoord, 2, GLES20.GlFloat, false, 0, _markerQuadUvBuffer);
            GLES20.GlDrawArrays(GLES20.GlTriangleStrip, 0, 4);
        }

        GLES20.GlDisable(GLES20.GlBlend);
    }

    private void SetQuadVertex(int index, float x, float y, float u, float v)
    {
        _markerQuad[index * 3] = x;
        _markerQuad[(index * 3) + 1] = y;
        _markerQuad[(index * 3) + 2] = 0f;
        _markerQuadUv[index * 2] = u;
        _markerQuadUv[(index * 2) + 1] = v;
    }

    private void HandleEglFailure(string operation)
    {
        int error = EGL14.EglGetError();
        if (error == EGL14.EglContextLost)
        {
            RecoverFromContextLoss();
            return;
        }

        ReportRenderFailure(new InvalidOperationException($"{operation} failed (0x{error:X})."));
    }

    // glGetError returns one latched flag per call, and flags persist until read. Draining before a checked
    // operation keeps a stale error from an earlier call from being blamed on it.
    private static void DrainGlErrors()
    {
        while (GLES20.GlGetError() != GLES20.GlNoError)
        {
        }
    }

    private static void ThrowOnGlError(string operation)
    {
        int error = GLES20.GlGetError();
        if (error == GLES20.GlNoError)
            return;

        DrainGlErrors(); // clear any additional latched flags so the next check starts clean
        throw new InvalidOperationException($"OpenGL error 0x{error:X} during {operation}.");
    }

    // Matrix4x4 is sixteen sequential floats, M11 first.
    private static void WriteMatrix(in Matrix4x4 m, float[] destination) =>
        MemoryMarshal.Cast<Matrix4x4, float>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in m), 1)).CopyTo(destination);

    private static int CreateProgram(string vertexSource, string fragmentSource)
    {
        int vertexShader = CompileShader(GLES20.GlVertexShader, vertexSource);
        int fragmentShader = CompileShader(GLES20.GlFragmentShader, fragmentSource);
        int program = GLES20.GlCreateProgram();
        GLES20.GlAttachShader(program, vertexShader);
        GLES20.GlAttachShader(program, fragmentShader);
        GLES20.GlLinkProgram(program);
        int[] status = new int[1];
        GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, status, 0);
        GLES20.GlDeleteShader(vertexShader);
        GLES20.GlDeleteShader(fragmentShader);
        if (status[0] == 0)
        {
            string? log = GLES20.GlGetProgramInfoLog(program);
            GLES20.GlDeleteProgram(program);
            throw new InvalidOperationException($"OpenGL program link failed: {log}");
        }

        return program;
    }

    private static int CompileShader(int type, string source)
    {
        int shader = GLES20.GlCreateShader(type);
        GLES20.GlShaderSource(shader, source);
        GLES20.GlCompileShader(shader);
        int[] status = new int[1];
        GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, status, 0);
        if (status[0] == 0)
        {
            string? log = GLES20.GlGetShaderInfoLog(shader);
            GLES20.GlDeleteShader(shader);
            throw new InvalidOperationException($"OpenGL shader compile failed: {log}");
        }

        return shader;
    }

    private static FloatBuffer CreateFloatBuffer(int floatCount)
        => ByteBuffer.AllocateDirect(floatCount * 4)!.Order(ByteOrder.NativeOrder())!.AsFloatBuffer()!;

    private static FloatBuffer ToFloatBuffer(float[] data)
    {
        FloatBuffer buffer = CreateFloatBuffer(data.Length);
        buffer.Put(data);
        buffer.Position(0);
        return buffer;
    }

    private static ShortBuffer ToShortBuffer(short[] data)
    {
        ShortBuffer buffer = ByteBuffer.AllocateDirect(data.Length * 2)!.Order(ByteOrder.NativeOrder())!.AsShortBuffer()!;
        buffer.Put(data);
        buffer.Position(0);
        return buffer;
    }
}
#endif
