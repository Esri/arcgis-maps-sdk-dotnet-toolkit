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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.Views;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Java.Nio;
using static Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramaCameraState;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// Android surface for the panoramic display: a TextureView with its own EGL context on a dedicated render thread,
// the host the SDK GeoView uses. The context is created once and survives backgrounding; only the window surface
// follows the SurfaceTexture. Rendering is on demand. Mesh and camera come from PanoramaCameraState.
internal sealed class PanoramicSurface : TextureView, TextureView.ISurfaceTextureListener
{
    // GL_CULL_FACE as a glEnable/glDisable capability. Mono.Android has no constant for it (GlCullFace is the method),
    // and GlCullFaceMode (0x0B45) is the glGet enum: passing that to glDisable is GL_INVALID_ENUM.
    private const int GlCullFaceCapability = 0x0B44;

    // All EGL/GL work runs on this serial queue; state transitions are atomic gates, so there is no dispose lock.
    private readonly BlockingCollection<Action> _renderQueue = new();
    private Thread? _renderThread;
    private int _renderQueued;
    private int _disposed;

    // EGL and GL objects - owned by the render thread after creation, which disposes them in TearDownEgl and
    // DeleteSceneResources; Dispose enqueues that and joins rather than racing a possibly-wedged thread.
#pragma warning disable CA2213
    private EGLDisplay? _eglDisplay;
    private EGLContext? _eglContext;
    private EGLSurface? _eglSurface;
    private FloatBuffer? _sphereVertices;
    private FloatBuffer? _sphereTexCoords;
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
    private readonly List<GlMarker> _glMarkers = new();

    // Cross-thread state. Camera fields are written on the UI thread and read on the render thread; float
    // reads/writes are atomic and every change is followed by a queued draw (a full fence), so no locks needed.
    private float _yaw;
    private float _pitch;
    private float _fieldOfView = MathF.PI / 2f;
    private float _clearR = 0.02f;
    private float _clearG = 0.02f;
    private float _clearB = 0.02f;
    private float _clearA = 1f;
    private volatile bool _surfaceReady;
    private volatile bool _hasTexture;
    private int _viewportWidth;
    private int _viewportHeight;

    // Pending content, applied by the render thread once the context exists (consume-once; the surface keeps no
    // CPU copy after upload - on a real context loss DeviceRecreated asks the display to re-decode).
    private readonly object _pendingLock = new();
    private Bitmap? _pendingBitmap;
    private MarkerSwatch[]? _pendingMarkers;

    private readonly GestureDetector _gestureDetector;
    private readonly ScaleGestureDetector _scaleDetector;

    public PanoramicSurface(Context context)
        : base(context)
    {
        SurfaceTextureListener = this;
        _gestureDetector = new GestureDetector(context, new PanGestureListener(this));
        _scaleDetector = new ScaleGestureDetector(context, new PinchListener(this));
    }

    // Same event surface as the Windows PanoramicSurface, so the display's contract layer stays shared.
    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    public event Action? DeviceRecreated;

    public event Action? CameraChanged;

    public float Yaw
    {
        get => _yaw;
        set => SetCamera(ref _yaw, value);
    }

    public float Pitch
    {
        get => _pitch;
        set => SetCamera(ref _pitch, value);
    }

    public float FieldOfView
    {
        get => _fieldOfView;
        set => SetCamera(ref _fieldOfView, value);
    }

    // Windows-surface-compatible size accessors (physical pixels on Android).
    public double ActualWidth => Width;

    public double ActualHeight => Height;

    private void SetCamera(ref float field, float value)
    {
        if (field == value)
            return;

        field = value;
        CameraChanged?.Invoke();
    }

    // Takes ownership of the bitmap (recycled after upload or when superseded). If it still exceeds the GL max
    // texture size it is downscaled once at upload.
    public void SetTexture(Bitmap bitmap)
    {
        lock (_pendingLock)
        {
            _pendingBitmap?.Recycle();
            _pendingBitmap = bitmap;
        }

        PostToRenderThread(ConsumePendingBitmap);
    }

    public void ClearTexture()
    {
        lock (_pendingLock)
        {
            _pendingBitmap?.Recycle();
            _pendingBitmap = null;
        }

        _hasTexture = false;
        PostToRenderThread(() =>
        {
            DeleteTexture();
            DrawCore();
        });
    }

    public void SetMarkers(IReadOnlyList<MarkerSwatch> swatches)
    {
        MarkerSwatch[] copy = swatches.ToArray();
        lock (_pendingLock)
        {
            _pendingMarkers = copy;
        }

        PostToRenderThread(ConsumePendingMarkers);
    }

