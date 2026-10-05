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
internal sealed class PanoramicSurface : TextureView, TextureView.ISurfaceTextureListener, Choreographer.IFrameCallback, ViewTreeObserver.IOnTouchModeChangeListener
{
    private const float FocusRingDips = 3f;

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

    // Cross-thread state. The UI thread replaces the immutable camera reference atomically; each draw captures it once.
    private PanoramaCameraState _camera = PanoramaCameraState.Initial;
    private float _clearR = 0.02f;
    private float _clearG = 0.02f;
    private float _clearB = 0.02f;
    private float _clearA = 1f;
    private volatile bool _surfaceReady;
    private volatile bool _hasTexture;

    // Drawn while the view has keyboard focus, in the theme's accent color.
    private volatile bool _showFocusRing;
    private readonly float _focusRingR;
    private readonly float _focusRingG;
    private readonly float _focusRingB;
    private int _viewportWidth;
    private int _viewportHeight;

    // Pending content, applied by the render thread once the context exists (consume-once; the surface keeps no
    // CPU copy after upload - on a real context loss DeviceRecreated asks the display to re-decode).
    private readonly object _pendingLock = new();
    private Bitmap? _pendingBitmap;
    private PanoramaMarker[]? _pendingMarkers;

    private readonly GestureDetector _gestureDetector;
    private readonly ScaleGestureDetector _scaleDetector;

    // Held navigation keys by key code, so a key-up clears exactly what its key-down set. The frame callback steps
    // the camera while any is held; the frame time is 0 until its first frame.
    private readonly Dictionary<Keycode, NavigationKeys> _heldKeys = new();
    private bool _keyboardFramePosted;
    private long _keyboardFrameTime;

    private float _density;

    public PanoramicSurface(Context context)
        : base(context)
    {
        SurfaceTextureListener = this;
        // With long press on, a finger that rests before dragging never pans.
        _gestureDetector = new GestureDetector(context, new PanGestureListener(this)) { IsLongpressEnabled = false };
        _scaleDetector = new ScaleGestureDetector(context, new PinchListener(this));
        // Focusable in touch mode, so a touch can give it keyboard focus as on the other heads.
        Focusable = true;
        FocusableInTouchMode = true;
        _density = context.Resources?.DisplayMetrics?.Density ?? 0f;
        (_focusRingR, _focusRingG, _focusRingB) = ResolveAccentColor(context);
    }

    // Same event surface as the Windows PanoramicSurface, so the display's contract layer stays shared.
    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    public event Action? DeviceLost;

    public event Action? DeviceRecreated;

    public event Action? ViewChanged;

    // Raised when the pixels per DIP change, so the display can rasterize its markers again.
    public event Action? ScaleChanged;

    public PanoramaCameraState Camera
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

    // In DIPs, like tap positions.
    public double ActualWidth => Width / PixelsPerDip;

    public double ActualHeight => Height / PixelsPerDip;

    // The display rasterizes markers at this scale.
    public double PixelsPerDip => _density > 0 ? _density : 1;

    protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
    {
        base.OnSizeChanged(w, h, oldw, oldh);
        ViewChanged?.Invoke();
    }

    // The activity handles density changes, so this view lives through them. A detached view misses them, so attaching
    // checks too.
    protected override void OnConfigurationChanged(Android.Content.Res.Configuration? newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        CheckDensity();
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        CheckDensity();
        ViewTreeObserver?.AddOnTouchModeChangeListener(this);
        UpdateFocusRing();
    }

    protected override void OnDetachedFromWindow()
    {
        ViewTreeObserver?.RemoveOnTouchModeChangeListener(this);
        base.OnDetachedFromWindow();
    }

    // The view keeps focus in touch mode, but the ring is only for keyboard focus.
    void ViewTreeObserver.IOnTouchModeChangeListener.OnTouchModeChanged(bool isInTouchMode) => UpdateFocusRing();

    private void CheckDensity()
    {
        float density = Resources?.DisplayMetrics?.Density ?? 0f;
        if (density <= 0f || density == _density)
            return;

        _density = density;
        ScaleChanged?.Invoke();
    }

    // Takes ownership of the frame's bitmap (recycled after upload or when superseded). If it still exceeds the GL max
    // texture size it is downscaled once at upload.
    public void SetTexture(PanoramaFrame frame)
    {
        lock (_pendingLock)
        {
            _pendingBitmap?.Recycle();
            _pendingBitmap = frame.Bitmap;
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

    public void SetMarkers(IReadOnlyList<PanoramaMarker> markers)
    {
        PanoramaMarker[] copy = markers.ToArray();
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

        // Keyboard input goes to the panorama the user last touched.
        if (e.ActionMasked == MotionEventActions.Down && !IsFocused)
            RequestFocus();

        _scaleDetector.OnTouchEvent(e);
        _gestureDetector.OnTouchEvent(e);
        return true;
    }

    public override PointerIcon? OnResolvePointerIcon(MotionEvent? e, int pointerIndex)
    {
        if (e is null || !e.IsFromSource(InputSourceType.Mouse) || Context is not Context context)
            return base.OnResolvePointerIcon(e, pointerIndex);

        return PointerIcon.GetSystemIcon(context, e.IsButtonPressed(MotionEventButtonState.Primary) ? PointerIconType.Grabbing : PointerIconType.Grab);
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
                Camera = Camera.ZoomWheel(notches);
                RequestRender();
                return true;
            }
        }

        return base.OnGenericMotionEvent(e);
    }

