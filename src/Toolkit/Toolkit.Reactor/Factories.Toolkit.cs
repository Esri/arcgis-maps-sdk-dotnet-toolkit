using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{

    public static CompassElement Compass(GeoViewElement? geoView, bool autoHide=true) =>
        new(geoView, autoHide);

    public static BasemapGalleryElement BasemapGallery(GeoModel geomodel) =>
        new(geomodel);

    public static BasemapGalleryElement ViewStyle(this BasemapGalleryElement element, UI.BasemapGalleryViewStyle style)
    {
        element.ViewStyle = style;
        return element;
    }

}