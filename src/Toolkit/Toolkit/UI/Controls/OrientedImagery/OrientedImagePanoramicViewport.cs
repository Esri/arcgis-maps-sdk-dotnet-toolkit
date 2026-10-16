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

// Inner viewport for equirectangular 360 images. PanoramaCameraState holds the screen and pixel math.
internal sealed partial class OrientedImagePanoramicViewport : OrientedImageInnerViewport
{
    private const double MarkerHitTolerance = 12d;

#if MAUI
    private readonly PanoramicSurfaceView _surface;
#else
    private readonly PanoramicSurface _surface;
#endif
    private readonly List<ResolvedMarker> _resolvedMarkers = [];

    // One swatch per symbol, kept until the symbol changes or no visible marker uses it. Failures are not cached.
    private readonly Dictionary<Symbol, Swatch> _swatches = [];
    private int _markerGeneration;
    private bool _markerPassQueued;
    private int _imageWidth;
    private int _imageHeight;

    internal OrientedImagePanoramicViewport()
    {
#if MAUI
        _surface = new PanoramicSurfaceView();
#else
        _surface = new PanoramicSurface();
#endif
        _surface.SurfaceTapped += OnSurfaceTapped;
        _surface.RenderFailed += OnRenderFailed;
        _surface.ScaleChanged += OnScaleChanged;

        // Shown only while an image loads or is presented, so a failed viewport shows the control's background as the
        // planar viewport does. Paging goes straight from interactive to busy and keeps the surface shown.
        StateChanged += (_, _) => SetViewVisible(IsBusy || IsInteractive);
        SetViewVisible(false);
        SetAutomationName(null);
    }

#if MAUI
    public override View View => _surface;
#elif WPF
    public override System.Windows.FrameworkElement View => _surface;
#else
    public override Microsoft.UI.Xaml.FrameworkElement View => _surface;
#endif

    protected override bool IsPresentationInteractive => _imageWidth > 0 && _imageHeight > 0;

    // The first render failure wins, since the blanking render below can fail too.
    private void OnRenderFailed(Exception ex)
    {
        if (PresentationError is not null)
            return;

        PresentationError = ex;
        ClearPresentation();
        UpdateState();
    }

    private void OnScaleChanged() => QueueMarkerPass();

