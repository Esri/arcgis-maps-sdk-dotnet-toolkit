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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// Android surface for the panoramic viewport: a TextureView with its own EGL context on a dedicated render thread,
// the host the SDK GeoView uses. The context is created once and survives backgrounding; only the window surface
// follows the SurfaceTexture. Rendering is on demand. Mesh and camera come from PanoramaCameraState.
//
// This part has the contract with the viewport, the view lifecycle, and the render thread. The Input part handles
// touch, mouse, and keyboard on the UI thread. The Rendering part owns EGL and the GL scene on the render thread.
internal sealed partial class PanoramicSurface : TextureView
{
    // All EGL and GL work runs on this serial queue. Disposal is gated by an interlocked flag rather than a lock.
    private readonly BlockingCollection<Action> _renderQueue = new();
    private Thread? _renderThread;
    private int _renderQueued;
    private int _disposed;

    // Cross-thread state. The UI thread replaces the immutable camera reference atomically; each draw captures it once.
    private PanoramaCameraState _camera = PanoramaCameraState.Initial;
    private float _clearR = 0.02f;
    private float _clearG = 0.02f;
    private float _clearB = 0.02f;
    private float _clearA = 1f;
    private volatile bool _surfaceReady;
    private volatile bool _hasTexture;
    private int _viewportWidth;
    private int _viewportHeight;
    private float _density;

    // Drawn while the view has keyboard focus, in the theme's accent color.
    private volatile bool _showFocusRing;
    private readonly float _focusRingR;
    private readonly float _focusRingG;
    private readonly float _focusRingB;

    // Pending content, applied by the render thread once the context exists and then dropped. The surface keeps no
    // CPU copy after upload, so after a context loss DeviceRecreated asks the panoramic viewport to decode again.
    private readonly object _pendingLock = new();
    private Bitmap? _pendingBitmap;
    private PanoramaMarker[]? _pendingMarkers;

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

    // The same events as the Windows and Apple surfaces, so the panoramic viewport code is shared.
    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    public event Action? DeviceLost;

    public event Action? DeviceRecreated;

    public event Action? ViewChanged;

    // Raised when the pixels per DIP change, so the panoramic viewport can rasterize its markers again.
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

    // The panoramic viewport rasterizes markers at this scale.
    public double PixelsPerDip => _density > 0 ? _density : 1;

    protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
    {
        base.OnSizeChanged(w, h, oldw, oldh);
        ViewChanged?.Invoke();
    }

    // The activity handles density changes, so this view lives through them.
    // A detached view misses them, so attaching checks too.
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

    // Off the window, key releases never arrive.
    protected override void OnDetachedFromWindow()
    {
        ViewTreeObserver?.RemoveOnTouchModeChangeListener(this);
        StopKeyboardNavigation();
        base.OnDetachedFromWindow();
    }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            StopKeyboardNavigation();
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

    // Queues work for the render thread, or returns false once the view is disposed.
    private bool PostToRenderThread(Action action)
    {
        if (Volatile.Read(ref _disposed) != 0)
            return false;

        EnsureRenderThread();
        try
        {
            _renderQueue.Add(action);
            return true;
        }
        catch (InvalidOperationException)
        {
            // Completed during teardown; nothing left to do.
            return false;
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

    // The panoramic viewport reports render-thread failures as Error, so they are raised on the UI thread.
    private void ReportRenderFailure(Exception ex) => Post(() => RenderFailed?.Invoke(ex));
}

// A decoded panorama: the (possibly downsampled) bitmap plus the image's full-resolution oriented dimensions.
internal readonly record struct PanoramaFrame(Android.Graphics.Bitmap Bitmap, int Width, int Height) : IDisposable
{
    // Releases a frame that is never shown now, since full-size bitmaps add up during rapid paging.
    public void Dispose() => Bitmap.Recycle();

    // Power-of-two downsample to the device budget (4096 low-RAM, else 8192) for the GPU texture only. Width and Height
    // stay the full-resolution oriented dimensions, the pixel space that markers and taps use. Throws when it can't
    // decode the file, such as a TIFF.
    internal static Task<PanoramaFrame> DecodeAsync(string path, CancellationToken token)
    {
        return Task.Run(
            () =>
            {
                ExifOrientationTransform orientation = ExifOrientationTransform.Read(path);
                var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
                Android.Graphics.BitmapFactory.DecodeFile(path, bounds);

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
                Android.Graphics.Bitmap bitmap = Android.Graphics.BitmapFactory.DecodeFile(path, options)
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
