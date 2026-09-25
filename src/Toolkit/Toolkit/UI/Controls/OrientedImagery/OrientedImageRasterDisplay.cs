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

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
#if MAUI
using Esri.ArcGISRuntime.Maui;
#else
using Esri.ArcGISRuntime.UI.Controls;
#if WPF
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
#else
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
#endif
#endif

// Disambiguate from Microsoft.Maui.Graphics.PointF (a MAUI global using)
using PointF = System.Drawing.PointF;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// Inner display for planar images: a MapView showing the image as a RasterLayer, markers as overlay graphics, and
// the visible pixel ring pushed to the footprint while auto-update is enabled.
internal sealed partial class OrientedImageRasterDisplay : OrientedImageInnerDisplay
{
    private readonly MapView _mapView;
    private readonly GraphicsOverlay _markersOverlay;

    private const double MarkerHitTolerance = 12d;

    private RasterLayer? _rasterLayer;
    private readonly Dictionary<OrientedImageMarker, Graphic> _markerGraphics = [];
    private readonly Dictionary<Graphic, OrientedImageMarker> _graphicMarkers = [];
    private bool _interactive;

    internal OrientedImageRasterDisplay()
    {
        // Lock map interaction until the image has loaded
        _mapView = new MapView { IsAttributionTextVisible = false, InteractionOptions = new MapViewInteractionOptions { IsEnabled = false } };
#if MAUI
        _mapView.HorizontalOptions = LayoutOptions.Fill;
        _mapView.VerticalOptions = LayoutOptions.Fill;
#endif

        // Default symbol for markers without their own; a marker's own Symbol overrides this renderer.
        _markersOverlay = new GraphicsOverlay
        {
            Renderer = new SimpleRenderer(OrientedImageDisplay.DefaultMarkerSymbol),
        };
        _mapView.GraphicsOverlays ??= new GraphicsOverlayCollection();
        _mapView.GraphicsOverlays.Add(_markersOverlay);
        _mapView.GeoViewTapped += OnMapViewTapped;
        _mapView.LayerViewStateChanged += (s, e) => UpdateState();
        _mapView.DrawStatusChanged += (s, e) =>
        {
            UpdateState();

            // The initial ViewpointChanged can fire while still drawing, where BeginFootprintUpdate rejects it, and no
            // later viewpoint event is guaranteed; push once drawing settles. Redundant pushes are safe (latest wins).
            if (e.Status == DrawStatus.Completed)
                UpdateFootprint();
        };
        Content = _mapView;
        UpdateAutomationName();
    }

#if MAUI
    protected override View AutomationNameTarget => _mapView;
#elif WPF
    protected override System.Windows.DependencyObject AutomationNameTarget => _mapView;
#else
    protected override Microsoft.UI.Xaml.DependencyObject AutomationNameTarget => _mapView;
#endif

    // A MapView with no Map sits at DrawStatus.InProgress forever, so only count drawing when there's a map.
    protected override bool IsPresentationBusy => _mapView.Map is not null && _mapView.DrawStatus == DrawStatus.InProgress;

    protected override bool IsPresentationInteractive => _interactive;

    // Error precedence: the image load error, the raster layer load error, the layer's view-state error, then
    // anything the load skeleton captured (e.g. a raster/layer construction failure that produces no LoadError).
    protected override Exception? ResolveError()
    {
        if (Footprint?.OrientedImage?.LoadError is Exception imageError)
            return imageError;

        if (_rasterLayer is RasterLayer layer)
        {
            if (layer.LoadError is Exception layerError)
                return layerError;

            // GetLayerViewState throws if the layer isn't in the current map (can happen during a map swap).
            if (_mapView.Map?.OperationalLayers.Contains(layer) == true &&
                _mapView.GetLayerViewState(layer) is LayerViewState layerViewState &&
                layerViewState.Error is Exception viewError)
            {
                // It's now standard for core to raise a warning on RasterLayers whenever pedata hasn't been set, which doesn't apply to most oriented image rasters
                // since most don't have a spatial reference in the first place
                if (!(layerViewState.Status.HasFlag(LayerViewStatus.Warning) && viewError.Message.Contains("The pedata directory has not been set.")))
                    return viewError;
            }
        }

        return PresentationError;
    }

