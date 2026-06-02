using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Microsoft.UI.Reactor.Core;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

/// <summary>
/// Represents a declarative <see cref="Compass"/> element.
/// </summary>
/// <param name="GeoView">The geoview associated with the compass.</param>
/// <param name="AutoHide"><see langword="true"/> to hide the compass when the geoview is north aligned; otherwise, <see langword="false"/>.</param>
public record CompassElement(GeoViewElement? GeoView = null, bool AutoHide = true) : Element
{ 
    /// <summary>
    /// Gets or sets the heading, in degrees, shown by the compass.
    /// </summary>
    public double Heading { get; set; }
    
    internal Action<Compass>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="BasemapGallery"/> element.
/// </summary>
/// <param name="GeoModel">The geo model whose basemap is displayed and updated by the gallery.</param>
public record BasemapGalleryElement(GeoModel? GeoModel = null) : Element
{
    /// <summary>
    /// Gets or sets the visual style used to present gallery items.
    /// </summary>
    public UI.BasemapGalleryViewStyle GalleryViewStyle { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the selected basemap changes.
    /// </summary>
    public Action<UI.BasemapGalleryItem?>? OnBasemapSelected { get; init; }

    /// <summary>
    /// Gets or sets the currently selected basemap item.
    /// </summary>
    public BasemapGalleryItem? SelectedBasemap { get; init; }
    internal Action<BasemapGallery>[] Setters { get; init; } = [];
}
