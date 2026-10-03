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

#if WPF || WINDOWS_XAML || __ANDROID__ || __IOS__ || (MAUI && WINDOWS)
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

    // Kept across passes. A marker's (u,v) depends on its position and the image, and its swatch on its symbol.
    // Failures are not kept.
    private readonly Dictionary<OrientedImageMarker, Uv> _markerUvs = [];
    private readonly Dictionary<OrientedImageMarker, Swatch> _markerSwatches = [];
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
        _surface.DeviceLost += OnDeviceLost;
        _surface.DeviceRecreated += OnDeviceRecreated;
        SetAutomationName(null);
    }

#if MAUI
    protected override View AutomationTarget => _surface;
#elif WPF
    protected override System.Windows.DependencyObject AutomationTarget => _surface;
#else
    protected override Microsoft.UI.Xaml.DependencyObject AutomationTarget => _surface;
#endif

    // Interactive once a panorama is decoded and shown, and not while recovering.
    protected override bool IsPresentationInteractive => _imageWidth > 0 && _imageHeight > 0 && !_recovering;

    // Device recovery is presentation work: the surface is blank from the loss until it is rebuilt and re-supplied.
    protected override bool IsPresentationBusy => _recovering;

    private void OnDeviceLost()
    {
        _recovering = true;
        UpdateState();
    }

    // Present-layer (device/bridge/render) failures happen outside the load path: blank the display and surface them
    // as Error. The blanking render can fail too; that is not reported again.
    private void OnRenderFailed(Exception ex)
    {
        if (PresentationError is not null)
            return;

        PresentationError = ex;
        _recovering = false;
        ClearPresentation();
        UpdateState();
    }

    // After a device-lost rebuild the GPU texture and markers are gone (the surface keeps no CPU copy): re-decode and
    // re-supply them. The camera lives on the surface and survives the rebuild.
    private async void OnDeviceRecreated()
    {
        // The rebuilt surface is blank until re-supplied: invalidate the dimensions so a tap isn't reported against the
        // old pixel space, and report busy/non-interactive so bound commands don't stay enabled over a blank panorama.
        _imageWidth = 0;
        _imageHeight = 0;
        _recovering = true;
        UpdateState();

        CancellationToken token = SessionToken;
        try
        {
            OrientedImage? image = Footprint?.OrientedImage;
            if (image is null || token.IsCancellationRequested)
                return; // nothing loaded, or an in-flight load will upload once it completes

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

    // The caches belong to the UI thread.
    protected override void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed) => this.Dispatch(() =>
    {
        foreach (OrientedImageMarker marker in removed)
        {
            _markerUvs.Remove(marker);
            _markerSwatches.Remove(marker);
        }

        _ = ResolveMarkersAsync();
    });

    // Drops the result the change invalidates and re-resolves. Tag is not drawn.
    protected override void OnMarkerChanged(OrientedImageMarker marker, string? propertyName) => this.Dispatch(() =>
    {
        switch (propertyName)
        {
            case nameof(OrientedImageMarker.Tag):
                return;
            case nameof(OrientedImageMarker.IsVisible):
                break;
            case nameof(OrientedImageMarker.Position):
                _markerUvs.Remove(marker);
                break;
            case nameof(OrientedImageMarker.Symbol):
                _markerSwatches.Remove(marker);
                break;
            default:
                _markerUvs.Remove(marker);
                _markerSwatches.Remove(marker);
                break;
        }

        _ = ResolveMarkersAsync();
    });

    // For tests. Bumped by each pass and by ClearPresentation.
    internal int MarkerGeneration => _markerGeneration;

    // Resolves every visible marker to a normalized (u,v) plus a rasterized swatch and pushes the set to the surface.
    // Cached results are reused, and the rest is computed off the UI thread. A superseded pass stops at its next check
    // and leaves the rest to the newer pass. Only the final apply marshals back.
    private async Task ResolveMarkersAsync()
    {
        int generation = Interlocked.Increment(ref _markerGeneration);
        CancellationToken token = SessionToken;
        OrientedImage? image = Footprint?.OrientedImage;
        int imageWidth = _imageWidth;
        int imageHeight = _imageHeight;

        // Snapshot the app-owned markers and the caches on the UI thread before going async. A swatch rasterized at
        // another display scale is a miss.
        double scale = GetScaleFactor();
        var pending = new List<PendingMarker>();
        if (image is not null && imageWidth > 0 && imageHeight > 0)
        {
            foreach (OrientedImageMarker marker in Markers)
            {
                if (!marker.IsVisible)
                    continue;

                Swatch? swatch = _markerSwatches.GetValueOrDefault(marker) is { } cached && cached.Scale == scale ? cached : null;
                pending.Add(new PendingMarker(marker, marker.Position, marker.Symbol ?? OrientedImageDisplay.DefaultMarkerSymbol, _markerUvs.GetValueOrDefault(marker), swatch));
            }
        }

        // Markers sharing a symbol share the swatch this pass rasterizes, never a cached one: a symbol change
        // invalidates its markers one at a time, so another marker's cache can be stale.
        var rasterized = new Dictionary<Symbol, Swatch>();
        var results = new List<(OrientedImageMarker Marker, Uv Uv, Swatch Swatch)>(pending.Count);
        foreach (PendingMarker item in pending)
        {
            Uv? uv = item.Uv ?? await ResolveUvAsync(item.Position, image!, imageWidth, imageHeight).ConfigureAwait(false);
            Swatch? swatch = item.Swatch ?? rasterized.GetValueOrDefault(item.Symbol);
            if (uv is not null && swatch is null)
            {
                swatch = await CreateSwatchAsync(item.Symbol, scale).ConfigureAwait(false);
                if (swatch is not null)
                    rasterized[item.Symbol] = swatch;
            }

            if (generation != Volatile.Read(ref _markerGeneration) || token.IsCancellationRequested)
                return;

            if (uv is not null && swatch is not null)
                results.Add((item.Marker, uv, swatch));
        }

        this.Dispatch(() =>
        {
            // Superseded, or the image changed meanwhile: the results are stale.
            if (generation != _markerGeneration || token.IsCancellationRequested)
                return;

            var swatches = new List<PanoramicSurface.MarkerSwatch>(results.Count);
            _resolvedMarkers.Clear();
            foreach ((OrientedImageMarker marker, Uv uv, Swatch swatch) in results)
            {
                _markerUvs[marker] = uv;
                _markerSwatches[marker] = swatch;
                _resolvedMarkers.Add(new ResolvedMarker(marker, uv.U, uv.V, swatch.OffsetX, swatch.OffsetY, swatch.Width / scale / 2, swatch.Height / scale / 2));
                swatches.Add(new PanoramicSurface.MarkerSwatch(uv.U, uv.V, swatch.Bgra, swatch.Width, swatch.Height, (float)(swatch.OffsetX * scale), (float)(swatch.OffsetY * scale)));
            }

            _surface.SetMarkers(swatches);
            _surface.RequestRender();
        });
    }

    // Image-anchored markers use their pixel directly, on their own image only; world-anchored markers project through
    // the camera model.
    private static async Task<Uv?> ResolveUvAsync(OrientedImageMarkerPosition position, OrientedImage image, int imageWidth, int imageHeight)
    {
        PointF pixel;
        if (position.ImagePoint is PointF imagePoint)
        {
            if (!ReferenceEquals(position.Image, image))
                return null;

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

        return new Uv(pixel.X / imageWidth, pixel.Y / imageHeight);
    }

    // Rasterizes a symbol to a tightly-packed BGRA8 swatch via RuntimeImage, with its offset.
    private static async Task<Swatch?> CreateSwatchAsync(Symbol symbol, double scale)
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

            (double offsetX, double offsetY) = GetMarkerOffset(symbol);
            return new Swatch(bytes, image.Width, image.Height, offsetX, offsetY, scale);
        }
        catch
        {
            return null;
        }
    }

    // Where a symbol draws relative to its anchor, in DIPs with y down. The map rotates a marker clockwise around its
    // anchor, so the offset turns with the angle; swatches include the angle but not the offset. Composite and
    // multilayer symbols carry per-layer offsets that one swatch cannot represent.
    internal static (double X, double Y) GetMarkerOffset(Symbol symbol)
    {
        if (symbol is not MarkerSymbol marker)
            return (0d, 0d);

        double angle = marker.Angle * Math.PI / 180d;
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return ((marker.OffsetX * cos) + (marker.OffsetY * sin), (marker.OffsetX * sin) - (marker.OffsetY * cos));
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
        // Swatches rasterize at the surface's pixel density, which on Windows approximates the monitor's scale.
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
            _surface.ViewChanged += OnViewChanged;
        else
            _surface.ViewChanged -= OnViewChanged;
    }

    private void OnViewChanged() => UpdateFootprint();

    // UpdateFootprintAsync derives the 360 ground footprint from the camera orientation and the view's angular extent.
    protected override Task? BeginFootprintUpdate(OrientedImageFootprint footprint)
    {
        double width = _surface.ActualWidth;
        double height = _surface.ActualHeight;
        if (_imageWidth <= 0 || _imageHeight <= 0)
            return null;

        if (!Camera.TryGetFootprintView(width, height, out PanoramaCameraState.FootprintView view))
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
        _recovering = false; // re-supplied, whether or not a rebuilt device asked; a later DeviceLost starts over

        // Each image opens facing north at the horizon, with a 90-degree vertical field of view. The identity camera
        // centers u = 0.75 and the center column faces CameraHeading, so yaw is -pi/2 minus the heading.
        _surface.Yaw = (-MathF.PI / 2f) - ReadHeadingRadians(image.Attributes);
        _surface.Pitch = 0f;
        _surface.FieldOfView = MathF.PI / 2f;
        _surface.RequestRender();

        // Push the footprint explicitly: the push made when auto-update was enabled ran with zero dimensions, and camera
        // assignments that don't change the values raise no ViewChanged.
        UpdateFootprint();
    }

    protected override void ClearPresentation()
    {
        _surface.ClearTexture();
        _imageWidth = 0;
        _imageHeight = 0;

        // Bump the generation so an in-flight resolve can't apply stale markers to the next texture. The (u,v)s go with
        // the image. The swatches stay.
        Interlocked.Increment(ref _markerGeneration);
        _markerUvs.Clear();
        _resolvedMarkers.Clear();
        _surface.SetMarkers(Array.Empty<PanoramicSurface.MarkerSwatch>());

        _surface.RequestRender();
    }

    protected override void OnPresentCompleted() => _ = ResolveMarkersAsync();

    private PanoramaCameraState Camera => new(_surface.Yaw, _surface.Pitch, _surface.FieldOfView);

    // Surface units per DIP: Android taps and view sizes are physical pixels.
    private double SurfaceUnitsPerDip =>