    protected override async Task PresentAsync(OrientedImage image, Uri dataUri, CancellationToken token)
    {
        // Abort a previous layer's load and keep the view locked until the new raster is framed.
        _rasterLayer?.CancelLoad();
        SetInteractive(false);

        RasterLayer layer = new(CreateRaster(dataUri));
        layer.ResamplingType = RasterResamplingType.BilinearInterpolation;
        Map map = new();
        map.OperationalLayers.Add(layer);
        _mapView.Map = map;
        _rasterLayer = layer;
        await layer.LoadAsync();
        token.ThrowIfCancellationRequested();

        if (layer.Raster?.RasterInfo?.Extent is Envelope extent)
        {
            // The effective rotation is clockwise, MapView rotation counter-clockwise: negate. Only the view rotates, so
            // markers and hit-testing stay in native pixel space (OrientedImageRotation.DesignNotes.md).
            double viewRotation = -GetEffectiveRotationDegrees(image);
            try
            {
                // Frame and rotate in one animation-free viewpoint set.
                _mapView.SetViewpoint(new Viewpoint(extent, viewRotation));
            }
            catch
            {
                // Framing is best-effort; never let it block the unlock below.
            }

            if (token.IsCancellationRequested)
                return;

            // Markers set before the raster loaded can now be placed (the pixel-map transform exists).
            _ = RefreshMarkerGeometriesAsync();
        }

        SetInteractive(true);
    }

    protected override void ClearPresentation()
    {
        _rasterLayer?.CancelLoad();
        _mapView.Map = null;
        _rasterLayer = null;
        SetInteractive(false);
    }

    public override void SetBackgroundColor(System.Drawing.Color color)
    {
        // Empty restores the default grid; otherwise a solid color with zero-width grid lines.
        _mapView.BackgroundGrid = color.IsEmpty
            ? new BackgroundGrid()
            : new BackgroundGrid(color, System.Drawing.Color.Transparent, 0f, 16f);
    }

    protected override void OnAutoUpdateFootprintChanged(bool enabled)
    {
        if (enabled)
            _mapView.ViewpointChanged += OnViewpointChanged;
        else
            _mapView.ViewpointChanged -= OnViewpointChanged;
    }

    // Locks/unlocks user interaction; programmatic SetViewpoint still works while locked. Replace the whole
    // MapViewInteractionOptions object: an in-place IsEnabled flip is ignored.
    private void SetInteractive(bool enabled)
    {
        _interactive = enabled;
        if (_mapView.InteractionOptions?.IsEnabled == enabled)
            return;

        _mapView.InteractionOptions = new MapViewInteractionOptions { IsEnabled = enabled };
    }

    private static Raster CreateRaster(Uri uri)
    {
        bool isHttp = uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (isHttp)
            return new ImageServiceRaster(uri);

        return new Raster(uri.IsAbsoluteUri && uri.IsFile ? uri.LocalPath : uri.OriginalString);
    }

    // Snapshot the app-owned collection on the calling thread; the overlay and dictionary work runs on the UI thread.
    protected override void RebuildMarkers()
    {
        List<OrientedImageMarker>? snapshot = Markers is null ? null : new(Markers);
        this.Dispatch(() =>
        {
            _markerGraphics.Clear();
            _graphicMarkers.Clear();
            _markersOverlay.Graphics.Clear();
            AddMarkers(snapshot ?? []);
        });
    }