    // Hardware-keyboard navigation, matching the SDK MapView on Android: held arrows move the view toward their side,
    // and whichever keys type "+" and "-" zoom. The camera steps once per frame while keys are held, so they combine.
    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        // Android's app quality guidelines have Esc release keyboard focus.
        if (keyCode == Keycode.Escape)
        {
            ClearFocus();
            return true;
        }

        NavigationKeys key = GetNavigationKey(keyCode, e);
        if (key == NavigationKeys.None)
            return base.OnKeyDown(keyCode, e);

        _heldKeys[keyCode] = key;
        if (!_keyboardFramePosted)
        {
            _keyboardFrameTime = 0;
            PostKeyboardFrame();
        }

        return true;
    }

    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e) => _heldKeys.Remove(keyCode) || base.OnKeyUp(keyCode, e);

    protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, Android.Graphics.Rect? previouslyFocusedRect)
    {
        base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);

        // Key-ups after focus leaves never arrive here.
        if (!gainFocus)
            _heldKeys.Clear();

        UpdateFocusRing();
    }

    // A TextureView gets no default focus highlight, so the frame draws one while the view has keyboard focus.
    private void UpdateFocusRing()
    {
        bool show = IsFocused && !IsInTouchMode;
        if (show == _showFocusRing)
            return;

        _showFocusRing = show;
        RequestRender();
    }

    private static (float R, float G, float B) ResolveAccentColor(Context context)
    {
        int argb = unchecked((int)0xFF1A73E8); // Material blue, for a theme without an accent
        using var value = new Android.Util.TypedValue();
        if (context.Theme?.ResolveAttribute(Android.Resource.Attribute.ColorAccent, value, true) == true)
        {
            if (value.Type >= Android.Util.DataType.FirstColorInt && value.Type <= Android.Util.DataType.LastColorInt)
                argb = value.Data;
            else if (value.ResourceId != 0 && context.Resources?.GetColorStateList(value.ResourceId, context.Theme) is { } colors)
                argb = colors.DefaultColor;
        }

        return (((argb >> 16) & 255) / 255f, ((argb >> 8) & 255) / 255f, (argb & 255) / 255f);
    }

    // As in the SDK MapView, arrows count only without modifiers, and the keys that type "+" and "-" zoom with any.
    private static NavigationKeys GetNavigationKey(Keycode keyCode, KeyEvent? e)
    {
        bool plain = e?.HasNoModifiers ?? true;
        return keyCode switch
        {
            Keycode.DpadLeft or Keycode.SystemNavigationLeft when plain => NavigationKeys.Left,
            Keycode.DpadRight or Keycode.SystemNavigationRight when plain => NavigationKeys.Right,
            Keycode.DpadUp or Keycode.SystemNavigationUp when plain => NavigationKeys.Up,
            Keycode.DpadDown or Keycode.SystemNavigationDown when plain => NavigationKeys.Down,
            _ => (char)(e?.UnicodeChar ?? 0) switch
            {
                '+' => NavigationKeys.ZoomIn,
                '-' => NavigationKeys.ZoomOut,
                _ => NavigationKeys.None,
            },
        };
    }

    private void PostKeyboardFrame()
    {
        Choreographer? choreographer = Choreographer.Instance;
        _keyboardFramePosted = choreographer is not null;
        choreographer?.PostFrameCallback(this);
    }

    // One frame of held-key navigation, reposted until every navigation key is released. The first frame only
    // records its time, since a step spans the time between frames.
    public void DoFrame(long frameTimeNanos)
    {
        NavigationKeys keys = NavigationKeys.None;
        foreach (NavigationKeys key in _heldKeys.Values)
            keys |= key;

        if (keys == NavigationKeys.None)
        {
            _keyboardFramePosted = false;
            return;
        }

        if (_keyboardFrameTime != 0)
        {
            double seconds = Math.Min((frameTimeNanos - _keyboardFrameTime) / 1e9, MaxKeyboardStepSeconds);
            Camera = Camera.Navigate(keys, seconds, ActualHeight);
            RequestRender();
        }

        _keyboardFrameTime = frameTimeNanos;
        PostKeyboardFrame();
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
        Post(() => DeviceLost?.Invoke());
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

        PanoramaMarker[]? markers;
        lock (_pendingLock)
        {
            markers = _pendingMarkers;
            _pendingMarkers = null;
        }

        if (markers is null)
            return;

        DeleteMarkerTextures();
        DrainGlErrors(); // the per-swatch checks must only see their own upload's errors
        foreach (PanoramaMarker marker in markers)
        {
            // A marker that can't be uploaded is skipped. It never fails the panorama.
            if (!marker.IsValid)
                continue;

            // Swatches arrive as BGRA (RuntimeImage raw buffer); GLES2 has no BGRA format without an
            // extension, so swap to RGBA on the CPU - swatches are tiny.
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
            if (!uploaded)
            {
                GLES20.GlDeleteTextures(1, new[] { textureId }, 0);
                continue;
            }

            _glMarkers.Add((textureId, marker));
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
        foreach ((int textureId, _) in _glMarkers)
            GLES20.GlDeleteTextures(1, new[] { textureId }, 0);

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
            _owner.SurfaceTapped?.Invoke(e.GetX() / _owner.PixelsPerDip, e.GetY() / _owner.PixelsPerDip);
            return true;
        }

        public override bool OnScroll(MotionEvent? e1, MotionEvent e2, float distanceX, float distanceY)
        {
            if (_owner._scaleDetector.IsInProgress)
                return false;

            // distance* are previous-minus-current. Same grab feel as the Windows heads:
            // drag right pans the view left; drag down looks up.
            _owner.Camera = _owner.Camera.Drag(-distanceX, -distanceY, _owner._viewportHeight);
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
            _owner.Camera = _owner.Camera.Zoom(detector.ScaleFactor);
            _owner.RequestRender();
            return true;
        }
    }

    #endregion
}

