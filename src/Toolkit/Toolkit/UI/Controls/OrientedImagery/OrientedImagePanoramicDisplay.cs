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

#if WPF || WINDOWS_XAML || __ANDROID__ || (MAUI && WINDOWS)
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if WPF
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
#elif WINDOWS_XAML
using System.Runtime.InteropServices.WindowsRuntime;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
#elif MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
#endif
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// Panoramic (360/equirectangular) inner display for OrientedImageDisplay.
// Supports the Windows heads (WPF + WinUI, hosting the shared Direct3D PanoramicSurface) and Android
// (hosting the GLES PanoramicSurface through the PanoramicSurfaceView handler); iOS/MacCatalyst pending.
// The shared OrientedImageInnerDisplay skeleton owns the load/session/state plumbing; this class decodes the
// image to a texture and surfaces clicks. All screen<->pixel math goes through the shared PanoramaCameraState.
internal sealed partial class OrientedImagePanoramicDisplay : OrientedImageInnerDisplay
{
    private const double MarkerHitTolerance = 12d;

#if MAUI
    private readonly PanoramicSurfaceView _surface;
#else
    private readonly PanoramicSurface _surface;
#endif
    private readonly List<ResolvedMarker> _resolvedMarkers = [];
    private readonly List<WeakEventListener<OrientedImagePanoramicDisplay, INotifyPropertyChanged, object?, PropertyChangedEventArgs>> _markerListeners = [];
    private int _markerGeneration;
    private int _imageWidth;
    private int _imageHeight;
    private bool _recovering;

    internal OrientedImagePanoramicDisplay()
    {
#if MAUI
        _surface = new PanoramicSurfaceView();
#else
        _surface = new PanoramicSurface();
#endif
        Content = _surface;
        _surface.SurfaceTapped += OnSurfaceTapped;
        _surface.RenderFailed += OnRenderFailed;
        _surface.DeviceRecreated += OnDeviceRecreated;
        UpdateAutomationName();
    }

#if MAUI
    protected override View AutomationNameTarget => _surface;
#elif WPF
    protected override System.Windows.DependencyObject AutomationNameTarget => _surface;
#else
    protected override Microsoft.UI.Xaml.DependencyObject AutomationNameTarget => _surface;
#endif

    // Interactive once a panorama is decoded and shown (the sphere is then navigable).
    protected override bool IsPresentationInteractive => _imageWidth > 0 && _imageHeight > 0;

    // A device-lost re-decode is presentation work: the surface is blank until it re-supplies.
    protected override bool IsPresentationBusy => _recovering;

    // Present-layer (device/bridge/render) failures happen outside the load path; surface them as Error.
    private void OnRenderFailed(Exception ex)
    {
        PresentationError = ex;
        UpdateState();
    }

    // After a device-lost rebuild the GPU texture + markers are gone (the surface does not keep a CPU copy, to avoid a
    // large idle duplicate). Re-decode the current image and re-supply texture + markers; the camera is preserved
    // because it lives on the surface and is untouched by the rebuild.
    private async void OnDeviceRecreated()
    {
        OrientedImage? image = Footprint?.OrientedImage;
        CancellationToken token = SessionToken;
        if (image is null || token.IsCancellationRequested)
            return; // nothing loaded, or an in-flight load will upload once it completes

        // The rebuilt surface has no texture yet (it blanks until re-supply). Invalidate dimensions so a click on the
        // blank surface isn't reported against the old pixel space, and surface the recovery immediately as
        // busy/non-interactive - commands bound to IsInteractive must not stay enabled over a blank panorama.
        _imageWidth = 0;
        _imageHeight = 0;
        _recovering = true;
        UpdateState();

        try
        {
            await image.RetryLoadAsync(); // idempotent; covers the image being unloaded/cancelled during teardown
            if (token.IsCancellationRequested || image.DataUri is not Uri uri)
                return;

            // A newer SetFootprint cancels the session token, aborting a now-pointless re-decode.
            PanoramaFrame? decoded = await DecodeAsync(uri, token);
            if (token.IsCancellationRequested)
            {
                DiscardFrame(decoded);
                return;
            }

            if (decoded is not PanoramaFrame frame)
                return;

            _imageWidth = frame.Width;
            _imageHeight = frame.Height;
            ApplyTexture(frame);
            _surface.RequestRender();
            _ = ResolveMarkersAsync();

            // Recovery succeeded: clear any error latched while the device was lost (the loss itself is recoverable).
            PresentationError = null;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer footprint; its own load re-supplies the surface.
        }
        catch (Exception ex)
        {
            // A footprint swapped in mid-recovery owns the display state now; don't overwrite it.
            if (!token.IsCancellationRequested)
                PresentationError = ex;
        }
        finally
        {
            // Recompute on every completion/cancellation/failure path (safe when superseded: UpdateState only
            // reads the current session's state).
            _recovering = false;
            UpdateState();
        }
    }