#if __ANDROID__
        GetScaleFactor();
#else
        1d;
#endif

    public override PointF? ScreenToImage(double x, double y) =>
        IsInteractive ? SurfaceToImage(x * SurfaceUnitsPerDip, y * SurfaceUnitsPerDip) : null;

    // The image coordinate under a point of the surface, in surface units.
    private PointF? SurfaceToImage(double x, double y) =>
        Camera.TryScreenToNormalizedUv(x, y, _surface.ActualWidth, _surface.ActualHeight, out float u, out float v)
            ? new PointF(u * _imageWidth, v * _imageHeight)
            : null;

    private void OnSurfaceTapped(double x, double y)
    {
        if (!IsInteractive || Footprint?.OrientedImage is not OrientedImage image || SurfaceToImage(x, y) is not PointF pixel)
            return;

        OrientedImageMarker? marker = HitTestMarker(_resolvedMarkers, Camera, _surface.ActualWidth, _surface.ActualHeight, x, y, SurfaceUnitsPerDip);
        RaiseImageTapped(new OrientedImageTappedEventArgs(pixel, image, marker));
    }

    // Returns the topmost marker whose swatch lies within the hit tolerance of the tap, or null, as the planar display's
    // identify does. Markers draw in list order, so the last one is on top. A swatch is centered on its projected anchor
    // plus its symbol offset. dip converts DIPs to view units; it is 1 where the view measures in DIPs.
    internal static OrientedImageMarker? HitTestMarker(IReadOnlyList<ResolvedMarker> markers, PanoramaCameraState camera,
        double viewWidth, double viewHeight, double x, double y, double dip)
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            ResolvedMarker resolved = markers[i];
            if (!camera.TryNormalizedUvToScreen(resolved.U, resolved.V, viewWidth, viewHeight, out double sx, out double sy))
                continue;

            // The tap's distance from the swatch's rectangle, in DIPs; zero inside it.
            double dx = Math.Max(Math.Abs(((x - sx) / dip) - resolved.OffsetX) - resolved.HalfWidth, 0);
            double dy = Math.Max(Math.Abs(((y - sy) / dip) - resolved.OffsetY) - resolved.HalfHeight, 0);
            if ((dx * dx) + (dy * dy) <= MarkerHitTolerance * MarkerHitTolerance)
                return resolved.Marker;
        }

        return null;
    }

    // CameraHeading is in degrees, and -999 means unknown. Values outside 0 to 360 wrap.
    internal static float ReadHeadingRadians(IDictionary<string, object?> attributes)
    {
        if (attributes.TryGetValue("CameraHeading", out object? raw) && raw is double degrees && !double.IsNaN(degrees) && degrees != -999)
            return (float)(degrees * Math.PI / 180.0);

        return 0f;
    }

