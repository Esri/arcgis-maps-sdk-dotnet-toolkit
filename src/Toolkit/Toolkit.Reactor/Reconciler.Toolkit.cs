using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

internal static partial class Reconciler
{
    internal static Compass CreateCompass(CompassElement element)
    {
        var compass = new Compass { 
            AutoHide = element.AutoHide,
            // GeoView = element.GeoView.GetUIElement()
        };
        ApplySetters(element.Setters, compass);
        return compass;
    }

    internal static BasemapGallery CreateBasemapGallery(BasemapGalleryElement element)
    {
        var gallery = new BasemapGallery
        {
            GeoModel = element.GeoModel,
        };
        ApplySetters(element.Setters, gallery);
        return gallery;
    }
}
