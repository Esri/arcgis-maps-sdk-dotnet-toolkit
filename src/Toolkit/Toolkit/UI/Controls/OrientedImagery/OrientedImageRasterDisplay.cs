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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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

// Raster inner display for OrientedImageDisplay: hosts a MapView showing the oriented image as a raster layer,
// renders markers, and (when enabled) recomputes the visible footprint corners as the viewport changes.
#if MAUI
internal sealed partial class OrientedImageRasterDisplay : ContentView, IOrientedImageDisplay
#else
internal sealed partial class OrientedImageRasterDisplay : ContentControl, IOrientedImageDisplay
#endif
{
    private readonly MapView _mapView;
    private readonly GraphicsOverlay _markersOverlay;

    private const double MarkerHitTolerance = 12d;

    private OrientedImageFootprint? _footprint;
    private RasterLayer? _rasterLayer;
    private ObservableCollection<OrientedImageMarker>? _markers;
    private WeakEventListener<OrientedImageRasterDisplay, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>? _markersListener;
    private readonly Dictionary<OrientedImageMarker, Graphic> _markerGraphics = [];
    private readonly Dictionary<Graphic, OrientedImageMarker> _graphicMarkers = [];
    private bool _autoUpdate;
    private CancellationTokenSource? _updateCts;
    private bool _isLoading;
    private bool _interactive;
    private int _footprintGeneration;

    public bool IsBusy { get; private set; }

    public bool IsInteractive { get; private set; }

    public Exception? Error { get; private set; }

    public event EventHandler? StateChanged;

    public event EventHandler<OrientedImageDisplay.ImageClickedEventArgs>? ImageClicked;

    internal OrientedImageRasterDisplay()
    {
        // Lock map interaction until the image has loaded
        _mapView = new MapView { IsAttributionTextVisible = false, InteractionOptions = new MapViewInteractionOptions { IsEnabled = false } };
#if MAUI
        _mapView.HorizontalOptions = LayoutOptions.Fill;
        _mapView.VerticalOptions = LayoutOptions.Fill;
#else
        // ContentControl content defaults to Left/Top; MapView has to be stretched to fill.
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        // The inner MapView is the focusable element
        IsTabStop = false;
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
        _mapView.DrawStatusChanged += (s, e) => UpdateState();
        Content = _mapView;
        UpdateAutomationName();
    }

    // Resolves the display's state from its sources and raises StateChanged when it changes.
    // IsBusy means "loading or drawing"; IsInteractive means "image loaded, map unlocked, no error".
    // A loaded raster stays interactive while it redraws during a pan.
    // Error aggregates the image load error, the raster layer load error, and the layer's view-state error.
    private void UpdateState()
    {
        // A MapView with no Map sits at DrawStatus.InProgress forever, so only count drawing when there's a map.
        bool busy = _isLoading || (_mapView.Map is not null && _mapView.DrawStatus == DrawStatus.InProgress);
        Exception? error = ResolveError();
        bool interactive = _interactive && error is null;
        if (busy == IsBusy && interactive == IsInteractive && ReferenceEquals(error, Error))
            return;

        IsBusy = busy;
        IsInteractive = interactive;
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

            // GetLayerViewState throws if the layer isn't in the current map (can happen during a map swap).
            if (_mapView.Map?.OperationalLayers.Contains(layer) == true &&
                _mapView.GetLayerViewState(layer)?.Error is Exception viewError)
                return viewError;
        }

        return null;
    }

    public void SetFootprint(OrientedImageFootprint? footprint)
    {
        _ = SetFootprintAsync(footprint);
    }

    private async Task SetFootprintAsync(OrientedImageFootprint? footprint)
    {
        // Thread safety warning! This async-method is re-entrant (can be called again before we're done with first call).
        // Use a generation counter to ignore stale completions.
        int generation = Interlocked.Increment(ref _footprintGeneration);
        OrientedImage? image = footprint?.OrientedImage;

        // If we are replacing a still-loading image, cancel it. It's no-op if already loaded.
        if (!ReferenceEquals(_footprint?.OrientedImage, image))
            _footprint?.OrientedImage?.CancelLoad();
        _rasterLayer?.CancelLoad();

        _footprint = footprint;
        UpdateAutomationName();
        if (image is null)
        {
            // We were given a null footprint; clear the view.
            _mapView.Map = null;
            _rasterLayer = null;
            _isLoading = false;
            SetInteractive(false);
            UpdateState();
            return;
        }

        // Lock the view while loading so the user can't accidentally pan/zoom away from the incoming image.
        SetInteractive(false);
        _isLoading = true;
        UpdateState();
        try
        {
            // The image resolves its DataUri during load (downloads the image file or first attachment).
            await image.RetryLoadAsync();
            if (generation != _footprintGeneration)
                return;

            if (image.DataUri is not Uri uri)
            {
                // Loaded with nothing displayable (attachment without image, MRF/UNC/video, or a load failure
                // surfaced via Error). Clear the view; the finally clause resets the loading flag.
                _mapView.Map = null;
                _rasterLayer = null;
                return;
            }

            RasterLayer layer = new(CreateRaster(uri));
            layer.ResamplingType = RasterResamplingType.BilinearInterpolation;
            Map map = new();
            map.OperationalLayers.Add(layer);
            _mapView.Map = map;
            _rasterLayer = layer;
            await layer.LoadAsync();
            if (generation != _footprintGeneration)
                return;

            if (layer.Raster?.RasterInfo?.Extent is Envelope extent)
            {
                // The effective rotation formula (from ArcGIS JS API) is clockwise,
                // but MapView rotation is counter-clockwise; negate it.
                // Only the view (not the raster) rotates, so marker placement and hit-testing stays in native pixel space.
                double viewRotation = -GetEffectiveRotationDegrees(image);
                try
                {
                    // Zoom and rotate in one quick animation-free viewpoint set. The extent may have a null spatial
                    // reference (a plain image map has none); the Viewpoint constructor accepts that.
                    _mapView.SetViewpoint(new Viewpoint(extent, viewRotation));
                }
                catch
                {
                    // Framing is best-effort; never let it block the unlock below.
                }

                if (generation != _footprintGeneration)
                    return;

                // Markers set before the raster loaded can now be placed (the pixel-map transform exists).
                _ = RefreshMarkerGeometriesAsync();
            }

            // Unlock once the raster has loaded.
            SetInteractive(true);
        }
        catch
        {
            // The image URI or raster may be unreachable; failures surface via Error.
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
        // Empty restores the MapView's default grid (gray with black grid lines).
        // Otherwise sets it to a solid color (no grid lines).
        _mapView.BackgroundGrid = color.IsEmpty
            ? new BackgroundGrid()
            : new BackgroundGrid(color, System.Drawing.Color.Transparent, 0f, 16f);
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

    // Locks/unlocks MapView user interaction (programmatic SetViewpoint still works while locked).
    // Replace the whole MapViewInteractionOptions object (an in-place IsEnabled flip would be ignored).
    // Tracks the enabled state in _interactive; each caller follows this with UpdateState, which surfaces it as IsInteractive.
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

    // Rebuilds the marker-to-graphic mapping from scratch and observes each marker for live updates.
    // Snapshots the app-owned collection on the calling thread (so it's never enumerated off its mutating thread),
    // then does the overlay/dictionary work on the UI thread.
    private void RebuildMarkers()
    {
        List<OrientedImageMarker>? snapshot = _markers is null ? null : new(_markers);
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
                    Graphic graphic = new() { Symbol = marker.Symbol, IsVisible = marker.IsVisible };
                    _markerGraphics[marker] = graphic;
                    _graphicMarkers[graphic] = marker;
                    _markersOverlay.Graphics.Add(graphic);
                    marker.PropertyChanged += OnMarkerPropertyChanged;
                }
            }

            _ = RefreshMarkerGeometriesAsync();
        });
    }

    // Dispatch so marker updates can't touch the graphic/dictionaries off the UI thread.
    private void OnMarkerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.Dispatch(() =>
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
    }

    // Recomputes the map-space geometry for every current marker.
    // Called after the raster loads (so markers set before load get placed) and whenever the marker set changes.
    private async Task RefreshMarkerGeometriesAsync()
    {
        var markerGraphicsSnapshot = new Dictionary<OrientedImageMarker, Graphic>(_markerGraphics);
        foreach (KeyValuePair<OrientedImageMarker, Graphic> pair in markerGraphicsSnapshot)
        {
            await ResolveAndApplyMarkerGeometryAsync(pair.Key, pair.Value);
        }
    }

    private async Task ResolveAndApplyMarkerGeometryAsync(OrientedImageMarker marker, Graphic graphic)
    {
        MapPoint? mapPoint = await ResolveMarkerMapPointAsync(marker);
        // The marker set may have changed while awaiting; only apply if this graphic is still the marker's graphic.
        // A null point (unprojectable location, see PixelToMap) clears the geometry.
        if (_markerGraphics.TryGetValue(marker, out Graphic? current) && ReferenceEquals(current, graphic))
            graphic.Geometry = mapPoint;
    }

    // Resolves a marker to display map space: image-anchored uses its pixel directly,
    // world-anchored projects to a pixel via the camera model first; both then map pixel->map via the raster extent.
    // Null if the raster isn't ready.
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

    // Effective in-plane display rotation (degrees, clockwise+):
    // CameraRoll + ImageRotation (from image attributes) + the JPEG's EXIF orientation.
    // Summed without clamping because real data exceeds spec's "+-90 degrees".
    private static double GetEffectiveRotationDegrees(OrientedImage image)
    {
        double roll = ReadRotationAttribute(image, "CameraRoll");
        double imageRotation = ReadRotationAttribute(image, "ImageRotation");
        return roll + imageRotation + ReadExifRotationDegrees(image.DataUri);
    }

    // Reads a rotation field (degrees) from the image attributes; absent/null/non-double/NaN reads as 0.
    // CameraRoll and ImageRotation are esriFieldTypeDouble, which surface as a boxed double, so no conversion is needed.
    private static double ReadRotationAttribute(OrientedImage image, string name)
    {
        if (image.Attributes.TryGetValue(name, out object? raw) && raw is double value && !double.IsNaN(value))
            return value;
        return 0d;
    }

    // Reads the downloaded image file's EXIF Orientation
    // and returns the associated clockwise display rotation (0/90/180/270).
    // Also returns 0 for a remote/non-file URI, a non-JPEG, missing EXIF, normal, or a mirror-only orientation.
    private static double ReadExifRotationDegrees(Uri? dataUri)
    {
        if (dataUri is null || !dataUri.IsFile)
            return 0;

        // Minimal metadata parser inspired by https://stackoverflow.com/q/7584794/383361
        try
        {
            // Core keeps the image file open, so open shared to avoid a sharing violation on this best-effort read.
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

    // Maps an image pixel to a point in the display's map space (the inverse of ComputeCorners).
    private MapPoint? PixelToMap(PointF pixel)
    {
        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
            return null;

        double cellX = info.CellSizeX == 0 ? 1 : Math.Abs(info.CellSizeX);
        double cellY = info.CellSizeY == 0 ? 1 : Math.Abs(info.CellSizeY);

        // Drop a non-finite or wildly off-image pixels (e.g. projecting camera's own location to itself)
        if (!IsPlaceablePixel(pixel.X, extent.Width / cellX) || !IsPlaceablePixel(pixel.Y, extent.Height / cellY))
            return null;

        double x = extent.XMin + (pixel.X * cellX);
        double y = extent.YMax - (pixel.Y * cellY);
        return new MapPoint(x, y, extent.SpatialReference);
    }

    // Placeable if finite and within a generous margin of the image bounds
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

        double cellX = info.CellSizeX == 0 ? 1 : Math.Abs(info.CellSizeX);
        double cellY = info.CellSizeY == 0 ? 1 : Math.Abs(info.CellSizeY);
        double col = (mapPoint.X - extent.XMin) / cellX;
        double row = (extent.YMax - mapPoint.Y) / cellY;
        return new PointF((float)col, (float)row);
    }

    private async void OnMapViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        // Every tap on the image raises ImageClicked with the pixel populated.
        // Clicking a marker still counts as an image click, but also puts the hit marker into the event args.
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
            // Identify can fail during map teardown. Report the image click without a marker.
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
        SemanticProperties.SetDescription(_mapView, name);
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

        List<PointF> corners = ComputeVisibleAreaPixels(visibleArea, extent, info.CellSizeX, info.CellSizeY);
        if (corners.Count < 3)
            return;

        _updateCts?.Cancel();
        CancellationTokenSource cts = new();
        _updateCts = cts;
        try
        {
            await _footprint.UpdateFootprintAsync(corners, cts.Token);
        }
        catch
        {
            // Ignore cancellation/failures from a superseded update.
        }
    }

    // Converts the map-space visible-area ring into an ordered list of image pixel vertices. The updated
    // OrientedImageFootprint API takes the full vertex list (not a fixed quad), so a rotated or clipped
    // visible area is passed through as-is instead of being reduced to four axis-classified corners.
    private static List<PointF> ComputeVisibleAreaPixels(Polygon visibleArea, Envelope extent, double cellSizeX, double cellSizeY)
    {
        double cellX = cellSizeX == 0 ? 1 : Math.Abs(cellSizeX);
        double cellY = cellSizeY == 0 ? 1 : Math.Abs(cellSizeY);
        double maxCol = extent.Width / cellX;
        double maxRow = extent.Height / cellY;

        IReadOnlyList<MapPoint> points = visibleArea.Parts[0].Points;
        var pixels = new List<PointF>(points.Count);
        foreach (MapPoint point in points)
        {
            double col = Math.Clamp((point.X - extent.XMin) / cellX, 0, maxCol);
            double row = Math.Clamp((extent.YMax - point.Y) / cellY, 0, maxRow);
            pixels.Add(new PointF((float)col, (float)row));
        }

        return pixels;
    }
}