    protected override void AddMarkers(IEnumerable<OrientedImageMarker> newMarkers)
    {
        this.Dispatch(() =>
        {
            foreach (OrientedImageMarker marker in newMarkers)
            {
                Graphic graphic = new() { Symbol = marker.Symbol, IsVisible = marker.IsVisible };
                _markerGraphics[marker] = graphic;
                _graphicMarkers[graphic] = marker;
                _markersOverlay.Graphics.Add(graphic);
            }

            _ = RefreshMarkerGeometriesAsync(newMarkers);
        });
    }

    protected override void ReplaceMarker(OrientedImageMarker oldMarker, OrientedImageMarker newMarker, int index)
    {
        this.Dispatch(() =>
        {
            var oldGraphic = _markersOverlay.Graphics[index];
            _markerGraphics.Remove(oldMarker);
            _graphicMarkers.Remove(oldGraphic);

            Graphic newGraphic = new() { Symbol = newMarker.Symbol, IsVisible = newMarker.IsVisible };
            _markerGraphics[newMarker] = newGraphic;
            _graphicMarkers[newGraphic] = newMarker;
            _markersOverlay.Graphics[index] = newGraphic;

            _ = RefreshMarkerGeometriesAsync([newMarker]);
        });
    }

    protected override void RemoveMarkers(int _, IEnumerable<OrientedImageMarker> removedMarkers)
    {
        this.Dispatch(() =>
        {
            foreach (OrientedImageMarker marker in removedMarkers)
            {
                if (_markerGraphics.Remove(marker, out Graphic? graphic))
                {
                    _graphicMarkers.Remove(graphic);
                    _markersOverlay.Graphics.Remove(graphic);
                }
            }
        });
    }

    protected override void MoveMarkers(int oldIndex, int newIndex) => this.Dispatch(() => _markersOverlay.Graphics.Move(oldIndex, newIndex));

    // Dispatch so marker updates can't touch the graphic/dictionaries off the UI thread.
    protected override void OnMarkerChanged(OrientedImageMarker marker, string? propertyName)
    {
        this.Dispatch(() =>
        {
            if (!_markerGraphics.TryGetValue(marker, out Graphic? graphic))
                return;

            switch (propertyName)
            {
                case nameof(OrientedImageMarker.Symbol):
                    graphic.Symbol = marker.Symbol;
                    break;
                case nameof(OrientedImageMarker.IsVisible):
                    graphic.IsVisible = marker.IsVisible;
                    break;
                case nameof(OrientedImageMarker.Position):
                    _ = ResolveAndApplyMarkerGeometryAsync(marker, graphic);
                    break;
            }
        });
    }

    // Re-places the given markers, or all of them when null.
    private async Task RefreshMarkerGeometriesAsync(IEnumerable<OrientedImageMarker>? markers = null)
    {
        if (markers == null)
        {
            var markerGraphicsSnapshot = new Dictionary<OrientedImageMarker, Graphic>(_markerGraphics);
            foreach (KeyValuePair<OrientedImageMarker, Graphic> pair in markerGraphicsSnapshot)
            {
                await ResolveAndApplyMarkerGeometryAsync(pair.Key, pair.Value);
            }
        }
        else
        {
            foreach (OrientedImageMarker marker in markers)
            {
                if (_markerGraphics.TryGetValue(marker, out Graphic? graphic))
                    await ResolveAndApplyMarkerGeometryAsync(marker, graphic);
            }
        }
    }

    private async Task ResolveAndApplyMarkerGeometryAsync(OrientedImageMarker marker, Graphic graphic)
    {
        CancellationToken token = SessionToken;
        MapPoint? mapPoint = await ResolveMarkerMapPointAsync(marker);
        // Apply only if the graphic is still the marker's and the session is unchanged: a pixel projected through the old
        // image's camera model must not land on the new raster. A null point clears the geometry.
        if (!token.IsCancellationRequested && _markerGraphics.TryGetValue(marker, out Graphic? current) && ReferenceEquals(current, graphic))
            graphic.Geometry = mapPoint;
    }