    // Re-subscribes to each current marker's PropertyChanged and re-resolves the whole set. Called when the collection
    // is replaced or changes; a marker's own property change re-resolves without re-subscribing.
    protected override void RebuildMarkers()
    {
        foreach (var listener in _markerListeners)
        {
            listener.Detach();
        }

        _markerListeners.Clear();
        AddMarkers(Markers ?? []);
    }

    protected override void AddMarkers(IEnumerable<OrientedImageMarker> newMarkers)
    {
        foreach (OrientedImageMarker marker in newMarkers)
        {
            SetMarkerListener(marker, out var listener);
            _markerListeners.Add(listener);
        }

        _ = ResolveMarkersAsync();
    }

    protected override void ReplaceMarker(OrientedImageMarker oldMarker, OrientedImageMarker newMarker, int index)
    {
        _markerListeners[index].Detach();
        SetMarkerListener(newMarker, out var listener);
        _markerListeners[index] = listener;

        _ = ResolveMarkersAsync();
    }

    protected override void RemoveMarkers(int startingIndex, IEnumerable<OrientedImageMarker> removedMarkers)
    {
        foreach (var marker in removedMarkers)
        {
            _markerListeners[startingIndex].Detach();
            _markerListeners.RemoveAt(startingIndex);
        }

        _ = ResolveMarkersAsync();
    }

    protected override void MoveMarkers(int oldIndex, int newIndex)
    {
        var temp = _markerListeners[oldIndex];
        _markerListeners[oldIndex] = _markerListeners[newIndex];
        _markerListeners[newIndex] = temp;

        _ = ResolveMarkersAsync();
    }

