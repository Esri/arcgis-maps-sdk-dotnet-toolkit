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
using System.Collections.Generic;
using System.ComponentModel;
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
#if WPF
using Point = System.Windows.Point;
#elif WINDOWS_XAML
using Point = Windows.Foundation.Point;
#elif MAUI
using Point = Microsoft.Maui.Graphics.Point;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// Inner display for planar images: a MapView showing the image as a RasterLayer, markers as overlay graphics, and
// the visible pixel ring pushed to the footprint while auto-update is enabled.
// Known limitation: georeferenced TIFFs are not supported by this planar display. RasterLayer honors embedded
// georeferencing, while the display's pixel transforms assume an unreferenced, axis-aligned image grid.
internal sealed partial class OrientedImageRasterDisplay : OrientedImageInnerDisplay
{
    private readonly MapView _mapView;
    private readonly GraphicsOverlay _markersOverlay;

    private const double MarkerHitTolerance = 12d;

    private RasterLayer? _rasterLayer;
    private ExifOrientationTransform _imageOrientation;
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
        SetAutomationName(null);
    }

#if MAUI
    protected override View AutomationTarget => _mapView;
#elif WPF
    protected override System.Windows.DependencyObject AutomationTarget => _mapView;
#else
    protected override Microsoft.UI.Xaml.DependencyObject AutomationTarget => _mapView;
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
            // A Warning, such as missing Projection Engine data, still draws the image, so only Error counts.
            if (_mapView.Map?.OperationalLayers.Contains(layer) == true &&
                _mapView.GetLayerViewState(layer) is LayerViewState viewState &&
                viewState.Status.HasFlag(LayerViewStatus.Error) &&
                viewState.Error is Exception viewError)
                return viewError;
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
        _imageOrientation = ExifOrientationTransform.Read(dataUri);

        if (layer.Raster?.RasterInfo?.Extent is Envelope extent)
        {
            // The raster layer draws the file's stored pixel grid and ignores EXIF orientation, so the view applies the
            // orientation's rotation too. A view can't reflect, so mirrored images appear upright but reversed. Both
            // rotations are clockwise; MapView rotation is counter-clockwise.
            double viewRotation = -(GetEffectiveRotationDegrees(image) + _imageOrientation.RotationDegrees);
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
        _imageOrientation = default;
        foreach (Graphic graphic in _markerGraphics.Values)
            graphic.Geometry = null; // placed on the old image; re-placed once the next one is framed
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

    // Graphics are kept per marker, so only added markers need placing. The overlay follows collection order, which
    // is the drawing order.
    protected override void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed)
    {
        IReadOnlyList<OrientedImageMarker> markers = Markers;
        this.Dispatch(() =>
        {
            foreach (OrientedImageMarker marker in removed)
            {
                if (_markerGraphics.Remove(marker, out Graphic? graphic))
                    _graphicMarkers.Remove(graphic);
            }

            foreach (OrientedImageMarker marker in added)
            {
                Graphic graphic = new() { Symbol = marker.Symbol, IsVisible = marker.IsVisible };
                _markerGraphics[marker] = graphic;
                _graphicMarkers[graphic] = marker;
            }

            List<Graphic> ordered = markers.Select(marker => _markerGraphics[marker]).ToList();
            if (!_markersOverlay.Graphics.SequenceEqual(ordered))
            {
                _markersOverlay.Graphics.Clear();
                _markersOverlay.Graphics.AddRange(ordered);
            }

            _ = RefreshMarkerGeometriesAsync(added);
        });
    }

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
        var markerGraphics = markers is null ? new Dictionary<OrientedImageMarker, Graphic>(_markerGraphics) : _markerGraphics;
        foreach (OrientedImageMarker marker in markers ?? markerGraphics.Keys)
        {
            if (markerGraphics.TryGetValue(marker, out Graphic? graphic))
                await ResolveAndApplyMarkerGeometryAsync(marker, graphic);
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

    // Marker -> image pixel -> map point; null while the raster isn't ready. PresentAsync places markers again after
    // the image loads.
    private async Task<MapPoint?> ResolveMarkerMapPointAsync(OrientedImageMarker marker) =>
        await ResolveMarkerPixelAsync(marker.Position, Footprint?.OrientedImage) is PointF pixel ? PixelToMap(pixel) : null;

    // Clockwise degrees, summed without clamping: real data exceeds the spec's +-90 roll.
    private static double GetEffectiveRotationDegrees(OrientedImage image)
    {
        double roll = ReadRotationAttribute(image, "CameraRoll");
        double imageRotation = ReadRotationAttribute(image, "ImageRotation");
        return roll + imageRotation;
    }

    // CameraRoll and ImageRotation are esriFieldTypeDouble, so a boxed double is the only shape to accept.
    private static double ReadRotationAttribute(OrientedImage image, string name)
    {
        if (image.Attributes.TryGetValue(name, out object? raw) && raw is double value && !double.IsNaN(value))
            return value;
        return 0d;
    }

    // Raster cell sizes can be negative (flipped axis) or zero (unknown); a pixel is one unit in either case.
    private static double CellSize(double size) => size == 0 ? 1 : Math.Abs(size);

    // Maps an image pixel, in the oriented space OrientedImage uses, to the stored raster's map space; the inverse of
    // MapToPixel.
    private MapPoint? PixelToMap(PointF pixel)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = CellSize(info.CellSizeX);
        double cellY = CellSize(info.CellSizeY);
        pixel = _imageOrientation.ImageToStored(pixel, extent.Width / cellX, extent.Height / cellY);

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

    // Maps a point in the display's map space back to an image pixel (to report ImageTapped in image coordinates).
    private PointF? MapToPixel(MapPoint mapPoint)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = CellSize(info.CellSizeX);
        double cellY = CellSize(info.CellSizeY);
        double col = (mapPoint.X - extent.XMin) / cellX;
        double row = (extent.YMax - mapPoint.Y) / cellY;
        return _imageOrientation.StoredToImage(new PointF((float)col, (float)row), extent.Width / cellX, extent.Height / cellY);
    }

    public override PointF? ScreenToImage(double x, double y) =>
        IsInteractive && _mapView.ScreenToLocation(new Point(x, y)) is MapPoint location ? MapToPixel(location) : null;

    private async void OnMapViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        if (!IsInteractive || e.Location is not MapPoint location || Footprint?.OrientedImage is not OrientedImage image || MapToPixel(location) is not PointF imagePoint)
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
            // Identify can fail during map teardown. Report the tap without a marker.
        }

        // The image may have changed while identifying; the captured pixel is in the old image's space.
        if (token.IsCancellationRequested)
            return;

        RaiseImageTapped(new OrientedImageTappedEventArgs(imagePoint, image, marker));
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

        List<PointF> pixels = ComputeVisibleAreaPixels(visibleArea, extent, info.CellSizeX, info.CellSizeY, _imageOrientation);
        if (pixels.Count < 3)
            return null;

        return footprint.UpdateFootprintAsync(pixels, NextFootprintUpdateToken());
    }

    // Visible-area ring -> image-pixel ring clipped to the image rectangle. A true polygon clip, not per-vertex
    // clamping, which collapses a rotated view enclosing the whole image to a diamond. Internal for unit tests.
    internal static List<PointF> ComputeVisibleAreaPixels(Polygon visibleArea, Envelope extent, double cellSizeX, double cellSizeY, ExifOrientationTransform orientation = default)
    {
        double cellX = CellSize(cellSizeX);
        double cellY = CellSize(cellSizeY);
        double maxCol = extent.Width / cellX;
        double maxRow = extent.Height / cellY;

        // Clip in raster coordinates; EXIF can swap axes or reflect the pixel grid.
        var firstRing = new Polygon(visibleArea.Parts[0].Points, visibleArea.SpatialReference);
        var clipped = (Polygon)GeometryEngine.Intersection(firstRing, extent);
        IReadOnlyList<MapPoint> points = clipped.Parts.Count == 0 ? [] : clipped.Parts[0].Points;
        var pixels = new List<PointF>(points.Count);
        foreach (MapPoint point in points)
        {
            double col = (point.X - extent.XMin) / cellX;
            double row = (extent.YMax - point.Y) / cellY;
            pixels.Add(orientation.StoredToImage(new PointF((float)col, (float)row), maxCol, maxRow));
        }

        // A reflection reverses the ring's winding; reverse it again to keep it clockwise.
        if (orientation.IsMirrored)
            pixels.Reverse();

        return pixels;
    }
}
