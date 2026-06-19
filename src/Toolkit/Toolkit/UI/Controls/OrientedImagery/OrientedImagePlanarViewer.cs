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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
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

/// <summary>
/// The planar inner viewer used by <see cref="OrientedImageDisplay"/> for non-panoramic, non-video imagery. It hosts
/// a <c>MapView</c> showing the oriented image as a raster layer, renders marker graphics, and (when enabled)
/// recomputes the visible image corners as the viewport changes.
/// </summary>
internal partial class OrientedImagePlanarViewer
{
    private readonly OrientedImageDisplay _owner;
    private readonly MapView _mapView;
    private readonly GraphicsOverlay _markersOverlay;

    private OrientedImageFootprint? _footprint;
    private RasterLayer? _rasterLayer;
    private ObservableCollection<Graphic>? _markers;
    private WeakEventListener<OrientedImagePlanarViewer, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>? _markersListener;
    private bool _autoUpdate;
    private CancellationTokenSource? _updateCts;

    internal OrientedImagePlanarViewer(OrientedImageDisplay owner)
    {
        _owner = owner;

        // This is an internal presentation host; it should not be a tab stop. The inner MapView (which provides
        // keyboard navigation) is the focusable element, so Tab lands directly on the map. (MAUI layout containers
        // are not tab stops by default, and VisualElement has no IsTabStop.)
#if !MAUI
        IsTabStop = false;
#endif

        _mapView = new MapView { IsAttributionTextVisible = false };
#if MAUI
        _mapView.HorizontalOptions = LayoutOptions.Fill;
        _mapView.VerticalOptions = LayoutOptions.Fill;
#endif
        _markersOverlay = new GraphicsOverlay();
        if (_mapView.GraphicsOverlays == null)
        {
            _mapView.GraphicsOverlays = new GraphicsOverlayCollection();
        }

        _mapView.GraphicsOverlays.Add(_markersOverlay);
        _mapView.GeoViewTapped += OnMapViewTapped;
        _mapView.LayerViewStateChanged += OnLayerViewStateChanged;
        Content = _mapView;
        UpdateAutomationName();
    }

    // Diagnostic logging for "nothing shows in the MapView". LayerViewStateChanged is the authoritative source for
    // why a layer is/ isn't drawing (NotVisible / OutOfScale / Error / Loading). Remove once the planar path is solid.
    private const string LogTag = "[OrientedImagePlanarViewer]";

    private static void Log(string message) => System.Diagnostics.Debug.WriteLine($"{LogTag} {message}");

    private void OnLayerViewStateChanged(object? sender, LayerViewStateChangedEventArgs e)
    {
        string layerName = string.IsNullOrEmpty(e.Layer?.Name) ? e.Layer?.GetType().Name ?? "<null>" : e.Layer!.Name;
        Log($"LayerViewState '{layerName}' => {e.LayerViewState.Status}" +
            (e.LayerViewState.Error is Exception err ? $" ERROR: {err.GetType().Name}: {err.Message}" : string.Empty));
    }