    private void SetMarkerListener(OrientedImageMarker marker, out WeakEventListener<OrientedImagePanoramicDisplay, INotifyPropertyChanged, object?, PropertyChangedEventArgs> listener)
    {
        listener = new WeakEventListener<OrientedImagePanoramicDisplay, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, marker)
        {
            OnEventAction = static (instance, source, eventArgs) => instance.OnMarkerPropertyChanged(source, eventArgs),
            OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
        };
        marker.PropertyChanged += listener.OnEvent;
    }

    // Dispatch so the snapshot of the app-owned marker is taken on the UI thread (the app may raise PropertyChanged off it).
    private void OnMarkerPropertyChanged(object? sender, PropertyChangedEventArgs e) => this.Dispatch(() => _ = ResolveMarkersAsync());

    // Resolves every visible marker to a normalized (u,v) and a rasterized swatch, then pushes the set to the surface.
    // Re-entrant: a generation counter discards a resolve superseded by a newer one. Resolution runs off the UI thread;
    // only the final apply (texture upload) marshals back.
    private async Task ResolveMarkersAsync()
    {
        int generation = Interlocked.Increment(ref _markerGeneration);
        CancellationToken token = SessionToken;
        OrientedImage? image = Footprint?.OrientedImage;
        int imageWidth = _imageWidth;
        int imageHeight = _imageHeight;

        // Snapshot the app-owned markers on the UI thread (Position/Symbol/IsVisible) before going async.
        var pending = new List<(OrientedImageMarker Marker, OrientedImageMarkerPosition Position, Symbol Symbol)>();
        if (Markers is not null && image is not null && imageWidth > 0 && imageHeight > 0)
        {
            foreach (OrientedImageMarker marker in Markers)
            {
                if (marker.IsVisible)
                {
                    pending.Add((marker, marker.Position, marker.Symbol ?? OrientedImageDisplay.DefaultMarkerSymbol));
                }
            }
        }

        double scale = GetScaleFactor();
        var resolved = new List<ResolvedMarker>(pending.Count);
        var swatches = new List<PanoramicSurface.MarkerSwatch>(pending.Count);
        foreach ((OrientedImageMarker marker, OrientedImageMarkerPosition position, Symbol symbol) in pending)
        {
            (float U, float V)? uv = await ResolveUvAsync(position, image!, imageWidth, imageHeight).ConfigureAwait(false);
            if (uv is not (float u, float v))
                continue;

            (byte[] Bgra, int Width, int Height)? swatch = await CreateSwatchAsync(symbol, scale).ConfigureAwait(false);
            if (swatch is not (byte[] bgra, int width, int height))
                continue;

            resolved.Add(new ResolvedMarker(marker, u, v));
            swatches.Add(new PanoramicSurface.MarkerSwatch(u, v, bgra, width, height));
        }

        this.Dispatch(() =>
        {
            // Discard if superseded (newer resolve) or if the session ended while resolving (stale image's markers).
            if (generation != _markerGeneration || token.IsCancellationRequested)
                return;

            _resolvedMarkers.Clear();
            _resolvedMarkers.AddRange(resolved);
            _surface.SetMarkers(swatches);
            _surface.RequestRender();
        });
    }

    // Image-anchored markers use their pixel directly; world-anchored markers project through the camera model.
    private static async Task<(float U, float V)?> ResolveUvAsync(OrientedImageMarkerPosition position, OrientedImage image, int imageWidth, int imageHeight)
    {
        PointF pixel;
        if (position.ImagePoint is PointF imagePoint)
        {
            pixel = imagePoint;
        }
        else if (position.Location is MapPoint location)
        {
            if (image.LoadStatus != LoadStatus.Loaded)
                return null;

            try
            {
                pixel = await image.LocationToImageAsync(location).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        // A location that doesn't project (e.g. at or behind the camera position) can come back non-finite; never
        // let NaN/Infinity into the marker pipeline (screen projection, GPU quad vertices, hit-test distances).
        if (!float.IsFinite(pixel.X) || !float.IsFinite(pixel.Y))
            return null;

        return (pixel.X / imageWidth, pixel.Y / imageHeight);
    }

    // Rasterizes a symbol to a tightly-packed BGRA8 swatch via RuntimeImage. The raw buffer is a publicly-visible,
    // exact-size MemoryStream, so it can be used without a copy; a Read fallback covers any future change to that.
    private static async Task<(byte[] Bgra, int Width, int Height)?> CreateSwatchAsync(Symbol symbol, double scale)
    {
        try
        {
            RuntimeImage? image = await symbol.CreateSwatchAsync(scale * 96).ConfigureAwait(false);
            if (image is null)
                return null;

            Stream raw = await image.GetRawBufferAsync().ConfigureAwait(false);
            byte[] bytes;
            if (raw is MemoryStream memory && memory.TryGetBuffer(out ArraySegment<byte> segment) &&
                segment.Array is byte[] array && segment.Offset == 0 && segment.Count == array.Length)
            {
                bytes = array; // GetRawBufferAsync returns a fresh, exact-size, publicly-visible buffer: own it directly.
            }
            else
            {
                using (raw)
                {
                    bytes = ReadAllBytes(raw);
                }
            }

            return (bytes, image.Width, image.Height);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream memory)
            return memory.ToArray();

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

#if MAUI
    private static double GetScaleFactor()
    {
        // Swatches are rasterized in physical pixels to match the physical-pixel GL viewport (Android) or the
        // composition-scaled back buffer (MAUI-Windows). On Windows the main display's density approximates the
        // panel's per-monitor CompositionScale that the WinUI head uses; exact parity would need a reach into
        // the platform view.
        double density = Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
        return density > 0 ? density : 1.0;
    }
#else
    private double GetScaleFactor()
    {
#if WINDOWS_XAML
        // Rasterize swatches at the SwapChainPanel's CompositionScale (the same factor that sizes the back buffer) so
        // marker pixels match the physical-pixel viewport. Fall back when the panel is not yet composed.
        float compositionScale = _surface.CompositionScaleX;
        if (compositionScale > 0)
            return compositionScale;

        return XamlRoot?.RasterizationScale ?? 1.0;
#else
        return System.Windows.PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
#endif
    }
#endif

    protected override void OnAutoUpdateFootprintChanged(bool enabled)
    {
        if (enabled)
            _surface.CameraChanged += OnCameraChanged;
        else
            _surface.CameraChanged -= OnCameraChanged;
    }

    private void OnCameraChanged() => UpdateFootprintCorners();

    protected override bool TryGetFootprintCorners(out IReadOnlyList<PointF> corners)
    {
        corners = Array.Empty<PointF>();
        double width = _surface.ActualWidth;
        double height = _surface.ActualHeight;
        if (_imageWidth <= 0 || _imageHeight <= 0 || width <= 0 || height <= 0)
            return false;

        var camera = new PanoramaCameraState(_surface.Yaw, _surface.Pitch, _surface.FieldOfView);
        return TryProjectViewRing(camera, width, height, out corners);
    }

    // Projects the screen-view boundary onto the equirectangular image as an ordered ring of pixel vertices,
    // densified (several samples per screen edge) because a panorama's straight screen edges map to curved arcs
    // on the image, which four corners approximate poorly. Horizontal coordinates are unwrapped across the
    // u = 0/1 seam so the ring stays continuous — pixels may fall outside [0, width], which core maps back onto
    // the sphere. Returns false when any sample can't be projected (e.g. looking past a pole).
    private bool TryProjectViewRing(PanoramaCameraState camera, double width, double height, out IReadOnlyList<PointF> ring)
    {
        ring = System.Array.Empty<PointF>();
        const int samplesPerEdge = 8;

        // Screen boundary sampled clockwise from the top-left corner, without repeating the shared corners.
        var boundary = new List<(double X, double Y)>(samplesPerEdge * 4);
        for (int i = 0; i < samplesPerEdge; i++)
            boundary.Add(((double)i / samplesPerEdge * width, 0));                    // top edge, left → right
        for (int i = 0; i < samplesPerEdge; i++)
            boundary.Add((width, (double)i / samplesPerEdge * height));               // right edge, top → bottom
        for (int i = 0; i < samplesPerEdge; i++)
            boundary.Add((width - ((double)i / samplesPerEdge * width), height));     // bottom edge, right → left
        for (int i = 0; i < samplesPerEdge; i++)
            boundary.Add((0, height - ((double)i / samplesPerEdge * height)));        // left edge, bottom → top

        var pixels = new List<PointF>(boundary.Count);
        double seamOffset = 0;
        float previousU = 0;
        for (int i = 0; i < boundary.Count; i++)
        {
            if (!camera.TryScreenToNormalizedUv(boundary[i].X, boundary[i].Y, width, height, out float u, out float v))
                return false;

            if (i != 0)
            {
                float delta = u - previousU;
                if (delta > 0.5f)
                    seamOffset -= 1.0;
                else if (delta < -0.5f)
                    seamOffset += 1.0;
            }

            previousU = u;
            pixels.Add(new PointF((float)((u + seamOffset) * _imageWidth), v * _imageHeight));
        }

        ring = pixels;
        return true;
    }

    public override void SetBackgroundColor(System.Drawing.Color color)
    {
        if (color.IsEmpty)
            _surface.SetClearColor(0.02f, 0.02f, 0.02f, 1f); // keep the renderer's default backdrop
        else
            _surface.SetClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

        _surface.RequestRender();
    }

    protected override async Task PresentAsync(OrientedImage image, Uri dataUri, CancellationToken token)
    {
        PanoramaFrame? decoded = await DecodeAsync(dataUri, token);
        if (token.IsCancellationRequested)
        {
            DiscardFrame(decoded); // never applied; release promptly rather than via finalizers
            return;
        }

        if (decoded is not PanoramaFrame frame)
        {
            ClearPresentation(); // decoded to nothing displayable
            return;
        }

        _imageWidth = frame.Width;
        _imageHeight = frame.Height;
        ApplyTexture(frame);

        // Orient the initial view to look NORTH (JS viewer parity). The image maps
        // azimuth = CameraHeading + (u - 0.5) * 2pi, i.e. the center column faces the camera heading, while the
        // identity camera centers u = 0.75 - so re-anchor by -pi/2 before subtracting the heading.
        _surface.Yaw = (-MathF.PI / 2f) - ReadHeadingRadians(image);
        _surface.Pitch = 0f;
        _surface.RequestRender();

        // Push the initial footprint explicitly: auto-update is usually enabled before this load completes
        // (image dimensions were still zero on its immediate push), and if the camera assignments above happen
        // not to change the values they raise no CameraChanged either.
        UpdateFootprintCorners();
    }

    // Blank the texture and invalidate dimensions NOW, so the user never sees the old panorama and clicks are
    // suppressed (zero dimensions) until a new texture is ready.
    protected override void ClearPresentation()
    {
        _surface.ClearTexture();
        _imageWidth = 0;
        _imageHeight = 0;

        // Drop the on-image markers immediately, and invalidate any in-flight resolve (generation bump), so stale
        // markers neither render on the next texture nor hit-test against it before a fresh resolve lands.
        Interlocked.Increment(ref _markerGeneration);
        _resolvedMarkers.Clear();
        _surface.SetMarkers(Array.Empty<PanoramicSurface.MarkerSwatch>());

        _surface.RequestRender();
    }

    // Re-resolve markers now that the image and its pixel dimensions are settled (world-anchored markers
    // reproject onto the new image; image-anchored markers re-scale to the new dimensions).
    protected override void OnPresentCompleted() => _ = ResolveMarkersAsync();

    private void OnSurfaceTapped(double x, double y)
    {
        if (Footprint?.OrientedImage is not OrientedImage image || _imageWidth <= 0 || _imageHeight <= 0)
            return;

        var camera = new PanoramaCameraState(_surface.Yaw, _surface.Pitch, _surface.FieldOfView);
        if (!camera.TryScreenToNormalizedUv(x, y, _surface.ActualWidth, _surface.ActualHeight, out float u, out float v))
            return;

        var pixel = new PointF(u * _imageWidth, v * _imageHeight);
        OrientedImageMarker? marker = HitTestMarker(camera, x, y);
        RaiseImageClicked(new OrientedImageDisplay.ImageClickedEventArgs(pixel, image, marker));
    }

    // Returns the nearest visible marker whose projected screen position is within the hit tolerance of the tap, or null.
    private OrientedImageMarker? HitTestMarker(PanoramaCameraState camera, double x, double y)
    {
        OrientedImageMarker? hit = null;
        double best = MarkerHitTolerance;
#if __ANDROID__
        best *= GetScaleFactor(); // Android taps and view sizes are physical pixels; the tolerance is DIP-defined
#endif
        foreach (ResolvedMarker resolved in _resolvedMarkers)
        {
            if (!camera.TryNormalizedUvToScreen(resolved.U, resolved.V, _surface.ActualWidth, _surface.ActualHeight, out double sx, out double sy))
                continue;

            double distance = Math.Sqrt(((sx - x) * (sx - x)) + ((sy - y) * (sy - y)));
            if (distance <= best)
            {
                best = distance;
                hit = resolved.Marker;
            }
        }

        return hit;
    }

    private static float ReadHeadingRadians(OrientedImage image)
    {
        if (image.Attributes.TryGetValue("CameraHeading", out object? raw) && raw is double degrees && !double.IsNaN(degrees))
            return (float)(degrees * Math.PI / 180.0);

        return 0f;
    }

#if WINDOWS_XAML || (MAUI && WINDOWS)
    private static async Task<PanoramaFrame?> DecodeAsync(Uri uri, CancellationToken token)
    {
        Windows.Storage.Streams.IRandomAccessStream? stream = null;
        try
        {
            if (uri.IsFile)
            {
                // Core keeps the image file open and StorageFile has no share-mode option, so open a FileShare.ReadWrite
                // FileStream and adapt it to the IRandomAccessStream BitmapDecoder needs.
                stream = new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).AsRandomAccessStream();
            }
            else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                using var httpClient = new System.Net.Http.HttpClient();
                byte[] bytes = await httpClient.GetByteArrayAsync(uri, token);
                var memory = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                await memory.WriteAsync(bytes.AsBuffer());
                memory.Seek(0);
                stream = memory;
            }

            if (stream is null)
                return null;

            Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            Windows.Graphics.Imaging.PixelDataProvider pixels = await decoder.GetPixelDataAsync(
                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Ignore,
                new Windows.Graphics.Imaging.BitmapTransform(),
                Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,
                Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
            return new PanoramaFrame(pixels.DetachPixelData(), (int)decoder.PixelWidth, (int)decoder.PixelHeight);
        }
        finally
        {
            stream?.Dispose();
        }
    }
#elif WPF
    private static Task<PanoramaFrame?> DecodeAsync(Uri uri, CancellationToken token)
    {
        return Task.Run(
            () =>
            {
                System.Windows.Media.Imaging.BitmapDecoder decoder;
                if (uri.IsFile)
                {
                    // Core keeps the image file open, so open it FileShare.ReadWrite (a Uri-based decoder can't) to avoid
                    // a sharing violation. OnLoad reads the image fully, so the stream can be disposed right after.
                    using var fileStream = new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(fileStream, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                }
                else
                {
                    decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(uri, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                }

                System.Windows.Media.Imaging.BitmapFrame frame = decoder.Frames[0];
                var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                int width = converted.PixelWidth;
                int height = converted.PixelHeight;
                int stride = width * 4;
                byte[] bytes = new byte[height * stride];
                converted.CopyPixels(bytes, stride, 0);
                return (PanoramaFrame?)new PanoramaFrame(bytes, width, height);
            },
            token);
    }
#elif __ANDROID__
    // Decodes with a power-of-two downsample so the longest side fits the device memory budget (isLowRamDevice:
    // 4096, else 8192). Width/Height report the ORIGINAL pixel dimensions - the SDK transforms, markers and clicks
    // all work in the original image pixel space; only the GPU texture is downsampled (uv is normalized, so the
    // sphere samples identically). A residual GL_MAX_TEXTURE_SIZE clamp happens at upload inside the surface.
    private static Task<PanoramaFrame?> DecodeAsync(Uri uri, CancellationToken token)
    {
        return Task.Run(
            async () =>
            {
                string? path = null;
                byte[]? downloaded = null;
                if (uri.IsFile)
                {
                    path = uri.LocalPath;
                }
                else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    downloaded = await httpClient.GetByteArrayAsync(uri, token).ConfigureAwait(false);
                }
                else
                {
                    return (PanoramaFrame?)null;
                }

                var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
                if (path is not null)
                    Android.Graphics.BitmapFactory.DecodeFile(path, bounds);
                else
                    Android.Graphics.BitmapFactory.DecodeByteArray(downloaded, 0, downloaded!.Length, bounds);

                int width = bounds.OutWidth;
                int height = bounds.OutHeight;
                if (width <= 0 || height <= 0)
                    return (PanoramaFrame?)null;

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
                Android.Graphics.Bitmap? bitmap = path is not null
                    ? Android.Graphics.BitmapFactory.DecodeFile(path, options)
                    : Android.Graphics.BitmapFactory.DecodeByteArray(downloaded, 0, downloaded!.Length, options);
                if (bitmap is null)
                    return (PanoramaFrame?)null;

                return (PanoramaFrame?)new PanoramaFrame(bitmap, width, height);
            },
            token);
    }
#endif

#if __ANDROID__
    // The decoded (possibly downsampled) panorama bitmap plus the ORIGINAL pixel dimensions of the source image.
    // This is also the seam where a future 360-video source would provide frames over time instead of a single decode.
    private readonly record struct PanoramaFrame(Android.Graphics.Bitmap Bitmap, int Width, int Height);

    private void ApplyTexture(PanoramaFrame frame) => _surface.SetTexture(frame.Bitmap);

    // A frame that lost a generation race is never applied; release its bitmap promptly instead of waiting
    // for finalizers (full-size panorama bitmaps add up fast during rapid paging).
    private static void DiscardFrame(PanoramaFrame? frame) => frame?.Bitmap.Recycle();
#else
    // The decoded equirectangular image as tightly-packed BGRA8 plus its pixel dimensions. This is also the seam
    // where a future 360-video source would provide frames over time instead of a single decode.
    private readonly record struct PanoramaFrame(byte[] Bgra, int Width, int Height);

    private void ApplyTexture(PanoramaFrame frame) => _surface.SetTexture(frame.Bgra, (uint)frame.Width, (uint)frame.Height);

    // byte[]-backed frames are plain managed memory; nothing to release eagerly.
    private static void DiscardFrame(PanoramaFrame? frame)
    {
    }
#endif

    // A marker resolved to a normalized (u,v), kept on the UI side for tap hit-testing (the surface owns the GPU side).
    private readonly record struct ResolvedMarker(OrientedImageMarker Marker, float U, float V);
}
#endif
