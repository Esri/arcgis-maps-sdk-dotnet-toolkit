using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    /// <summary>
    /// Creates a declarative <see cref="CompassElement"/>.
    /// </summary>
    /// <param name="geoView">The geoview associated with the compass.</param>
    /// <param name="autoHide"><see langword="true"/> to hide the compass when the geoview is north aligned; otherwise, <see langword="false"/>.</param>
    /// <returns>A new <see cref="CompassElement"/> instance.</returns>
    public static CompassElement Compass(GeoViewElement? geoView, bool autoHide=true) =>
        new(geoView, autoHide);

    /// <summary>
    /// Creates a declarative <see cref="BasemapGalleryElement"/>.
    /// </summary>
    /// <param name="geomodel">The geo model whose basemap is displayed and updated by the gallery.</param>
    /// <returns>A new <see cref="BasemapGalleryElement"/> instance.</returns>
    public static BasemapGalleryElement BasemapGallery(GeoModel geomodel) =>
        new(geomodel);

    /// <summary>
    /// Sets the view style used to present basemap gallery items.
    /// </summary>
    /// <param name="element">The basemap gallery element to configure.</param>
    /// <param name="style">The basemap gallery view style.</param>
    /// <returns>The configured basemap gallery element.</returns>
    public static BasemapGalleryElement ViewStyle(this BasemapGalleryElement element, UI.BasemapGalleryViewStyle style)
    {
        element.GalleryViewStyle = style;
        return element;
    }

}