#if WINDOWS_XAML || (MAUI && WINDOWS)
    internal static async Task<PanoramaFrame?> DecodeAsync(Uri uri, CancellationToken token)
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
            bool orientJpeg = decoder.DecoderInformation.CodecId == Windows.Graphics.Imaging.BitmapDecoder.JpegDecoderId;
            Windows.Graphics.Imaging.PixelDataProvider pixels = await decoder.GetPixelDataAsync(
                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Ignore,
                new Windows.Graphics.Imaging.BitmapTransform(),
                orientJpeg ? Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation : Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,
                Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
            return new PanoramaFrame(pixels.DetachPixelData(),
                (int)(orientJpeg ? decoder.OrientedPixelWidth : decoder.PixelWidth),
                (int)(orientJpeg ? decoder.OrientedPixelHeight : decoder.PixelHeight));
        }
        finally
        {
            stream?.Dispose();
        }
    }
#elif WPF
    internal static Task<PanoramaFrame?> DecodeAsync(Uri uri, CancellationToken token)
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
                var orientation = decoder is System.Windows.Media.Imaging.JpegBitmapDecoder &&
                    frame.Metadata is System.Windows.Media.Imaging.BitmapMetadata metadata &&
                    metadata.GetQuery("/app1/ifd/{ushort=274}") is ushort value
                    ? new ExifOrientationTransform(value) : default;
                System.Windows.Media.Imaging.BitmapSource source = frame;
                if (orientation.IsMirrored || orientation.RotationDegrees != 0)
                {
                    // Rotate, then reflect (see ExifOrientationTransform.RotationDegrees). Matrix.Rotate and Scale
                    // append, so the rotation applies first.
                    var transform = System.Windows.Media.Matrix.Identity;
                    transform.Rotate(orientation.RotationDegrees);
                    if (orientation.IsMirrored)
                        transform.Scale(-1, 1);
                    source = new System.Windows.Media.Imaging.TransformedBitmap(frame, new System.Windows.Media.MatrixTransform(transform));
                }

                token.ThrowIfCancellationRequested();
                var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap(source, System.Windows.Media.PixelFormats.Bgra32, null, 0);
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
    // Power-of-two downsample to the device budget (4096 low-RAM, else 8192) for the GPU texture only. Width and Height
    // stay the full-resolution oriented dimensions, the pixel space that markers and taps use.
    internal static Task<PanoramaFrame?> DecodeAsync(Uri uri, CancellationToken token)
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

                    return (PanoramaFrame?)new PanoramaFrame(bitmap, orientation.SwapsDimensions ? height : width, orientation.SwapsDimensions ? width : height);
                }
                catch
                {
                    bitmap.Recycle();
                    throw;
                }
            },
            token);
    }
