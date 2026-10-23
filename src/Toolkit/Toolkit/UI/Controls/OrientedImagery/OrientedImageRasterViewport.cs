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

// Disambiguate from Microsoft.Maui.Graphics.PointF, a MAUI global using.
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

// Shows a planar image as a RasterLayer in a MapView. Georeferenced TIFFs are unsupported: RasterLayer honors their
// georeferencing, but the pixel transforms here assume an unreferenced, axis-aligned grid.
internal sealed partial class OrientedImageRasterViewport : OrientedImageInnerViewport
{
    private readonly MapView _mapView;
    private readonly GraphicsOverlay _markersOverlay;

    private const double MarkerHitTolerance = 12d;

    private RasterLayer? _rasterLayer;
    private ExifOrientationTransform _imageOrientation;
    private readonly Dictionary<OrientedImageMarker, Graphic> _markerGraphics = [];
    private readonly Dictionary<Graphic, OrientedImageMarker> _graphicMarkers = [];
    private bool _interactive;

    // The visible area reflects the fit only after the first draw, so footprint updates wait for it.
    private bool _hasDrawn;

    internal OrientedImageRasterViewport()
    {
        _mapView = new MapView { IsAttributionTextVisible = false, InteractionOptions = new MapViewInteractionOptions { IsEnabled = false } };
#if MAUI
        _mapView.HorizontalOptions = LayoutOptions.Fill;
        _mapView.VerticalOptions = LayoutOptions.Fill;
#endif

        // For markers without a symbol; a graphic's own symbol overrides the renderer.
        _markersOverlay = new GraphicsOverlay
        {
            Renderer = new SimpleRenderer(OrientedImageViewport.DefaultMarkerSymbol),
        };
        _mapView.GraphicsOverlays ??= new GraphicsOverlayCollection();
        _mapView.GraphicsOverlays.Add(_markersOverlay);
        _mapView.GeoViewTapped += OnMapViewTapped;
        _mapView.LayerViewStateChanged += (s, e) => UpdateState();
        _mapView.DrawStatusChanged += (s, e) =>
        {
            if (e.Status != DrawStatus.Completed)
                return;

            // The initial ViewpointChanged came before the first draw and was rejected, so update here.
            _hasDrawn = true;
            UpdateFootprint();
        };
        SetViewVisible(false);
        SetAutomationName(null);
    }

#if MAUI
    public override View View => _mapView;
#elif WPF
    public override System.Windows.FrameworkElement View => _mapView;
#else
    public override Microsoft.UI.Xaml.FrameworkElement View => _mapView;
#endif

    protected override bool IsPresentationInteractive => _interactive;

    protected override Exception? ResolveError()
    {
        if (!_isLoading && Footprint?.OrientedImage?.LoadError is Exception imageError)
            return imageError;

        if (_rasterLayer is RasterLayer layer)
        {
            if (layer.LoadError is Exception layerError)
                return layerError;

            // GetLayerViewState throws for a layer outside the current map, as during a map swap. A Warning still draws
            // the image, so only Error counts.
            if (_mapView.Map?.OperationalLayers.Contains(layer) == true &&
                _mapView.GetLayerViewState(layer) is LayerViewState viewState &&
                viewState.Status.HasFlag(LayerViewStatus.Error) &&
                viewState.Error is Exception viewError)
                return viewError;
        }

        return PresentationError;
    }

    protected override async Task PresentAsync(OrientedImage image, string path, CancellationToken token)
    {
        _rasterLayer?.CancelLoad();
        SetInteractive(false);

        RasterLayer layer = new(new Raster(path));
        layer.ResamplingType = RasterResamplingType.BilinearInterpolation;
        Map map = new();
        map.OperationalLayers.Add(layer);
        _mapView.Map = map;
        _rasterLayer = layer;
        _hasDrawn = false;
        SetViewVisible(true);
        await layer.LoadAsync();
        token.ThrowIfCancellationRequested();
        _imageOrientation = ExifOrientationTransform.Read(path);

        LookAtTarget? lookAt = await ConvertHeldLookAtAsync(image);
        token.ThrowIfCancellationRequested();

        if (layer.Raster?.RasterInfo?.Extent is Envelope extent)
        {
            MapPoint? target = lookAt is LookAtTarget held ? PixelToMap(held.Pixel, onImage: true) : null;
            try
            {
                _mapView.SetViewpoint(FitViewpoint(image, extent, target ?? extent.GetCenter()));
            }
            catch
            {
                // Framing is best-effort and must not block the unlock.
            }

            if (token.IsCancellationRequested)
                return;

            // Place markers set while the raster loaded, when no pixel-to-map transform existed.
            _ = RefreshMarkerGeometriesAsync();
        }

        SetInteractive(true);
    }