    // Marker state belongs to the UI thread; the hooks can run on any thread.
    protected override void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed) =>
        _surface.Dispatch(QueueMarkerPass);

    // Tag is not drawn. A symbol change, replaced or edited in place, re-rasterizes the symbol the marker has now.
    protected override void OnMarkerChanged(OrientedImageMarker marker, string? propertyName) => _surface.Dispatch(() =>
    {
        if (propertyName == nameof(OrientedImageMarker.Tag))
            return;

        if (propertyName is null or nameof(OrientedImageMarker.Symbol))
            _swatches.Remove(marker.Symbol ?? OrientedImageViewport.DefaultMarkerSymbol);
        QueueMarkerPass();
    });

    // For tests.
    internal int MarkerGeneration => _markerGeneration;

    // For tests.
    internal int MarkerPasses { get; private set; }

    // Changes come in bursts: each supersedes the pass in flight, and one pass on the next UI turn covers the burst.
    private void QueueMarkerPass()
    {
        _markerGeneration++;
        if (_markerPassQueued)
            return;

        _markerPassQueued = true;
        _surface.Post(() =>
        {
            _markerPassQueued = false;
            _ = ResolveMarkersAsync();
        });
    }

    // Resolves every visible marker to a (u,v) and a swatch, then hands the set to the surface. Every await resumes on
    // the UI thread, and a superseded pass stops at the next one.
    private async Task ResolveMarkersAsync()
    {
        MarkerPasses++;
        int generation = _markerGeneration;
        CancellationToken token = SessionToken;
        OrientedImage? image = Footprint?.OrientedImage;
        int imageWidth = _imageWidth;
        int imageHeight = _imageHeight;
        double scale = _surface.PixelsPerDip; // a swatch rasterized at another scale is a miss

        var resolved = new List<ResolvedMarker>();
        var drawn = new List<PanoramaMarker>();
        var usedSymbols = new HashSet<Symbol>();
        if (image is not null && imageWidth > 0 && imageHeight > 0)
        {
            foreach (OrientedImageMarker marker in Markers)
            {
                if (!marker.IsVisible)
                    continue;

                PointF? pixel = await ResolveMarkerPixelAsync(marker.Position, image);
                if (generation != _markerGeneration || token.IsCancellationRequested)
                    return;
                if (pixel is not PointF p)
                    continue;

                Symbol symbol = marker.Symbol ?? OrientedImageViewport.DefaultMarkerSymbol;
                Swatch? swatch = _swatches.GetValueOrDefault(symbol);
                if (swatch is null || swatch.Scale != scale)
                {
                    swatch = await CreateSwatchAsync(symbol, scale);
                    if (generation != _markerGeneration || token.IsCancellationRequested)
                        return;
                    if (swatch is null)
                        continue;
                    _swatches[symbol] = swatch;
                }

                usedSymbols.Add(symbol);
                float u = p.X / imageWidth;
                float v = p.Y / imageHeight;
                resolved.Add(new ResolvedMarker(marker, u, v, swatch.OffsetX, swatch.OffsetY, swatch.Width / scale / 2, swatch.Height / scale / 2));
                drawn.Add(new PanoramaMarker(u, v, swatch.Bgra, swatch.Width, swatch.Height, (float)(swatch.OffsetX * scale), (float)(swatch.OffsetY * scale)));
            }
        }

        foreach (Symbol symbol in _swatches.Keys.Where(symbol => !usedSymbols.Contains(symbol)).ToList())
            _swatches.Remove(symbol);

        _resolvedMarkers.Clear();
        _resolvedMarkers.AddRange(resolved);
        _surface.SetMarkers(drawn);
        _surface.RequestRender();
    }

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

    // Offset from the anchor in DIPs, y down, turned clockwise with the angle as on the map. Swatches include the angle
    // but not the offset. Composite and multilayer symbols get none, since their offsets vary per layer.
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

    protected override Task? BeginFootprintUpdate(OrientedImageFootprint footprint)
    {
        double width = _surface.ActualWidth;
        double height = _surface.ActualHeight;
        if (_imageWidth <= 0 || _imageHeight <= 0)
            return null;

        double heading = footprint.OrientedImage is OrientedImage image ? ReadHeadingDegrees(image.Attributes) : 0d;
        if (!Camera.TryGetFootprintView(width, height, heading, out PanoramaCameraState.FootprintView view))
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

        // A held target is converted after the decode, so a call made during it still shapes the first frame.
        LookAtTarget? lookAt = await ConvertHeldLookAtAsync(image);
        if (token.IsCancellationRequested)
        {
            frame.Dispose();
            return;
        }

        _imageWidth = frame.Width;
        _imageHeight = frame.Height;
        _surface.SetTexture(frame, path);

        // Open facing north at the horizon unless a look-at target is held. The identity camera centers u = 0.75 and
        // the image's center column faces CameraHeading, so yaw is -pi/2 minus the heading.
        PanoramaCameraState camera = PanoramaCameraState.Initial with { Yaw = (-MathF.PI / 2f) - ReadHeadingRadians(image.Attributes) };
        if (lookAt is LookAtTarget target)
            camera = LookAtCamera(camera, target);
        _surface.Camera = camera;
        _surface.RequestRender();

        // Push explicitly: the earlier push had no image dimensions, and an unchanged camera raises no ViewChanged.
        UpdateFootprint();
    }

    // The camera change raises ViewChanged, which updates the footprint.
    protected override bool TryLookAt(LookAtTarget target)
    {
        _surface.Camera = LookAtCamera(Camera, target);
        _surface.RequestRender();
        return true;
    }

    private PanoramaCameraState LookAtCamera(PanoramaCameraState camera, LookAtTarget target) =>
        camera.LookAt(target.Pixel.X / _imageWidth, target.AimAtRow ? target.Pixel.Y / _imageHeight : null);

    protected override void ClearPresentation()
    {
        _surface.ClearTexture();
        _imageWidth = 0;
        _imageHeight = 0;

        // Keeps an in-flight pass from applying stale markers. The swatches don't depend on the image.
        _markerGeneration++;
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

    // Markers draw in list order, so the last hit is on top, as in the planar viewport. All values are in DIPs.
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

    // CameraHeading is in degrees, and -999 means unknown. Values outside 0 to 360 wrap where they are used.
    internal static double ReadHeadingDegrees(IDictionary<string, object?> attributes) =>
        attributes.TryGetValue("CameraHeading", out object? raw) && raw is double degrees && !double.IsNaN(degrees) && degrees != -999 ? degrees : 0d;

    internal static float ReadHeadingRadians(IDictionary<string, object?> attributes) => (float)(ReadHeadingDegrees(attributes) * Math.PI / 180.0);

    // A drawn marker as tap hit-testing sees it. Offsets and half-sizes are in DIPs.
    internal readonly record struct ResolvedMarker(OrientedImageMarker Marker, float U, float V, double OffsetX, double OffsetY, double HalfWidth, double HalfHeight);

    // Tightly packed BGRA8 pixels, the offset from the anchor in DIPs, and the display scale it was rasterized at.
    private sealed record Swatch(byte[] Bgra, int Width, int Height, double OffsetX, double OffsetY, double Scale);
}
#endif