#elif __IOS__
    // The longest texture side, 128 MB as BGRA8. Larger images decode at a power-of-two fraction of their size.
    private const int MaxTextureSize = 8192;

    // One decode at a time, so rapid paging never holds several full-size buffers.
    private static readonly SemaphoreSlim s_decodeGate = new(1, 1);

    // Decodes into a texture on the shared Metal device, applying a JPEG's EXIF orientation as the SDK does. Width and
    // Height are the full-size oriented dimensions, which markers and taps use. Throws when it can't decode the data.
    internal static Task<PanoramaFrame?> DecodeAsync(Uri uri, CancellationToken token)
    {
        return Task.Run(
            async () =>
            {
                // Waiting for the pipeline keeps the display busy until it can draw.
                PanoramicSurface.Pipeline pipeline = await PanoramicSurface.GetPipelineAsync().ConfigureAwait(false);
                ImageIO.CGImageSource? source;
                ExifOrientationTransform orientation;
                if (uri.IsFile)
                {
                    source = ImageIO.CGImageSource.FromUrl(Foundation.NSUrl.FromFilename(uri.LocalPath));
                    orientation = ExifOrientationTransform.Read(uri);
                }
                else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    byte[] downloaded = await httpClient.GetByteArrayAsync(uri, token).ConfigureAwait(false);
                    source = ImageIO.CGImageSource.FromData(Foundation.NSData.FromArray(downloaded));
                    using var metadataStream = new MemoryStream(downloaded, writable: false);
                    orientation = ExifOrientationTransform.Read(metadataStream);
                }
                else
                {
                    throw new NotSupportedException($"Images can't be read from '{uri.Scheme}' locations.");
                }

                using (source)
                {
                    await s_decodeGate.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        return (PanoramaFrame?)Decode(pipeline.Device, source, orientation, token);
                    }
                    finally
                    {
                        s_decodeGate.Release();
                    }
                }
            },
            token);
    }

    private static PanoramaFrame Decode(Metal.IMTLDevice device, ImageIO.CGImageSource? source, ExifOrientationTransform orientation, CancellationToken token)
    {
        CoreGraphics.CGImageProperties? properties = source?.ImageCount > 0 ? source.GetProperties(0, null) : null;
        int width = properties?.PixelWidth ?? 0;
        int height = properties?.PixelHeight ?? 0;
        if (source is null || width <= 0 || height <= 0)
            throw new InvalidDataException("The image could not be decoded.");

        int sample = 1;
        while (Math.Max(width, height) / sample > MaxTextureSize)
            sample *= 2;

        // Without caching, the full-size image decodes straight into the buffer it's drawn into.
        using CoreGraphics.CGImage image = (sample == 1
            ? source.CreateImage(0, new ImageIO.CGImageOptions { ShouldCache = false })
            : source.CreateThumbnail(0, new ImageIO.CGImageThumbnailOptions
            {
                CreateThumbnailFromImageAlways = true,
                MaxPixelSize = (int)Math.Ceiling(Math.Max(width, height) / (double)sample),
            }))
            ?? throw new InvalidDataException("The image could not be decoded.");
        token.ThrowIfCancellationRequested();

        Metal.IMTLTexture texture = DrawToTexture(device, image, orientation);
        return new PanoramaFrame(texture, orientation.SwapsDimensions ? height : width, orientation.SwapsDimensions ? width : height);
    }

    // Draws the image upright into a BGRA8 texture on the CPU, so it works in the background.
    private static Metal.IMTLTexture DrawToTexture(Metal.IMTLDevice device, CoreGraphics.CGImage image, ExifOrientationTransform orientation)
    {
        int storedWidth = (int)image.Width;
        int storedHeight = (int)image.Height;
        int width = orientation.SwapsDimensions ? storedHeight : storedWidth;
        int height = orientation.SwapsDimensions ? storedWidth : storedHeight;
        int bytesPerRow = width * 4;
        IntPtr pixels = System.Runtime.InteropServices.Marshal.AllocHGlobal((nint)bytesPerRow * height);
        try
        {
            using (CoreGraphics.CGColorSpace colorSpace = CoreGraphics.CGColorSpace.CreateDeviceRGB())
            using (var context = new CoreGraphics.CGBitmapContext(pixels, width, height, 8, bytesPerRow, colorSpace, CoreGraphics.CGBitmapFlags.ByteOrder32Little | CoreGraphics.CGBitmapFlags.PremultipliedFirst))
            {
                context.ConcatCTM(OrientationTransform(orientation, storedWidth, storedHeight, height));
                context.DrawImage(new CoreGraphics.CGRect(0, 0, storedWidth, storedHeight), image);
            }

            return PanoramicSurface.CreateTexture(device, pixels, width, height, bytesPerRow);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(pixels);
        }
    }

    // Maps the stored image onto the oriented canvas as ExifOrientationTransform.StoredToImage does. CoreGraphics
    // measures y from the bottom, so both sides flip y.
    private static CoreGraphics.CGAffineTransform OrientationTransform(ExifOrientationTransform orientation, int width, int height, int orientedHeight)
    {
        PointF origin = Map(0, 0);
        PointF unitX = Map(1, 0);
        PointF unitY = Map(0, 1);
        return new CoreGraphics.CGAffineTransform(unitX.X - origin.X, unitX.Y - origin.Y, unitY.X - origin.X, unitY.Y - origin.Y, origin.X, origin.Y);

        PointF Map(float x, float y)
        {
            PointF pixel = orientation.StoredToImage(new PointF(x, height - y), width, height);
            return new PointF(pixel.X, orientedHeight - pixel.Y);
        }
    }