    protected override bool TryLookAt(LookAtTarget target)
    {
        if (PixelToMap(target.Pixel, onImage: true) is not MapPoint center || Footprint?.OrientedImage is not OrientedImage image
            || _rasterLayer?.Raster?.RasterInfo?.Extent is not Envelope extent)
            return false;

        try
        {
            // The view has no viewpoint until it has a size. Until then, keep the scale of the pending fit.
            _mapView.SetViewpoint(_mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale) is Viewpoint current
                ? new Viewpoint(center, current.TargetScale, current.Rotation)
                : FitViewpoint(image, extent, center));
            return true;
        }
        catch
        {
            // The view rejects viewpoints during map teardown.
            return false;
        }
    }

    protected override void ClearPresentation()
    {
        _rasterLayer?.CancelLoad();
        _mapView.Map = null;
        _rasterLayer = null;
        _hasDrawn = false;
        SetViewVisible(false);
        _imageOrientation = default;
        foreach (Graphic graphic in _markerGraphics.Values)
            graphic.Geometry = null;
        SetInteractive(false);
    }

    public override void SetBackgroundColor(System.Drawing.Color color)
    {
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

    // SetViewpoint still works while locked. Replace the options object; the view ignores an in-place IsEnabled change.
    private void SetInteractive(bool enabled)
    {
        _interactive = enabled;
        if (_mapView.InteractionOptions?.IsEnabled == enabled)
            return;

        _mapView.InteractionOptions = new MapViewInteractionOptions { IsEnabled = enabled };
    }

    // The overlay follows collection order, which is the drawing order.
    protected override void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed)
    {
        IReadOnlyList<OrientedImageMarker> markers = Markers;
        _mapView.Dispatch(() =>
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

    protected override void OnMarkerChanged(OrientedImageMarker marker, string? propertyName)
    {
        _mapView.Dispatch(() =>
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
        OrientedImageMarkerPosition position = marker.Position;
        MapPoint? mapPoint = await ResolveMarkerMapPointAsync(position);

        // A pixel from the old image's camera model must not land on the new raster, and a slow projection of an
        // earlier position must not replace a newer one.
        if (!token.IsCancellationRequested && marker.Position == position &&
            _markerGraphics.TryGetValue(marker, out Graphic? current) && ReferenceEquals(current, graphic))
        {
            graphic.Geometry = mapPoint;
        }
    }

    private async Task<MapPoint?> ResolveMarkerMapPointAsync(OrientedImageMarkerPosition position) =>
        await ResolveMarkerPixelAsync(position, Footprint?.OrientedImage) is PointF pixel ? PixelToMap(pixel) : null;

    // An image-sized envelope fits at the same scale wherever centered. The view defers the fit until it has a size.
    private Viewpoint FitViewpoint(OrientedImage image, Envelope extent, MapPoint center)
    {
        // RasterLayer draws the stored pixel grid, ignoring EXIF orientation, so the view applies its rotation too.
        // Both rotations are clockwise, and MapView rotation is counter-clockwise.
        double viewRotation = -(GetEffectiveRotationDegrees(image) + _imageOrientation.RotationDegrees);
        return new Viewpoint(new Envelope(center, extent.Width, extent.Height), viewRotation);
    }

    // Clockwise degrees, summed without clamping: real data exceeds the spec's +-90 roll.
    private static double GetEffectiveRotationDegrees(OrientedImage image)
    {
        double roll = ReadRotationAttribute(image, "CameraRoll");
        double imageRotation = ReadRotationAttribute(image, "ImageRotation");
        return roll + imageRotation;
    }

    // CameraRoll and ImageRotation are esriFieldTypeDouble, so only a boxed double is accepted.
    private static double ReadRotationAttribute(OrientedImage image, string name)
    {
        if (image.Attributes.TryGetValue(name, out object? raw) && raw is double value && !double.IsNaN(value))
            return value;
        return 0d;
    }

    // Cell sizes can be negative for a flipped axis. An unknown size of zero counts as one unit.
    private static double CellSize(double size) => size == 0 ? 1 : Math.Abs(size);

    // Maps a pixel in OrientedImage's oriented space to the stored raster's map space. Unless onImage is set, a pixel
    // within a wide margin past the edge still maps, so a marker just off the image draws.
    private MapPoint? PixelToMap(PointF pixel, bool onImage = false)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = CellSize(info.CellSizeX);
        double cellY = CellSize(info.CellSizeY);
        double width = extent.Width / cellX;
        double height = extent.Height / cellY;
        pixel = _imageOrientation.ImageToStored(pixel, width, height);

        // Drop wildly off-image pixels, such as the camera's own location projected onto its image.
        bool placeable = onImage
            ? pixel.X >= 0 && pixel.X <= width && pixel.Y >= 0 && pixel.Y <= height
            : IsPlaceablePixel(pixel.X, width) && IsPlaceablePixel(pixel.Y, height);
        if (!placeable)
            return null;

        double x = extent.XMin + (pixel.X * cellX);
        double y = extent.YMax - (pixel.Y * cellY);
        return new MapPoint(x, y, extent.SpatialReference);
    }

    private static bool IsPlaceablePixel(double value, double max)
    {
        if (!double.IsFinite(value))
            return false;

        const double MarkerPlacementMarginFactor = 100d;
        double margin = Math.Max(Math.Abs(max), 1d) * MarkerPlacementMarginFactor;
        return value >= -margin && value <= max + margin;
    }

    private PointF? MapToPixel(MapPoint mapPoint)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = CellSize(info.CellSizeX);
        double cellY = CellSize(info.CellSizeY);
        double width = extent.Width / cellX;
        double height = extent.Height / cellY;
        double col = (mapPoint.X - extent.XMin) / cellX;
        double row = (extent.YMax - mapPoint.Y) / cellY;
        if (col < 0 || row < 0 || col > width || row > height)
            return null;

        return _imageOrientation.StoredToImage(new PointF((float)col, (float)row), width, height);
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

        // The image may have changed while identifying, leaving the pixel in the old image's space.
        if (token.IsCancellationRequested)
            return;

        RaiseImageTapped(new OrientedImageTappedEventArgs(imagePoint, image, marker));
    }

    private void OnViewpointChanged(object? sender, EventArgs e) => UpdateFootprint();

    protected override Task? BeginFootprintUpdate(OrientedImageFootprint footprint)
    {
        if (!_hasDrawn)
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

    // Clips the visible area to the image as a polygon, because clamping each vertex collapses a rotated view that
    // encloses the whole image to a diamond. Internal for unit tests.
    internal static List<PointF> ComputeVisibleAreaPixels(Polygon visibleArea, Envelope extent, double cellSizeX, double cellSizeY, ExifOrientationTransform orientation = default)
    {
        double cellX = CellSize(cellSizeX);
        double cellY = CellSize(cellSizeY);
        double maxCol = extent.Width / cellX;
        double maxRow = extent.Height / cellY;

        // Clip in raster coordinates; EXIF can swap axes or reflect the pixel grid.
        // The raster extent has no spatial reference but the visible area does, and Intersection wants identical SRs.
        var firstRing = new Polygon(visibleArea.Parts[0].Points, visibleArea.SpatialReference);
        var bounds = new Envelope(extent.XMin, extent.YMin, extent.XMax, extent.YMax, visibleArea.SpatialReference);
        var clipped = (Polygon)GeometryEngine.Intersection(firstRing, bounds);
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
