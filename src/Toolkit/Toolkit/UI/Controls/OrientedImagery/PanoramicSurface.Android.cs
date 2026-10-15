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

// Android surface for the panoramic viewport: a TextureView, as in the SDK GeoView, with its own EGL context on a
// dedicated render thread. The context survives backgrounding; only the window surface follows the SurfaceTexture.
internal sealed partial class PanoramicSurface : TextureView
{
    // All EGL and GL work runs on this serial queue.
    private readonly BlockingCollection<Action> _renderQueue = new();
    private Thread? _renderThread;
    private int _renderQueued;
    private int _disposed;

    // Cross-thread state. The UI thread swaps the immutable camera reference, and each draw reads it once.
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

    private volatile bool _showFocusRing;
    private readonly float _focusRingR;
    private readonly float _focusRingG;
    private readonly float _focusRingB;

    // Applied by the render thread once a surface exists. No CPU copy survives the texture upload: a context rebuilt
    // without the texture decodes the file again.
    private readonly object _pendingLock = new();
    private Bitmap? _pendingBitmap;
    private PanoramaMarker[] _markers = [];
    private bool _markersDirty;
    private string? _texturePath;

    public PanoramicSurface(Context context)
        : base(context)
    {
        SurfaceTextureListener = this;
        // With long press on, a finger that rests before dragging never pans.
        _gestureDetector = new GestureDetector(context, new PanGestureListener(this)) { IsLongpressEnabled = false };
        _scaleDetector = new ScaleGestureDetector(context, new PinchListener(this));
        Focusable = true;
        FocusableInTouchMode = true;
        _density = context.Resources?.DisplayMetrics?.Density ?? 0f;
        (_focusRingR, _focusRingG, _focusRingB) = ResolveAccentColor(context);
    }

    // The same events as the Windows and Apple surfaces, so the panoramic viewport code is shared.
    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    public event Action? ViewChanged;

    // Raised when PixelsPerDip changes, since the panoramic viewport rasterizes markers at that scale.
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

    // Takes ownership of the frame's bitmap, which is recycled after upload or when superseded.
    public void SetTexture(PanoramaFrame frame, string path)
    {
        _texturePath = path;
        lock (_pendingLock)
        {
            _pendingBitmap?.Recycle();
            _pendingBitmap = frame.Bitmap;
        }

        PostToRenderThread(ConsumePendingBitmap);
    }

    public void ClearTexture()
    {
        _texturePath = null;
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
        lock (_pendingLock)
        {
            _markers = markers.ToArray();
            _markersDirty = true;
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
                _markers = [];
            }

            bool stopped = true;
            if (_renderThread is not null)
            {
                _renderQueue.Add(TearDownEgl);
                _renderQueue.CompleteAdding();
                stopped = _renderThread.Join(2000);
            }

            // A failed join means the render thread is stuck in a driver call. The queue stays alive under it, and the
            // queued TearDownEgl cleans up late.
            if (stopped)
                _renderQueue.Dispose();

            _gestureDetector.Dispose();
            _scaleDetector.Dispose();
        }

        base.Dispose(disposing);
    }

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
            // Dispose completed the queue after the check above.
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

    // Raised on the UI thread, since the panoramic viewport reports it as Error.
    private void ReportRenderFailure(Exception ex) => Post(() => RenderFailed?.Invoke(ex));

    // Decodes the file behind the texture again for a context rebuilt without it. Runs on the UI thread, which owns
    // the path like SetTexture does.
    private async Task RestoreTextureAsync()
    {
        if (_texturePath is not string path)
            return;

        try
        {
            PanoramaFrame frame = await PanoramaFrame.DecodeAsync(path, CancellationToken.None);
            if (path == _texturePath && Volatile.Read(ref _disposed) == 0)
                SetTexture(frame, path);
            else
                frame.Dispose();
        }
        catch (Exception ex)
        {
            if (path == _texturePath)
                RenderFailed?.Invoke(ex);
        }
    }
}

// Width and Height are the image's full-resolution oriented size, the pixel space of markers and taps. The bitmap may
// be downsampled to fit the device's memory budget.
internal readonly record struct PanoramaFrame(Android.Graphics.Bitmap Bitmap, int Width, int Height) : IDisposable
{
    // For a frame that is never shown, since full-size bitmaps add up during rapid paging.
    public void Dispose() => Bitmap.Recycle();

    // One decode at a time, so rapid paging never holds several full-size bitmaps.
    private static readonly SemaphoreSlim s_decodeGate = new(1, 1);

    // Throws for a format BitmapFactory can't decode, such as TIFF.
    internal static Task<PanoramaFrame> DecodeAsync(string path, CancellationToken token)
    {
        return Task.Run(
            async () =>
            {
                await s_decodeGate.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    return Decode(path, token);
                }
                finally
                {
                    s_decodeGate.Release();
                }
            },
            token);
    }

    private static PanoramaFrame Decode(string path, CancellationToken token)
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
        token.ThrowIfCancellationRequested();
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
    }
}
#endif
