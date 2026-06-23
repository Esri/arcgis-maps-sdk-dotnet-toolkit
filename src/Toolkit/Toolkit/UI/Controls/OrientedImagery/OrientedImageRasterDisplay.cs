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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
#endif

// Disambiguate from Microsoft.Maui.Graphics.PointF (a MAUI global using); OrientedImagePixelCorners uses System.Drawing.PointF.
using PointF = System.Drawing.PointF;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// Raster inner display for OrientedImageDisplay: hosts a MapView showing the oriented image as a raster layer,
// renders markers, and (when enabled) recomputes the visible footprint corners as the viewport changes.
#if MAUI
internal partial class OrientedImageRasterDisplay : ContentView, IOrientedImageDisplay
#else
internal partial class OrientedImageRasterDisplay : ContentControl, IOrientedImageDisplay
#endif
{
    private readonly MapView _mapView;
    private readonly GraphicsOverlay _markersOverlay;

    private const double MarkerHitTolerance = 12d;

    private OrientedImageFootprint? _footprint;
    private RasterLayer? _rasterLayer;
    private ObservableCollection<OrientedImageMarker>? _markers;
    private WeakEventListener<OrientedImageRasterDisplay, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>? _markersListener;
    private readonly Dictionary<OrientedImageMarker, Graphic> _markerGraphics = new();
    private readonly Dictionary<Graphic, OrientedImageMarker> _graphicMarkers = new();
    private bool _autoUpdate;
    private CancellationTokenSource? _updateCts;
    private bool _isLoading;
    private int _footprintGeneration;

    public bool IsActive { get; private set; }

    public Exception? Error { get; private set; }

    public event EventHandler? StateChanged;

    public event EventHandler<OrientedImageDisplay.ImageClickedEventArgs>? ImageClicked;

    internal OrientedImageRasterDisplay()
    {
        // Not a tab stop; the inner MapView is the focusable element. (MAUI containers aren't tab stops by default.)
#if !MAUI
        IsTabStop = false;
        // ContentControl content defaults to Left/Top (notably on WinUI), leaving the MapView unsized; stretch it to fill.
#if WPF
        HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
        VerticalContentAlignment = System.Windows.VerticalAlignment.Stretch;
#else
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
#endif
#endif

        _mapView = new MapView { IsAttributionTextVisible = false };
#if MAUI
        _mapView.HorizontalOptions = LayoutOptions.Fill;
        _mapView.VerticalOptions = LayoutOptions.Fill;
#endif

        // Default symbol for markers without their own; a marker's own Symbol overrides this renderer.
        _markersOverlay = new GraphicsOverlay
        {
            Renderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(255, 0, 122, 194), 10)),
        };
        if (_mapView.GraphicsOverlays == null)
            _mapView.GraphicsOverlays = new GraphicsOverlayCollection();

        _mapView.GraphicsOverlays.Add(_markersOverlay);
        _mapView.GeoViewTapped += OnMapViewTapped;
        _mapView.LayerViewStateChanged += (s, e) => UpdateState();
        _mapView.DrawStatusChanged += (s, e) => UpdateState();
        Content = _mapView;
        UpdateAutomationName();
    }

    // Resolves the display's state from its sources and raises StateChanged when it changes. IsActive is "loading or
    // drawing"; Error aggregates the image load error, the raster layer load error, and the layer's view-state error.
    private void UpdateState()
    {
        // A MapView with no Map sits at DrawStatus.InProgress forever, so only count drawing when there's a map.
        bool active = _isLoading || (_mapView.Map is not null && _mapView.DrawStatus == DrawStatus.InProgress);
        Exception? error = ResolveError();
        if (active == IsActive && ReferenceEquals(error, Error))
            return;

        IsActive = active;
        Error = error;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private Exception? ResolveError()
    {
        if (_footprint?.OrientedImage?.LoadError is Exception imageError)
            return imageError;

        if (_rasterLayer is RasterLayer layer)
        {
            if (layer.LoadError is Exception layerError)
                return layerError;

            // GetLayerViewState throws if the layer isn't in the current map (can happen transiently during a map swap).
            if (_mapView.Map?.OperationalLayers.Contains(layer) == true &&
                _mapView.GetLayerViewState(layer)?.Error is Exception viewError)
                return viewError;
        }

        return null;
    }

    public async void SetFootprint(OrientedImageFootprint? footprint)
    {
        // Re-entrant (async void): stamp this call so a superseded one bails after each await instead of clobbering state.
        int generation = ++_footprintGeneration;
        _footprint = footprint;
        UpdateAutomationName();
        OrientedImage? image = footprint?.OrientedImage;
        if (image?.DataUri is not Uri uri)
        {
            _mapView.Map = null;
            _rasterLayer = null;
            _isLoading = false;
            UpdateState();
            return;
        }

        _isLoading = true;
        UpdateState();
        try
        {
            await image.LoadAsync();
            if (generation != _footprintGeneration)
                return;

            RasterLayer layer = new RasterLayer(CreateRaster(uri));
            Map map = new Map();
            map.OperationalLayers.Add(layer);
            _mapView.Map = map;
            _rasterLayer = layer;
            await layer.LoadAsync();
            if (generation != _footprintGeneration)
                return;

            if (layer.Raster?.RasterInfo?.Extent is Envelope extent)
            {
                await _mapView.SetViewpointGeometryAsync(extent);
                if (generation != _footprintGeneration)
                    return;

                // Markers set before the raster loaded can now be placed (the pixel-map transform exists).
                _ = RefreshMarkerGeometriesAsync();
            }
        }
        catch
        {
            // OrientedImage is a no-op skeleton and the URI may be unreachable; failures surface via Error.
        }
        finally
        {
            // Don't let a superseded call clear the loading flag a newer one set.
            if (generation == _footprintGeneration)
            {
                _isLoading = false;
                UpdateState();
            }
        }
    }

    public void SetMarkers(ObservableCollection<OrientedImageMarker>? markers)
    {
        if (ReferenceEquals(_markers, markers))
            return;

        _markersListener?.Detach();
        _markersListener = null;
        _markers = markers;
        RebuildMarkers();

        if (markers is INotifyCollectionChanged incc)
        {
            _markersListener = new WeakEventListener<OrientedImageRasterDisplay, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, incc)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.RebuildMarkers(),
                OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
            };
            incc.CollectionChanged += _markersListener.OnEvent;
        }
    }

    public void SetBackgroundColor(System.Drawing.Color color)
    {
        // Empty keeps the MapView's default grid; otherwise a solid color (no grid lines).
        if (!color.IsEmpty)
            _mapView.BackgroundGrid = new BackgroundGrid(color, System.Drawing.Color.Transparent, 0f, 16f);
    }

    public void SetAutoUpdateFootprint(bool enabled)
    {
        if (enabled == _autoUpdate)
            return;

        _autoUpdate = enabled;
        if (enabled)
            _mapView.ViewpointChanged += OnViewpointChanged;
        else
            _mapView.ViewpointChanged -= OnViewpointChanged;
    }

    private static Raster CreateRaster(Uri uri)
    {
        bool isHttp = uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (isHttp)
            return new ImageServiceRaster(uri);

        return new Raster(uri.IsAbsoluteUri && uri.IsFile ? uri.LocalPath : uri.OriginalString);
    }

    // Rebuilds the marker-to-graphic mapping from scratch and observes each marker for live updates. Snapshots the
    // app-owned collection on the calling thread (so it's never enumerated off its mutating thread), then does the
    // UI-affine overlay/dictionary work via Dispatch (inline when already on the UI thread).
    private void RebuildMarkers()
    {
        List<OrientedImageMarker>? snapshot = _markers is null ? null : new List<OrientedImageMarker>(_markers);
        this.Dispatch(() =>
        {
            foreach (OrientedImageMarker existing in _markerGraphics.Keys)
            {
                existing.PropertyChanged -= OnMarkerPropertyChanged;
            }

            _markerGraphics.Clear();
            _graphicMarkers.Clear();
            _markersOverlay.Graphics.Clear();

            if (snapshot != null)
            {
                foreach (OrientedImageMarker marker in snapshot)
                {
                    Graphic graphic = new Graphic { Symbol = marker.Symbol, IsVisible = marker.IsVisible };
                    _markerGraphics[marker] = graphic;
                    _graphicMarkers[graphic] = marker;
                    _markersOverlay.Graphics.Add(graphic);
                    marker.PropertyChanged += OnMarkerPropertyChanged;
                }
            }

            _ = RefreshMarkerGeometriesAsync();
        });
    }

    // Dispatch so an off-thread marker update doesn't touch the graphic/dictionaries off-thread; the value is read at
    // apply time, so with FIFO dispatch the latest change wins.
    private void OnMarkerPropertyChanged(object? sender, PropertyChangedEventArgs e) => this.Dispatch(() =>
    {
        if (sender is not OrientedImageMarker marker || !_markerGraphics.TryGetValue(marker, out Graphic? graphic))
            return;

        switch (e.PropertyName)
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

    // Recomputes the map-space geometry for every current marker. Called after the raster loads (so markers set
    // before load get placed) and whenever the marker set changes.
    private async Task RefreshMarkerGeometriesAsync()
    {
        foreach (KeyValuePair<OrientedImageMarker, Graphic> pair in _markerGraphics)
        {
            await ResolveAndApplyMarkerGeometryAsync(pair.Key, pair.Value);
        }
    }

    private async Task ResolveAndApplyMarkerGeometryAsync(OrientedImageMarker marker, Graphic graphic)
    {
        MapPoint? mapPoint = await ResolveMarkerMapPointAsync(marker);

        // The marker set may have changed while awaiting; only apply if this graphic is still the marker's graphic.
        if (_markerGraphics.TryGetValue(marker, out Graphic? current) && ReferenceEquals(current, graphic))
            graphic.Geometry = mapPoint;
    }

    // Resolves a marker to display map space: image-anchored uses its pixel directly, world-anchored projects to a pixel
    // via the camera model first; both then map pixel->map via the raster extent. Null if the raster isn't ready.
    private async Task<MapPoint?> ResolveMarkerMapPointAsync(OrientedImageMarker marker)
    {
        OrientedImageMarkerPosition position = marker.Position;
        PointF pixel;
        if (position.ImagePoint is PointF imagePoint)
        {
            pixel = imagePoint;
        }
        else if (position.Location is MapPoint location && _footprint?.OrientedImage is OrientedImage image)
        {
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

    // Maps an image pixel to a point in the display's map space (the inverse of the ComputeCorners classification).
    private MapPoint? PixelToMap(PointF pixel)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = info.CellSizeX == 0 ? 1 : Math.Abs(info.CellSizeX);
        double cellY = info.CellSizeY == 0 ? 1 : Math.Abs(info.CellSizeY);
        double x = extent.XMin + (pixel.X * cellX);
        double y = extent.YMax - (pixel.Y * cellY);
        return new MapPoint(x, y, extent.SpatialReference);
    }

    // Maps a point in the display's map space back to an image pixel (to report ImageClicked in image coordinates).
    private PointF? MapToPixel(MapPoint mapPoint)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = info.CellSizeX == 0 ? 1 : Math.Abs(info.CellSizeX);
        double cellY = info.CellSizeY == 0 ? 1 : Math.Abs(info.CellSizeY);
        double col = (mapPoint.X - extent.XMin) / cellX;
        double row = (extent.YMax - mapPoint.Y) / cellY;
        return new PointF((float)col, (float)row);
    }

    private async void OnMapViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        // Every tap on the image raises ImageClicked with the pixel populated; a hit marker (if any) rides on the args,
        // so a marker's hit buffer never suppresses the underlying image click.
        if (e.Location is not MapPoint location || _footprint?.OrientedImage is not OrientedImage image || MapToPixel(location) is not PointF imagePoint)
            return;

        OrientedImageMarker? marker = null;
        try
        {
            var result = await _mapView.IdentifyGraphicsOverlayAsync(_markersOverlay, e.Position, MarkerHitTolerance, false, 1);
            if (result.Graphics.Count > 0)
                _graphicMarkers.TryGetValue(result.Graphics[0], out marker);
        }
        catch
        {
            // Identify can fail transiently (e.g. during map teardown); report the image click without a marker.
        }

        ImageClicked?.Invoke(this, new OrientedImageDisplay.ImageClickedEventArgs(imagePoint, image, marker));
    }

    // Gives the focusable MapView a meaningful screen-reader label instead of a generic "map".
    private void UpdateAutomationName()
    {
        string name = _footprint?.OrientedImage?.Type is OrientedImageType type
            ? string.Format(Properties.Resources.GetString("OrientedImageDisplayImageAutomationNameFormat") ?? "Oriented image, {0}", type)
            : Properties.Resources.GetString("OrientedImageDisplayAutomationName") ?? "Oriented image display";
#if WPF
        System.Windows.Automation.AutomationProperties.SetName(_mapView, name);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_mapView, name);
#elif MAUI
        Microsoft.Maui.Controls.SemanticProperties.SetDescription(_mapView, name);
#endif
    }

    private void OnViewpointChanged(object? sender, EventArgs e) => UpdateFootprintCorners();

    private async void UpdateFootprintCorners()
    {
        if (!_autoUpdate || _footprint is null)
            return;

        if (_mapView.DrawStatus != DrawStatus.Completed)
            return;

        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return;

        if (_mapView.VisibleArea is not Polygon visibleArea || visibleArea.Parts.Count == 0)
            return;

        OrientedImagePixelCorners corners = ComputeCorners(visibleArea, extent, info.CellSizeX, info.CellSizeY);

        _updateCts?.Cancel();
        CancellationTokenSource cts = new CancellationTokenSource();
        _updateCts = cts;
        try
        {
            await _footprint.UpdateFootprintAsync(corners, cts.Token);
        }
        catch
        {
            // Skeleton UpdateAsync is a no-op; ignore cancellation/failures.
        }
    }

    // Converts the map-space visible-area quadrilateral into image pixel corners, classified by sum/difference so the
    // result survives view rotation.
    private static OrientedImagePixelCorners ComputeCorners(Polygon visibleArea, Envelope extent, double cellSizeX, double cellSizeY)
    {
        double cellX = cellSizeX == 0 ? 1 : Math.Abs(cellSizeX);
        double cellY = cellSizeY == 0 ? 1 : Math.Abs(cellSizeY);
        double maxCol = extent.Width / cellX;
        double maxRow = extent.Height / cellY;

        PointF topLeft = default, topRight = default, bottomRight = default, bottomLeft = default;
        double minSum = 0, maxSum = 0, maxDiff = 0, minDiff = 0;
        bool any = false;

        foreach (MapPoint point in visibleArea.Parts[0].Points)
        {
            double col = Math.Clamp((point.X - extent.XMin) / cellX, 0, maxCol);
            double row = Math.Clamp((extent.YMax - point.Y) / cellY, 0, maxRow);
            PointF pixel = new PointF((float)col, (float)row);
            double sum = col + row;
            double diff = col - row;

            if (!any || sum < minSum)
            {
                minSum = sum;
                topLeft = pixel;
            }

            if (!any || sum > maxSum)
            {
                maxSum = sum;
                bottomRight = pixel;
            }

            if (!any || diff > maxDiff)
            {
                maxDiff = diff;
                topRight = pixel;
            }

            if (!any || diff < minDiff)
            {
                minDiff = diff;
                bottomLeft = pixel;
            }

            any = true;
        }

        return new OrientedImagePixelCorners(topLeft, topRight, bottomRight, bottomLeft);
    }
}
