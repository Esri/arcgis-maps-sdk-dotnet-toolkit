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
    private bool _markerPassQueued;
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
        _surface.ScaleChanged += OnScaleChanged;
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

    // Internal for tests, which can't lose or rebuild a device.
    internal void OnDeviceLost()
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
    internal async void OnDeviceRecreated()
    {
        // A load in flight supplies the rebuilt surface when it completes, so a second decode would be wasted. A load
        // that failed stays failed: the display never retries on its own. Without an image there is nothing to
        // re-supply. In all three cases the recovery ends here, and a load in flight still reports busy on its own.
        OrientedImage? image = Footprint?.OrientedImage;
        if (image is null || IsLoading || image.LoadStatus == LoadStatus.FailedToLoad)
        {
            _recovering = false;
            UpdateState();
            return;
        }

        // The rebuilt surface is blank until re-supplied: invalidate the dimensions so a tap isn't reported against the
        // old pixel space, and report busy/non-interactive so bound commands don't stay enabled over a blank panorama.
        _imageWidth = 0;
        _imageHeight = 0;
        _recovering = true;
        UpdateState();

        CancellationToken token = SessionToken;
        try
        {
            if (token.IsCancellationRequested)
                return;

            await image.RetryLoadAsync(); // idempotent; covers the image being unloaded/cancelled during teardown
            if (token.IsCancellationRequested || image.DataUri is not { IsAbsoluteUri: true, IsFile: true } uri)
                return;

            // A newer SetFootprint cancels the session token, aborting a now-pointless re-decode.
            PanoramaFrame frame = await PanoramaFrame.DecodeAsync(uri.LocalPath, token);
            if (token.IsCancellationRequested)
            {
                frame.Dispose();
                return;
            }

            _imageWidth = frame.Width;
            _imageHeight = frame.Height;
            _surface.SetTexture(frame);
            _surface.RequestRender();
            QueueMarkerPass();

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

    // Resolving again rasterizes the swatches at the new scale.
    private void OnScaleChanged() => QueueMarkerPass();

    // The caches belong to the UI thread.
    protected override void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed) => this.Dispatch(() =>
    {
        foreach (OrientedImageMarker marker in removed)
        {
            _markerUvs.Remove(marker);
            _markerSwatches.Remove(marker);
        }

        QueueMarkerPass();
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

        QueueMarkerPass();
    });

    // For tests. Bumped by each marker change that needs a pass, and by ClearPresentation.
    internal int MarkerGeneration => _markerGeneration;

    // For tests. The number of passes that started.
    internal int MarkerPasses { get; private set; }

    // Marker changes come in bursts, such as a loop over the markers or a symbol that many of them share. Each change
    // supersedes the pass in flight, and one pass on the next UI turn covers the whole burst.
    private void QueueMarkerPass()
    {
        Interlocked.Increment(ref _markerGeneration);
        if (_markerPassQueued)
            return;

        _markerPassQueued = true;
        this.Post(() =>
        {
            _markerPassQueued = false;
            _ = ResolveMarkersAsync();
        });
    }

    // Resolves every visible marker to a normalized (u,v) plus a rasterized swatch and pushes the set to the surface.
    // Cached results are reused, and the rest is computed off the UI thread. A superseded pass stops at its next check
    // and leaves the rest to the newer pass. Only the final apply marshals back.
    private async Task ResolveMarkersAsync()
    {
        MarkerPasses++;
        int generation = Volatile.Read(ref _markerGeneration);
        CancellationToken token = SessionToken;
        OrientedImage? image = Footprint?.OrientedImage;
        int imageWidth = _imageWidth;
        int imageHeight = _imageHeight;

        // Snapshot the app-owned markers and the caches on the UI thread before going async. A swatch rasterized at
        // another scale is a miss.
        double scale = _surface.PixelsPerDip;
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

            var drawn = new List<PanoramaMarker>(results.Count);
            _resolvedMarkers.Clear();
            foreach ((OrientedImageMarker marker, Uv uv, Swatch swatch) in results)
            {
                _markerUvs[marker] = uv;
                _markerSwatches[marker] = swatch;
                _resolvedMarkers.Add(new ResolvedMarker(marker, uv.U, uv.V, swatch.OffsetX, swatch.OffsetY, swatch.Width / scale / 2, swatch.Height / scale / 2));
                drawn.Add(new PanoramaMarker(uv.U, uv.V, swatch.Bgra, swatch.Width, swatch.Height, (float)(swatch.OffsetX * scale), (float)(swatch.OffsetY * scale)));
            }

            _surface.SetMarkers(drawn);
            _surface.RequestRender();
        });
    }

    private static async Task<Uv?> ResolveUvAsync(OrientedImageMarkerPosition position, OrientedImage image, int imageWidth, int imageHeight)
    {
        PointF? pixel = await ResolveMarkerPixelAsync(position, image).ConfigureAwait(false);
        return pixel is PointF p ? new Uv(p.X / imageWidth, p.Y / imageHeight) : null;
    }

    // Rasterizes a symbol to a tightly-packed BGRA8 swatch via RuntimeImage, with its offset.
    private static async Task<Swatch?> CreateSwatchAsync(Symbol symbol, double scale)
    {
        try
        {
            RuntimeImage? image = await symbol.CreateSwatchAsync(scale * 96).ConfigureAwait(false);
            if (image is null)
                return null;

            using Stream raw = await image.GetRawBufferAsync().ConfigureAwait(false);
            // GetRawBufferAsync returns a fresh, exact-size buffer that can be owned without copying.
            byte[] bytes;
            if (raw is MemoryStream memory && memory.TryGetBuffer(out ArraySegment<byte> segment) &&
                segment.Array is byte[] array && segment.Offset == 0 && segment.Count == array.Length)
            {
                bytes = array;
            }
            else
            {
                bytes = ReadAllBytes(raw);
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

    protected override async Task PresentAsync(OrientedImage image, string path, CancellationToken token)
    {
        PanoramaFrame frame = await PanoramaFrame.DecodeAsync(path, token);
        if (token.IsCancellationRequested)
        {
            frame.Dispose(); // never applied; release promptly rather than via finalizers
            return;
        }

        _imageWidth = frame.Width;
        _imageHeight = frame.Height;
        _surface.SetTexture(frame);
        _recovering = false; // re-supplied, whether or not a rebuilt device asked; a later DeviceLost starts over

        // Each image opens facing north at the horizon, with a 90-degree vertical field of view. The identity camera
        // centers u = 0.75 and the center column faces CameraHeading, so yaw is -pi/2 minus the heading.
        _surface.Camera = PanoramaCameraState.Initial with { Yaw = (-MathF.PI / 2f) - ReadHeadingRadians(image.Attributes) };
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
        _surface.SetMarkers(Array.Empty<PanoramaMarker>());

        _surface.RequestRender();
    }

    protected override void OnPresentCompleted() => QueueMarkerPass();

    private PanoramaCameraState Camera => _surface.Camera;

    // The projection answers for any direction, so points off the surface are rejected first.
    public override PointF? ScreenToImage(double x, double y) =>
        IsInteractive && x >= 0 && y >= 0 && x <= _surface.ActualWidth && y <= _surface.ActualHeight &&
        Camera.TryScreenToNormalizedUv(x, y, _surface.ActualWidth, _surface.ActualHeight, out float u, out float v)
            ? new PointF(u * _imageWidth, v * _imageHeight)
            : null;

    private void OnSurfaceTapped(double x, double y)
    {
        if (Footprint?.OrientedImage is not OrientedImage image || ScreenToImage(x, y) is not PointF pixel)
            return;

        OrientedImageMarker? marker = HitTestMarker(_resolvedMarkers, Camera, _surface.ActualWidth, _surface.ActualHeight, x, y);
        RaiseImageTapped(new OrientedImageTappedEventArgs(pixel, image, marker));
    }

    // Returns the topmost marker whose swatch lies within the hit tolerance of the tap, or null, as the planar display's
    // identify does. Markers draw in list order, so the last one is on top. A swatch is centered on its projected anchor
    // plus its symbol offset. All values are in DIPs.
    internal static OrientedImageMarker? HitTestMarker(IReadOnlyList<ResolvedMarker> markers, PanoramaCameraState camera,
        double viewWidth, double viewHeight, double x, double y)
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            ResolvedMarker resolved = markers[i];
            if (!camera.TryNormalizedUvToScreen(resolved.U, resolved.V, viewWidth, viewHeight, out double sx, out double sy))
                continue;

            // The tap's distance from the swatch's rectangle; zero inside it.
            double dx = Math.Max(Math.Abs(x - sx - resolved.OffsetX) - resolved.HalfWidth, 0);
            double dy = Math.Max(Math.Abs(y - sy - resolved.OffsetY) - resolved.HalfHeight, 0);
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