    // Marker -> image pixel (world-anchored via the camera model) -> map point; null while the raster isn't ready.
    private async Task<MapPoint?> ResolveMarkerMapPointAsync(OrientedImageMarker marker)
    {
        OrientedImageMarkerPosition position = marker.Position;
        PointF pixel;
        if (position.ImagePoint is PointF imagePoint)
        {
            pixel = imagePoint;
        }
        else if (position.Location is MapPoint location && Footprint?.OrientedImage is OrientedImage image)
        {
            // Core fails every later transform on an image whose first transform ran before load; PresentAsync retries after load.
            if (image.LoadStatus != LoadStatus.Loaded)
                return null;

            try
            {
                pixel = await image.LocationToImageAsync(location);
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

        return PixelToMap(pixel);
    }

    // Clockwise degrees, summed without clamping: real data exceeds the spec's +-90 roll.
    private static double GetEffectiveRotationDegrees(OrientedImage image)
    {
        double roll = ReadRotationAttribute(image, "CameraRoll");
        double imageRotation = ReadRotationAttribute(image, "ImageRotation");
        return roll + imageRotation + ReadExifRotationDegrees(image.DataUri);
    }

    // CameraRoll and ImageRotation are esriFieldTypeDouble, so a boxed double is the only shape to accept.
    private static double ReadRotationAttribute(OrientedImage image, string name)
    {
        if (image.Attributes.TryGetValue(name, out object? raw) && raw is double value && !double.IsNaN(value))
            return value;
        return 0d;
    }

    // Clockwise display rotation (0/90/180/270) from a local JPEG's EXIF Orientation; 0 when absent or not applicable.
    private static double ReadExifRotationDegrees(Uri? dataUri)
    {
        if (dataUri is null || !dataUri.IsFile)
            return 0;

        // Minimal metadata parser inspired by https://stackoverflow.com/q/7584794/383361
        try
        {
            // Open shared: the SDK owns the downloaded file.
            using FileStream stream = new FileStream(dataUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.ReadByte() != 0xFF || stream.ReadByte() != 0xD8)
                return 0; // not a JPEG

            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0)
                    return 0; // EOF before metadata
                if (b != 0xFF)
                    continue;

                int marker;
                do
                {
                    marker = stream.ReadByte();
                }
                while (marker == 0xFF);
                if (marker < 0 || marker == 0xDA || marker == 0xD9)
                    return 0; // start-of-scan / end-of-image: no (more) metadata to read

                if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                    continue; // standalone markers, no length field

                int hi = stream.ReadByte();
                int lo = stream.ReadByte();
                if (hi < 0 || lo < 0)
                    return 0; // EOF before length field
                int payloadLength = ((hi << 8) | lo) - 2;
                if (payloadLength < 0 || payloadLength > stream.Length - stream.Position)
                    return 0; // malformed length field

                if (marker == 0xE1 && payloadLength >= 14)
                {
                    byte[] payload = new byte[payloadLength];
                    stream.ReadExactly(payload);
                    int orientation = ParseExifOrientation(payload);
                    if (orientation > 0)
                        return orientation switch { 3 => 180d, 6 => 90d, 8 => 270d, _ => 0d };
                    continue; // not the EXIF APP1 (e.g. XMP) or no Orientation tag
                }

                stream.Seek(payloadLength, SeekOrigin.Current);
            }
        }
        catch
        {
            // Fall back to "no rotation" in case of I/O or parsing errors.
            return 0;
        }
    }