// A decoded panorama: the (possibly downsampled) bitmap plus the image's full-resolution oriented dimensions.
internal readonly record struct PanoramaFrame(Android.Graphics.Bitmap Bitmap, int Width, int Height) : IDisposable
{
    // Releases a frame that is never shown now rather than via finalizers; full-size bitmaps add up during rapid paging.
    public void Dispose() => Bitmap.Recycle();

    // Power-of-two downsample to the device budget (4096 low-RAM, else 8192) for the GPU texture only. Width and Height
    // stay the full-resolution oriented dimensions, the pixel space that markers and taps use. Throws when it can't
    // decode the data, such as a TIFF.
    internal static Task<PanoramaFrame> DecodeAsync(Uri uri, CancellationToken token)
    {
        return Task.Run(
            async () =>
            {
                (string? path, byte[]? downloaded) = await PanoramaImageFetcher.FetchAsync(uri, token).ConfigureAwait(false);
                ExifOrientationTransform orientation;
                if (path is not null)
                    orientation = ExifOrientationTransform.Read(uri);
                else
                {
                    using var metadataStream = new MemoryStream(downloaded!, writable: false);
                    orientation = ExifOrientationTransform.Read(metadataStream);
                }

                var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
                if (path is not null)
                    Android.Graphics.BitmapFactory.DecodeFile(path, bounds);
                else
                    Android.Graphics.BitmapFactory.DecodeByteArray(downloaded, 0, downloaded!.Length, bounds);

                int width = bounds.OutWidth;
                int height = bounds.OutHeight;
                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("The image could not be decoded.");

                bool lowRam = (Android.App.Application.Context.GetSystemService(Android.Content.Context.ActivityService)
                    as Android.App.ActivityManager)?.IsLowRamDevice == true;
                int budget = lowRam ? 4096 : 8192;
                int sample = 1;
                while (Math.Max(width, height) / sample > budget)
                    sample *= 2;

                var options = new Android.Graphics.BitmapFactory.Options
                {
                    InSampleSize = sample,
                    InPreferredConfig = Android.Graphics.Bitmap.Config.Argb8888,
                };
                Android.Graphics.Bitmap bitmap = (path is not null
                    ? Android.Graphics.BitmapFactory.DecodeFile(path, options)
                    : Android.Graphics.BitmapFactory.DecodeByteArray(downloaded, 0, downloaded!.Length, options))
                    ?? throw new InvalidDataException("The image could not be decoded.");

                try
                {
                    token.ThrowIfCancellationRequested();
                    if (orientation.IsMirrored || orientation.RotationDegrees != 0)
                    {
                        // Rotate, then reflect (see ExifOrientationTransform.RotationDegrees).
                        using var transform = new Android.Graphics.Matrix();
                        transform.SetRotate((float)orientation.RotationDegrees);
                        if (orientation.IsMirrored)
                            transform.PostScale(-1, 1);
                        Android.Graphics.Bitmap oriented = Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, transform, false)
                            ?? throw new InvalidOperationException("Unable to apply the image orientation.");
                        if (!ReferenceEquals(oriented, bitmap))
                            bitmap.Recycle();
                        bitmap = oriented;
                    }

                    return new PanoramaFrame(bitmap, orientation.SwapsDimensions ? height : width, orientation.SwapsDimensions ? width : height);
                }
                catch
                {
                    bitmap.Recycle();
                    throw;
                }
            },
            token);
    }
}
#endif
