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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using PointF = System.Drawing.PointF;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if WINDOWS_XAML || (MAUI && WINDOWS)
using System.Runtime.InteropServices.WindowsRuntime;
#endif
#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// Panoramic (equirectangular 360) inner display: decodes the image to a texture on the platform PanoramicSurface
// and surfaces taps. All screen<->pixel math goes through PanoramaCameraState.
internal sealed partial class OrientedImagePanoramicDisplay : OrientedImageInnerDisplay
{
    private const double MarkerHitTolerance = 12d;

#if MAUI
    private readonly PanoramicSurfaceView _surface;
#else
    private readonly PanoramicSurface _surface;
#endif
    private readonly List<ResolvedMarker> _resolvedMarkers = [];
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

    // After a device-lost rebuild the GPU texture and markers are gone (the surface keeps no CPU copy): re-decode and
    // re-supply them. The camera lives on the surface and survives the rebuild.
    private async void OnDeviceRecreated()
    {
        OrientedImage? image = Footprint?.OrientedImage;
        CancellationToken token = SessionToken;
        if (image is null || token.IsCancellationRequested)
            return; // nothing loaded, or an in-flight load will upload once it completes

        // The rebuilt surface is blank until re-supplied: invalidate the dimensions so a tap isn't reported against the
        // old pixel space, and report busy/non-interactive so bound commands don't stay enabled over a blank panorama.
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
            // Safe when superseded: UpdateState reads only the current session's state.
            _recovering = false;
            UpdateState();
        }
    }

    // Every marker change re-resolves the whole set; the surface redraws all markers from the result anyway.
    protected override void RebuildMarkers() => _ = ResolveMarkersAsync();

    protected override void AddMarkers(IEnumerable<OrientedImageMarker> newMarkers) => _ = ResolveMarkersAsync();

    protected override void ReplaceMarker(OrientedImageMarker oldMarker, OrientedImageMarker newMarker, int index) => _ = ResolveMarkersAsync();

    protected override void RemoveMarkers(int startingIndex, IEnumerable<OrientedImageMarker> removedMarkers) => _ = ResolveMarkersAsync();

    protected override void MoveMarkers(int oldIndex, int newIndex) => _ = ResolveMarkersAsync();

    // Dispatch so the snapshot of the app-owned marker is taken on the UI thread.
    protected override void OnMarkerChanged(OrientedImageMarker marker, string? propertyName) => this.Dispatch(() => _ = ResolveMarkersAsync());

    // Resolves every visible marker to a normalized (u,v) plus a rasterized swatch and pushes the set to the surface.
    // Runs off the UI thread; only the final apply marshals back.
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

        // A location at or behind the camera can project non-finite; keep NaN/Infinity out of the marker pipeline.
        if (!float.IsFinite(pixel.X) || !float.IsFinite(pixel.Y))
            return null;

        return (pixel.X / imageWidth, pixel.Y / imageHeight);
    }

    // Rasterizes a symbol to a tightly-packed BGRA8 swatch via RuntimeImage.
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
        // Swatches rasterize in physical pixels to match the GL viewport (Android) or the composition-scaled back buffer
        // (Windows). MainDisplayInfo.Density approximates the panel's per-monitor CompositionScale.
        double density = Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
        return density > 0 ? density : 1.0;
    }
#else
    private double GetScaleFactor()
    {
#if WINDOWS_XAML
        // CompositionScale sizes the back buffer, so swatches rasterized at it match the viewport; fall back before composition.
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

    private void OnCameraChanged() => UpdateFootprint();

    // Core derives the 360 ground footprint from the camera orientation and the view's angular extent.
    protected override Task? BeginFootprintUpdate(OrientedImageFootprint footprint)
    {
        double width = _surface.ActualWidth;
        double height = _surface.ActualHeight;
        if (_imageWidth <= 0 || _imageHeight <= 0)
            return null;

        var camera = new PanoramaCameraState(_surface.Yaw, _surface.Pitch, _surface.FieldOfView);
        if (!camera.TryGetFootprintView(width, height, out PanoramaCameraState.FootprintView view))
            return null;

        return footprint.UpdateFootprintAsync(view.Yaw, view.Pitch, view.HorizontalFieldOfView, view.VerticalFieldOfView, NextFootprintUpdateToken());
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

        // Look north initially (JS viewer parity): the center column faces CameraHeading but the identity camera centers
        // u = 0.75, so re-anchor by -pi/2 before subtracting the heading.
        _surface.Yaw = (-MathF.PI / 2f) - ReadHeadingRadians(image);
        _surface.Pitch = 0f;
        _surface.RequestRender();

        // Push the footprint explicitly: the push made when auto-update was enabled ran with zero dimensions, and camera
        // assignments that don't change the values raise no CameraChanged.
        UpdateFootprint();
    }

    protected override void ClearPresentation()
    {
        _surface.ClearTexture();
        _imageWidth = 0;
        _imageHeight = 0;

        // Bump the generation so an in-flight resolve can't apply stale markers to the next texture.
        Interlocked.Increment(ref _markerGeneration);
        _resolvedMarkers.Clear();
        _surface.SetMarkers(Array.Empty<PanoramicSurface.MarkerSwatch>());

        _surface.RequestRender();
    }

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
                // Open shared (StorageFile has no share mode): the SDK owns the downloaded file.
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
                    // Open shared (a Uri-based decoder can't): the SDK owns the downloaded file. OnLoad reads it fully.
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
    // Power-of-two downsample to the device budget (4096 low-RAM, else 8192) for the GPU texture only; Width/Height
    // stay the ORIGINAL dimensions because markers and taps work in source pixel space.
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
    // The decoded (possibly downsampled) bitmap plus the ORIGINAL pixel dimensions of the source image.
    private readonly record struct PanoramaFrame(Android.Graphics.Bitmap Bitmap, int Width, int Height);

    private void ApplyTexture(PanoramaFrame frame) => _surface.SetTexture(frame.Bitmap);

    // Lost a generation race: recycle now rather than via finalizers; full-size bitmaps add up during rapid paging.
    private static void DiscardFrame(PanoramaFrame? frame) => frame?.Bitmap.Recycle();
#else
    // The decoded image as tightly-packed BGRA8 plus its pixel dimensions.
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