    // Extracts the EXIF Orientation value (1..8, or 0 if absent) from a JPEG APP1 payload ("Exif\0\0" + TIFF + IFD0).
    private static int ParseExifOrientation(byte[] app1)
    {
        if (app1.Length < 14 ||
            app1[0] != (byte)'E' || app1[1] != (byte)'x' || app1[2] != (byte)'i' || app1[3] != (byte)'f' || app1[4] != 0 || app1[5] != 0)
            return 0;

        const int tiff = 6;
        bool little = app1[tiff] == 0x49 && app1[tiff + 1] == 0x49;
        if (!little && !(app1[tiff] == 0x4D && app1[tiff + 1] == 0x4D))
            return 0; // byte-order mark is neither "II" (little) nor "MM" (big)

        if (ReadExifUInt16(app1, tiff + 2, little) != 42)
            return 0; // expected TIFF magic number

        long ifd0 = tiff + ReadExifUInt32(app1, tiff + 4, little);
        if (ifd0 < 0 || ifd0 + 2 > app1.Length)
            return 0;

        int count = ReadExifUInt16(app1, (int)ifd0, little);
        for (int i = 0; i < count; i++)
        {
            int entry = (int)ifd0 + 2 + (i * 12);
            if (entry + 12 > app1.Length)
                return 0;
            if (ReadExifUInt16(app1, entry, little) == 0x0112)
                return ReadExifUInt16(app1, entry + 8, little); // Orientation is a SHORT in the value field
        }

        return 0;
    }