    public void SetClearColor(float r, float g, float b, float a)
    {
        _clearR = r;
        _clearG = g;
        _clearB = b;
        _clearA = a;
    }

    // Coalesced on-demand render: at most one draw is queued at a time.
    public void RequestRender()
    {
        if (Interlocked.Exchange(ref _renderQueued, 1) == 0)
        {
            PostToRenderThread(() =>
            {
                Interlocked.Exchange(ref _renderQueued, 0);
                DrawCore();
            });
        }
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null)
            return false;

        _scaleDetector.OnTouchEvent(e);
        _gestureDetector.OnTouchEvent(e);
        return true;
    }

    public override bool OnGenericMotionEvent(MotionEvent? e)
    {
        // A mouse wheel (emulator, ChromeOS, DeX) arrives as an ACTION_SCROLL generic motion event, not a touch. Map it
        // to zoom like the Windows wheel; consuming it also stops the synthetic-pan fallback.
        if (e?.Action == MotionEventActions.Scroll)
        {
            float notches = e.GetAxisValue(Axis.Vscroll); // wheel up = positive = zoom in (narrower FOV)
            if (notches != 0f)
            {
                FieldOfView = Math.Clamp(FieldOfView - (notches * 0.1f), MinFieldOfView, MaxFieldOfView);
                RequestRender();
                return true;
            }
        }

        return base.OnGenericMotionEvent(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            lock (_pendingLock)
            {
                _pendingBitmap?.Recycle();
                _pendingBitmap = null;
                _pendingMarkers = null;
            }

            bool stopped = true;
            if (_renderThread is not null)
            {
                _renderQueue.Add(TearDownEgl); // releases the GL/EGL objects and their JNI wrappers on their owner thread
                _renderQueue.CompleteAdding();
                stopped = _renderThread.Join(2000);
            }

            // A failed join means the render thread is wedged in a driver call: leave the queue alive rather than dispose
            // it under a live consumer. The queued TearDownEgl then cleans up late.
            if (stopped)
                _renderQueue.Dispose();

            _gestureDetector.Dispose();
            _scaleDetector.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PostToRenderThread(Action action)
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        EnsureRenderThread();
        try
        {
            _renderQueue.Add(action);
        }
        catch (InvalidOperationException)
        {
            // Completed during teardown; nothing left to do.
        }
    }

    private void EnsureRenderThread()
    {
        if (_renderThread is not null)
            return;

        var thread = new Thread(RenderLoop) { Name = "PanoramicSurface render thread", IsBackground = true };
        if (Interlocked.CompareExchange(ref _renderThread, thread, null) is null)
            thread.Start();
    }

    private void RenderLoop()
    {
        foreach (Action action in _renderQueue.GetConsumingEnumerable())
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ReportRenderFailure(ex);
            }
        }
    }

    // Failures on the render thread surface as Error via the display; marshal to the UI thread first.
    private void ReportRenderFailure(Exception ex) => Post(() => RenderFailed?.Invoke(ex));

    #region ISurfaceTextureListener (UI thread) -> render thread

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
    {
        _surfaceTexture = surface;
        _viewportWidth = width;
        _viewportHeight = height;
        PostToRenderThread(() =>
        {
            // Snapshot BEFORE CreateWindowSurface sets the flag, or the very first surface reads as a
            // "recreation" and triggers a redundant second load/decode while the initial one is in flight.
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

            // A recreated context with nothing to show: ask the display to re-supply (the DeviceRecreated contract).
            if (recreated)
                Post(() => DeviceRecreated?.Invoke());
        });
    }

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        RequestRender(); // the EGL window surface tracks the SurfaceTexture buffer; only the viewport changes
    }

    bool TextureView.ISurfaceTextureListener.OnSurfaceTextureDestroyed(SurfaceTexture surface)
    {
        // Stop drawing now; destroy the EGL surface (keep the context - the panorama texture survives
        // backgrounding) and release the SurfaceTexture on the render thread after it is no longer bound.
        _surfaceReady = false;
        _surfaceTexture = null;
        PostToRenderThread(() =>
        {
            DestroyWindowSurface();
            surface.Release();
        });
        return false; // we release it ourselves once the EGL surface is gone
    }

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureUpdated(SurfaceTexture surface)
    {
    }

    #endregion

    #region EGL lifecycle (render thread)

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

    // EGL_CONTEXT_LOST: rebuild on the spot; the display re-supplies content through DeviceRecreated.
    private void RecoverFromContextLoss()
    {
        SurfaceTexture? surface = _surfaceTexture;
        TearDownEgl();
        if (surface is null || !_surfaceReady)
            return;

        EnsureEglContext();
        CreateWindowSurface(surface);
        Post(() => DeviceRecreated?.Invoke());
    }

    #endregion

    #region GL scene (render thread)

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

        // Leave setup with a clean error state so upload checks can't be poisoned by init-time noise.
        DrainGlErrors();
    }

    private void DeleteSceneResources()
    {
        DeleteTexture();
        DeleteMarkerTextures();
        _sphereVertices?.Dispose();
        _sphereTexCoords?.Dispose();
        _sphereIndices?.Dispose();
        _markerQuadBuffer?.Dispose();
        _markerQuadUvBuffer?.Dispose();
        _sphereVertices = null;
        _sphereTexCoords = null;
        _sphereIndices = null;
        _markerQuadBuffer = null;
        _markerQuadUvBuffer = null;
    }

    private void CreateSphereMesh()
    {
        (float[] positions, float[] texCoords, short[] indices) = PanoramaCameraState.CreateSphereMesh();
        _sphereVertices = ToFloatBuffer(positions);
        _sphereTexCoords = ToFloatBuffer(texCoords);
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
        // Render thread only: check readiness before taking the stash; if not ready, the surface-available path consumes it.
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

    private void ConsumePendingMarkers()
    {
        if (_eglContext is null || _eglSurface is null)
            return;

        MarkerSwatch[]? swatches;
        lock (_pendingLock)
        {
            swatches = _pendingMarkers;
            _pendingMarkers = null;
        }

        if (swatches is null)
            return;

        DeleteMarkerTextures();
        DrainGlErrors(); // the per-swatch checks must only see their own upload's errors
        foreach (MarkerSwatch swatch in swatches)
        {
            // Swatches arrive as BGRA (RuntimeImage raw buffer); GLES2 has no BGRA format without an
            // extension, so swap to RGBA on the CPU - swatches are tiny.
            byte[] rgba = new byte[swatch.Bgra.Length];
            for (int i = 0; i + 3 < swatch.Bgra.Length; i += 4)
            {
                rgba[i] = swatch.Bgra[i + 2];
                rgba[i + 1] = swatch.Bgra[i + 1];
                rgba[i + 2] = swatch.Bgra[i];
                rgba[i + 3] = swatch.Bgra[i + 3];
            }

            int textureId = CreateTexture(GLES20.GlClampToEdge);
            using (ByteBuffer buffer = ByteBuffer.AllocateDirect(rgba.Length))
            {
                buffer.Put(rgba);
                buffer.Position(0);
                GLES20.GlTexImage2D(GLES20.GlTexture2d, 0, GLES20.GlRgba, swatch.Width, swatch.Height, 0, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);
            }

            ThrowOnGlError("marker texture upload");
            GLES20.GlBindTexture(GLES20.GlTexture2d, 0);
            _glMarkers.Add(new GlMarker(textureId, swatch.U, swatch.V, swatch.Width, swatch.Height));
        }

        DrawCore();
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
        foreach (GlMarker marker in _glMarkers)
            GLES20.GlDeleteTextures(1, new[] { marker.TextureId }, 0);

        _glMarkers.Clear();
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
            float yaw = _yaw;
            float pitch = _pitch;
            float fov = _fieldOfView;

            GLES20.GlUseProgram(_program);

            // Row-major System.Numerics bytes read column-major by GLSL are the transpose the row-vector math needs, so
            // upload with transpose = false.
            WriteMatrix(new PanoramaCameraState(yaw, pitch, fov).GetWorldViewProjection(width / (float)Math.Max(1, height)), _mvp);
            GLES20.GlUniformMatrix4fv(_uMvp, 1, false, _mvp, 0);

            GLES20.GlActiveTexture(GLES20.GlTexture0);
            GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
            GLES20.GlUniform1i(_uTexture, 0);

            _sphereVertices!.Position(0);
            GLES20.GlEnableVertexAttribArray(_aPosition);
            GLES20.GlVertexAttribPointer(_aPosition, 3, GLES20.GlFloat, false, 0, _sphereVertices);
            _sphereTexCoords!.Position(0);
            GLES20.GlEnableVertexAttribArray(_aTexCoord);
            GLES20.GlVertexAttribPointer(_aTexCoord, 2, GLES20.GlFloat, false, 0, _sphereTexCoords);
            _sphereIndices!.Position(0);
            GLES20.GlDrawElements(GLES20.GlTriangles, _sphereIndexCount, GLES20.GlUnsignedShort, _sphereIndices);

            DrawMarkers(width, height, yaw, pitch, fov);

            GLES20.GlDisableVertexAttribArray(_aPosition);
            GLES20.GlDisableVertexAttribArray(_aTexCoord);
        }

        if (!EGL14.EglSwapBuffers(_eglDisplay, _eglSurface))
            HandleEglFailure("eglSwapBuffers");
    }

    // Screen-aligned alpha-blended quads sized to the swatch, projected with the shared camera math, drawn with an identity MVP.
    private void DrawMarkers(int width, int height, float yaw, float pitch, float fov)
    {
        if (_glMarkers.Count == 0 || _markerQuadBuffer is null || _markerQuadUvBuffer is null)
            return;

        var camera = new PanoramaCameraState(yaw, pitch, fov);
        Matrix4x4 identity = Matrix4x4.Identity;
        WriteMatrix(identity, _mvp);
        GLES20.GlUniformMatrix4fv(_uMvp, 1, false, _mvp, 0);

        GLES20.GlEnable(GLES20.GlBlend);
        GLES20.GlBlendFunc(GLES20.GlSrcAlpha, GLES20.GlOneMinusSrcAlpha); // straight-alpha source-over

        foreach (GlMarker marker in _glMarkers)
        {
            if (!camera.TryNormalizedUvToScreen(marker.U, marker.V, width, height, out double sx, out double sy))
                continue; // behind the camera

            float cx = (float)((sx / width * 2.0) - 1.0);
            float cy = (float)(1.0 - (sy / height * 2.0));
            float halfW = marker.Width / (float)width;
            float halfH = marker.Height / (float)height;

            // Two triangles as a strip: top-left, bottom-left, top-right, bottom-right.
            SetQuadVertex(0, cx - halfW, cy + halfH, 0f, 0f);
            SetQuadVertex(1, cx - halfW, cy - halfH, 0f, 1f);
            SetQuadVertex(2, cx + halfW, cy + halfH, 1f, 0f);
            SetQuadVertex(3, cx + halfW, cy - halfH, 1f, 1f);
            _markerQuadBuffer.Position(0);
            _markerQuadBuffer.Put(_markerQuad);
            _markerQuadBuffer.Position(0);
            _markerQuadUvBuffer.Position(0);
            _markerQuadUvBuffer.Put(_markerQuadUv);
            _markerQuadUvBuffer.Position(0);

            GLES20.GlBindTexture(GLES20.GlTexture2d, marker.TextureId);
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

    // glGetError returns ONE latched flag per call and flags persist until read: drain the state before an
    // operation that will be checked, or a stale error from unrelated earlier calls falsely condemns it.
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

    #endregion

    #region Input (UI thread)

    // Platform recognizers handle second-pointer transitions and tap-vs-drag disambiguation.
    private sealed class PanGestureListener : GestureDetector.SimpleOnGestureListener
    {
        private readonly PanoramicSurface _owner;

        public PanGestureListener(PanoramicSurface owner) => _owner = owner;

        public override bool OnDown(MotionEvent e) => true;

        public override bool OnSingleTapUp(MotionEvent e)
        {
            _owner.SurfaceTapped?.Invoke(e.GetX(), e.GetY());
            return true;
        }

        public override bool OnScroll(MotionEvent? e1, MotionEvent e2, float distanceX, float distanceY)
        {
            if (_owner._scaleDetector.IsInProgress)
                return false;

            // distance* are previous-minus-current. Same grab feel as the Windows heads:
            // drag right pans the view left; drag down looks up.
            float scale = DragRotationScale(_owner._fieldOfView, _owner._viewportHeight);
            _owner.Yaw += distanceX * scale;
            _owner.Pitch = Math.Clamp(_owner._pitch + (distanceY * scale), MinPitch, MaxPitch);
            _owner.RequestRender();
            return true;
        }
    }

    private sealed class PinchListener : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private readonly PanoramicSurface _owner;

        public PinchListener(PanoramicSurface owner) => _owner = owner;

        public override bool OnScale(ScaleGestureDetector detector)
        {
            // Pinch out (factor > 1) zooms in = narrower field of view (same as the WinUI pinch).
            _owner.FieldOfView = Math.Clamp(_owner._fieldOfView / detector.ScaleFactor, MinFieldOfView, MaxFieldOfView);
            _owner.RequestRender();
            return true;
        }
    }

    #endregion

    // Same nested name and shape as the Windows PanoramicSurface.MarkerSwatch so the display code is shared.
    internal readonly record struct MarkerSwatch(float U, float V, byte[] Bgra, int Width, int Height);

    private readonly record struct GlMarker(int TextureId, float U, float V, int Width, int Height);
}
#endif