#endif

#if __ANDROID__
    // The decoded (possibly downsampled) bitmap plus the image's full-resolution oriented dimensions.
    internal readonly record struct PanoramaFrame(Android.Graphics.Bitmap Bitmap, int Width, int Height);

    private void ApplyTexture(PanoramaFrame frame) => _surface.SetTexture(frame.Bitmap);

    // Lost a generation race: recycle now rather than via finalizers; full-size bitmaps add up during rapid paging.
    private static void DiscardFrame(PanoramaFrame? frame) => frame?.Bitmap.Recycle();
#elif __IOS__
    // The decoded (possibly downsampled) texture plus the image's full-resolution oriented dimensions.
    internal readonly record struct PanoramaFrame(Metal.IMTLTexture Texture, int Width, int Height);

    private void ApplyTexture(PanoramaFrame frame) => _surface.SetTexture(frame.Texture);

    // Releases a superseded frame now, since full-size textures add up during rapid paging.
    private static void DiscardFrame(PanoramaFrame? frame) => frame?.Texture.Dispose();
#else
    // The decoded image as tightly-packed BGRA8 plus its pixel dimensions.
    internal readonly record struct PanoramaFrame(byte[] Bgra, int Width, int Height);

    private void ApplyTexture(PanoramaFrame frame) => _surface.SetTexture(frame.Bgra, (uint)frame.Width, (uint)frame.Height);

    // byte[]-backed frames are plain managed memory; nothing to release eagerly.
    private static void DiscardFrame(PanoramaFrame? frame)
    {
    }
#endif

    // A marker resolved to a normalized (u,v), its symbol offset, and its swatch's half-size, all sizes in DIPs, kept on
    // the UI side for tap hit-testing (the surface owns the GPU side).
    internal readonly record struct ResolvedMarker(OrientedImageMarker Marker, float U, float V, double OffsetX, double OffsetY, double HalfWidth, double HalfHeight);

    // A marker's normalized (u,v) on the image.
    private sealed record Uv(float U, float V);

    // A rasterized symbol: its tightly-packed BGRA8 pixels, its offset from the anchor in DIPs, and the display scale
    // it was rasterized at.
    private sealed record Swatch(byte[] Bgra, int Width, int Height, double OffsetX, double OffsetY, double Scale);

    // A visible marker snapshotted for a pass, with its cached (u,v) and swatch if any.
    private readonly record struct PendingMarker(OrientedImageMarker Marker, OrientedImageMarkerPosition Position, Symbol Symbol, Uv? Uv, Swatch? Swatch);
}
#endif
