using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI.Editing;
using Esri.ArcGISRuntime.UI.GeoAnalysis;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

/// <summary>
/// Represents a declarative <see cref="GeoView"/> element.
/// </summary>
/// <param name="OnTapped">The action invoked when the underlying <see cref="GeoView"/> is tapped.</param>
public abstract record GeoViewElement(Action<GeoViewInputEventArgs>? OnTapped = null) : Element
{
    internal Action<GeoView>[] Setters { get; init; } = [];

    /// <summary>
    /// Gets or sets the graphics overlays displayed by the underlying <see cref="GeoView"/>.
    /// </summary>
    public GraphicsOverlayCollection? GraphicsOverlays { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Esri attribution text is visible.
    /// </summary>
    public bool IsAttributionTextVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the viewport inset padding.
    /// </summary>
    public Thickness ViewInsets { get; set; }

    /// <summary>
    /// Gets or sets the time extent used to filter time-aware content in the underlying <see cref="GeoView"/>.
    /// </summary>
    public TimeExtent? TimeExtent { get; set; }

    /// <summary>
    /// Gets or sets the selection properties for graphics overlays and selectable layers in the underlying <see cref="GeoView"/>.
    /// </summary>
    public SelectionProperties? SelectionProperties { get; set; }

    /// <summary>
    /// Gets or sets the labeling configuration for the underlying <see cref="GeoView"/>.
    /// </summary>
    public ViewLabelProperties? Labeling { get; set; }

    /// <summary>
    /// Gets or sets the coordinate system grid displayed on top of the underlying <see cref="GeoView"/>.
    /// </summary>
    public Grid? Grid { get; set; }

    /// <summary>
    /// Gets or sets the image overlays displayed by the underlying <see cref="GeoView"/>.
    /// </summary>
    public ImageOverlayCollection? ImageOverlays { get; set; }

    /// <summary>
    /// Gets or sets the analysis overlays rendered by the underlying <see cref="GeoView"/>.
    /// </summary>
    public AnalysisOverlayCollection? AnalysisOverlays { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the draw status changes.
    /// </summary>
    public Action<DrawStatusChangedEventArgs>? OnDrawStatusChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the layer view state changes.
    /// </summary>
    public Action<LayerViewStateChangedEventArgs>? OnLayerViewStateChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the spatial reference changes.
    /// </summary>
    public Action? OnSpatialReferenceChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the analysis view state changes.
    /// </summary>
    public Action<AnalysisViewStateChangedEventArgs>? OnAnalysisViewStateChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the viewpoint changes.
    /// </summary>
    public Action? OnViewpointChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the underlying <see cref="GeoView"/> is double tapped.
    /// </summary>
    public Action<GeoViewInputEventArgs>? OnGeoViewDoubleTapped { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the underlying <see cref="GeoView"/> is held.
    /// </summary>
    public Action<GeoViewInputEventArgs>? OnGeoViewHolding { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when navigation completes.
    /// </summary>
    public Action? OnNavigationCompleted { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the critical error changes.
    /// </summary>
    public Action<Exception?>? OnCriticalErrorChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the geo model error changes.
    /// </summary>
    public Action<Exception?>? OnGeoModelErrorChanged { get; init; }
}

/// <summary>
/// Represents a declarative <see cref="MapView"/> element.
/// </summary>
/// <param name="Map">The map displayed by the view.</param>
/// <param name="OnTapped">The action invoked when the underlying <see cref="MapView"/> is tapped.</param>
public record MapViewElement(Map? Map, Action<GeoViewInputEventArgs>? OnTapped = null) : GeoViewElement(OnTapped)
{
    /// <summary>
    /// Gets or sets the geometry editor used by the underlying <see cref="MapView"/>.
    /// </summary>
    public GeometryEditor? GeometryEditor { get; set; }

    /// <summary>
    /// Gets or sets the location display configuration for the underlying <see cref="MapView"/>.
    /// </summary>
    public LocationDisplayElement LocationDisplay { get; set; } = new LocationDisplayElement();

    /// <summary>
    /// Gets or sets whether continuous panning across the international date line is enabled.
    /// </summary>
    public WrapAroundMode WrapAroundMode { get; set; } = WrapAroundMode.EnabledWhenSupported;

    /// <summary>
    /// Gets or sets the background grid displayed beneath the map.
    /// </summary>
    public BackgroundGrid? BackgroundGrid { get; set; }

    /// <summary>
    /// Gets or sets the interaction options that control user interaction with the map view.
    /// </summary>
    public MapViewInteractionOptions? InteractionOptions { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the map scale changes.
    /// </summary>
    public Action<double>? OnMapScaleChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the units-per-pixel value changes.
    /// </summary>
    public Action<double>? OnUnitsPerPixelChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the map rotation changes.
    /// </summary>
    public Action<double>? OnMapRotationChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the visible area changes.
    /// </summary>
    public Action<Polygon?>? OnVisibleAreaChanged { get; init; }
}

/// <summary>
/// Represents the configuration of a <see cref="Esri.ArcGISRuntime.UI.LocationDisplay"/>.
/// </summary>
public record LocationDisplayElement : Element
{
    /// <summary>
    /// Gets or sets a value indicating whether the location display is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the auto-pan mode used by the location display.
    /// </summary>
    public LocationDisplayAutoPanMode AutoPanMode { get; set; } = LocationDisplayAutoPanMode.Off;
}

/// <summary>
/// Represents a declarative global <see cref="SceneView"/> element.
/// </summary>
/// <param name="Scene">The scene displayed by the view.</param>
/// <param name="OnTapped">The action invoked when the underlying <see cref="SceneView"/> is tapped.</param>
public record SceneViewElement(Scene? Scene, Action<GeoViewInputEventArgs>? OnTapped = null) : GeoViewElement(OnTapped)
{
    /// <summary>
    /// Gets or sets the camera controller that manages the scene view camera.
    /// </summary>
    public CameraController? CameraController { get; set; }

    /// <summary>
    /// Gets or sets the effect applied to the scene atmosphere.
    /// </summary>
    public AtmosphereEffect? AtmosphereEffect { get; set; }

    /// <summary>
    /// Gets or sets the type of ambient sunlight and shadows in the scene view.
    /// </summary>
    public LightingMode? SunLighting { get; set; }

    /// <summary>
    /// Gets or sets the date and time used to position the sun in the scene view.
    /// </summary>
    public DateTimeOffset? SunTime { get; set; }

    /// <summary>
    /// Gets or sets the ambient light color for the scene view.
    /// </summary>
    public Windows.UI.Color? AmbientLightColor { get; set; }

    /// <summary>
    /// Gets or sets the visual effect of outer space in the scene view.
    /// </summary>
    public SpaceEffect? SpaceEffect { get; set; }

    /// <summary>
    /// Gets or sets the interaction options that control user interaction with the scene view.
    /// </summary>
    public SceneViewInteractionOptions? InteractionOptions { get; set; }
}

/// <summary>
/// Represents a declarative local <see cref="LocalSceneView"/> element.
/// </summary>
/// <param name="Scene">The local scene displayed by the view.</param>
/// <param name="OnTapped">The action invoked when the underlying <see cref="LocalSceneView"/> is tapped.</param>
public record LocalSceneViewElement(Scene? Scene, Action<GeoViewInputEventArgs>? OnTapped = null) : GeoViewElement(OnTapped)
{
    /// <summary>
    /// Gets or sets the interaction options that control user interaction with the local scene view.
    /// </summary>
    public SceneViewInteractionOptions? InteractionOptions { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the local scene warnings change.
    /// </summary>
    public Action<IEnumerable<Exception>>? OnWarningsChanged { get; init; }
}
