using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI.Editing;
using Microsoft.UI.Reactor.Core;

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
}

/// <summary>
/// Represents a declarative local <see cref="LocalSceneView"/> element.
/// </summary>
/// <param name="Scene">The local scene displayed by the view.</param>
/// <param name="OnTapped">The action invoked when the underlying <see cref="LocalSceneView"/> is tapped.</param>
public record LocalSceneViewElement(Scene? Scene, Action<GeoViewInputEventArgs>? OnTapped = null) : GeoViewElement(OnTapped)
{
}