    /// <summary>
    /// Sets the footprint whose oriented image should be displayed, loading and showing its raster.
    /// </summary>
    internal async void SetFootprint(OrientedImageFootprint? footprint)
    {
        _footprint = footprint;
        UpdateAutomationName();
        OrientedImage? image = footprint?.OrientedImage;
        if (image?.Uri is not Uri uri)
        {
            Log("SetFootprint: no image/URI; clearing map.");
            _mapView.Map = null;
            _rasterLayer = null;
            return;
        }

        try
        {
            Log($"SetFootprint: loading image, URI='{uri}'");
            await image.LoadAsync();
            Raster raster = CreateRaster(uri);
            Log($"SetFootprint: raster created ({raster.GetType().Name}); loading layer.");
            RasterLayer layer = new RasterLayer(raster);
            Map map = new Map();
            map.OperationalLayers.Add(layer);
            _mapView.Map = map;
            _rasterLayer = layer;
            await layer.LoadAsync();
            Log($"SetFootprint: layer LoadStatus={layer.LoadStatus}, Extent={layer.Raster?.RasterInfo?.Extent?.ToString() ?? "<null>"}, SR={layer.SpatialReference?.Wkid.ToString() ?? "<null>"}");
            if (layer.LoadStatus != LoadStatus.Loaded && layer.LoadError is Exception loadErr)
            {
                Log($"SetFootprint: layer load FAILED: {loadErr.GetType().Name}: {loadErr.Message}");
            }

            if (layer.Raster?.RasterInfo?.Extent is Envelope extent)
            {
                await _mapView.SetViewpointGeometryAsync(extent);
                Log($"SetFootprint: viewpoint set to extent; map SR={_mapView.SpatialReference?.Wkid.ToString() ?? "<null>"}");
            }
            else
            {
                Log("SetFootprint: layer has no RasterInfo.Extent; cannot set viewpoint (nothing to frame).");
            }
        }
        catch (Exception ex)
        {
            // Skeleton: OrientedImage is a no-op and the URI may be unreachable; log but don't crash the host.
            Log($"SetFootprint: exception: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the marker graphics rendered over the image and tracks collection changes.
    /// </summary>
    internal void SetMarkers(ObservableCollection<Graphic>? markers)
    {
        if (ReferenceEquals(_markers, markers))
        {
            return;
        }

        _markersListener?.Detach();
        _markersListener = null;
        _markers = markers;
        RebuildMarkerGraphics();

        if (markers is INotifyCollectionChanged incc)
        {
            _markersListener = new WeakEventListener<OrientedImagePlanarViewer, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, incc)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.RebuildMarkerGraphics(),
                OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
            };
            incc.CollectionChanged += _markersListener.OnEvent;
        }
    }

    /// <summary>
    /// Enables or disables automatic recomputation of the footprint corners on viewport change.
    /// </summary>
    internal void SetAutoUpdateFootprint(bool enabled)
    {
        if (enabled == _autoUpdate)
        {
            return;
        }

        _autoUpdate = enabled;
        if (enabled)
        {
            _mapView.ViewpointChanged += OnViewpointChanged;
        }
        else
        {
            _mapView.ViewpointChanged -= OnViewpointChanged;
        }
    }

    private static Raster CreateRaster(Uri uri)
    {
        bool isHttp = uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (isHttp)
        {
            return new ImageServiceRaster(uri);
        }

        return new Raster(uri.IsAbsoluteUri && uri.IsFile ? uri.LocalPath : uri.OriginalString);
    }

    private void RebuildMarkerGraphics()
    {
        _markersOverlay.Graphics.Clear();
        if (_markers != null)
        {
            foreach (Graphic graphic in _markers)
            {
                _markersOverlay.Graphics.Add(graphic);
            }
        }
    }

    private void OnMapViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        if (e.Location is MapPoint location)
        {
            _owner.OnImageClicked(location);
        }
    }

    // Gives the focusable MapView a meaningful screen-reader label instead of a generic "map".
    private void UpdateAutomationName()
    {
        string name = _footprint?.OrientedImage?.OrientedImageType is OrientedImageType type
            ? string.Format(Properties.Resources.GetString("OrientedImageDisplayImageAutomationNameFormat") ?? "Oriented image, {0}", type)
            : Properties.Resources.GetString("OrientedImageDisplayAutomationName") ?? "Oriented image viewer";
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
        {
            return;
        }

        if (_mapView.DrawStatus != DrawStatus.Completed)
        {
            return;
        }

        if (_rasterLayer?.Raster?.RasterInfo is not RasterInfo info || info.Extent is not Envelope extent)
        {
            return;
        }

        if (_mapView.VisibleArea is not Polygon visibleArea || visibleArea.Parts.Count == 0)
        {
            return;
        }

        OrientedImagePixelCorners corners = ComputeCorners(visibleArea, extent, info.CellSizeX, info.CellSizeY);

        _updateCts?.Cancel();
        CancellationTokenSource cts = new CancellationTokenSource();
        _updateCts = cts;
        try
        {
            await _footprint.UpdateAsync(corners, cts.Token);
        }
        catch
        {
            // Skeleton UpdateAsync is a no-op; ignore cancellation/failures.
        }
    }

    /// <summary>
    /// Converts the map-space visible-area quadrilateral into image pixel-space corners.
    /// </summary>
    /// <remarks>
    /// Image pixels map linearly to the raster extent (col = (x - XMin) / CellSizeX, row = (YMax - y) / CellSizeY).
    /// For the common non-georeferenced case CellSize is 1 and the extent spans the pixel grid, so map space is
    /// effectively pixel space (y-flipped). Corners are classified by sum/difference so the result is robust to
    /// view rotation.
    /// </remarks>
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