    private static ushort ReadExifUInt16(byte[] data, int offset, bool little) =>
        little
            ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset))
            : BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));

    private static uint ReadExifUInt32(byte[] data, int offset, bool little) =>
        little
            ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset))
            : BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));

    // Raster cell sizes can be negative (flipped axis) or zero (unknown); a pixel is one unit in either case.
    private static double CellSize(double size) => size == 0 ? 1 : Math.Abs(size);

    // Maps an image pixel to display map space; the inverse of MapToPixel.
    private MapPoint? PixelToMap(PointF pixel)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = CellSize(info.CellSizeX);
        double cellY = CellSize(info.CellSizeY);

        // Drop non-finite or wildly off-image pixels (e.g. the camera's own location projected onto its image).
        if (!IsPlaceablePixel(pixel.X, extent.Width / cellX) || !IsPlaceablePixel(pixel.Y, extent.Height / cellY))
            return null;

        double x = extent.XMin + (pixel.X * cellX);
        double y = extent.YMax - (pixel.Y * cellY);
        return new MapPoint(x, y, extent.SpatialReference);
    }

    private static bool IsPlaceablePixel(double value, double max)
    {
        if (!double.IsFinite(value))
            return false;

        // Pixels beyond this many image-sizes off the raster are treated as unplaceable
        const double MarkerPlacementMarginFactor = 100d;
        double margin = Math.Max(Math.Abs(max), 1d) * MarkerPlacementMarginFactor;
        return value >= -margin && value <= max + margin;
    }

    // Maps a point in the display's map space back to an image pixel (to report ImageClicked in image coordinates).
    private PointF? MapToPixel(MapPoint mapPoint)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = CellSize(info.CellSizeX);
        double cellY = CellSize(info.CellSizeY);
        double col = (mapPoint.X - extent.XMin) / cellX;
        double row = (extent.YMax - mapPoint.Y) / cellY;
        return new PointF((float)col, (float)row);
    }

    private async void OnMapViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        if (e.Location is not MapPoint location || Footprint?.OrientedImage is not OrientedImage image || MapToPixel(location) is not PointF imagePoint)
            return;

        CancellationToken token = SessionToken;
        OrientedImageMarker? marker = null;
        try
        {
            var result = await _mapView.IdentifyGraphicsOverlayAsync(_markersOverlay, e.Position, MarkerHitTolerance, false, 1);
            if (result.Graphics.Count > 0)
                _graphicMarkers.TryGetValue(result.Graphics[0], out marker);
        }
        catch
        {
            // Identify can fail during map teardown. Report the image click without a marker.
        }

        // The image may have changed while identifying; the captured pixel is in the old image's space.
        if (token.IsCancellationRequested)
            return;

        RaiseImageClicked(new OrientedImageDisplay.ImageClickedEventArgs(imagePoint, image, marker));
    }

    private void OnViewpointChanged(object? sender, EventArgs e) => UpdateFootprint();

    // Planar image: push the visible part of the raster as a pixel ring.
    protected override Task? BeginFootprintUpdate(OrientedImageFootprint footprint)
    {
        if (_mapView.DrawStatus != DrawStatus.Completed)
            return null;

        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        if (_mapView.VisibleArea is not Polygon visibleArea || visibleArea.Parts.Count == 0)
            return null;

        List<PointF> pixels = ComputeVisibleAreaPixels(visibleArea, extent, info.CellSizeX, info.CellSizeY);
        if (pixels.Count < 3)
            return null;

        return footprint.UpdateFootprintAsync(pixels, NextFootprintUpdateToken());
    }

    // Visible-area ring -> image-pixel ring clipped to the image rectangle. A true polygon clip, not per-vertex
    // clamping, which collapses a rotated view enclosing the whole image to a diamond. Internal for unit tests.
    internal static List<PointF> ComputeVisibleAreaPixels(Polygon visibleArea, Envelope extent, double cellSizeX, double cellSizeY)
    {
        double cellX = CellSize(cellSizeX);
        double cellY = CellSize(cellSizeY);
        double maxCol = extent.Width / cellX;
        double maxRow = extent.Height / cellY;

        IReadOnlyList<MapPoint> points = visibleArea.Parts[0].Points;
        var ring = new List<(double X, double Y)>(points.Count);
        foreach (MapPoint point in points)
            ring.Add(((point.X - extent.XMin) / cellX, (extent.YMax - point.Y) / cellY));

        List<(double X, double Y)> clipped = ClipToRectangle(ring, maxCol, maxRow);
        var pixels = new List<PointF>(clipped.Count);
        foreach ((double x, double y) in clipped)
            pixels.Add(new PointF((float)x, (float)y));

        return pixels;
    }

    // Sutherland-Hodgman intersection of a polygon ring with the axis-aligned rectangle [0,maxX]x[0,maxY].
    // The clip region is convex, so this is exact for any simple subject ring. Empty when fully outside.
    private static List<(double X, double Y)> ClipToRectangle(List<(double X, double Y)> ring, double maxX, double maxY)
    {
        List<(double X, double Y)> output = ring;
        output = ClipEdge(output, p => p.X >= 0, (a, b) => IntersectAtX(a, b, 0));
        output = ClipEdge(output, p => p.X <= maxX, (a, b) => IntersectAtX(a, b, maxX));
        output = ClipEdge(output, p => p.Y >= 0, (a, b) => IntersectAtY(a, b, 0));
        output = ClipEdge(output, p => p.Y <= maxY, (a, b) => IntersectAtY(a, b, maxY));
        return output;
    }

    private static List<(double X, double Y)> ClipEdge(
        List<(double X, double Y)> input,
        Func<(double X, double Y), bool> inside,
        Func<(double X, double Y), (double X, double Y), (double X, double Y)> intersect)
    {
        var output = new List<(double X, double Y)>(input.Count + 2);
        for (int i = 0; i < input.Count; i++)
        {
            (double X, double Y) current = input[i];
            (double X, double Y) previous = input[(i + input.Count - 1) % input.Count];
            bool currentInside = inside(current);
            if (currentInside != inside(previous))
                output.Add(intersect(previous, current));
            if (currentInside)
                output.Add(current);
        }

        return output;
    }

    private static (double X, double Y) IntersectAtX((double X, double Y) a, (double X, double Y) b, double x)
        => (x, a.Y + ((b.Y - a.Y) * ((x - a.X) / (b.X - a.X))));

    private static (double X, double Y) IntersectAtY((double X, double Y) a, (double X, double Y) b, double y)
        => (a.X + ((b.X - a.X) * ((y - a.Y) / (b.Y - a.Y))), y);
}
