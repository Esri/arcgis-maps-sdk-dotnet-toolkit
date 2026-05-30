using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public record CompassElement(GeoViewElement? GeoView = null, bool AutoHide = true) : Element
{ 
    public double Heading { get; set; }
    
    internal Action<Compass>[] Setters { get; init; } = [];

    internal static Compass Mount(CompassElement element) =>
        Reconciler.CreateCompass(element);

    internal static void Update(CompassElement oldElement, CompassElement newElement, Compass compass)
    {
        if (oldElement.Heading != newElement.Heading)
            compass.Heading = newElement.Heading;
        if (oldElement.AutoHide != newElement.AutoHide)
            compass.AutoHide = newElement.AutoHide;
        if (oldElement.GeoView != newElement.GeoView)
        {
            // compass.GeoView = newElement.GeoView; TODO
        }
    }
    internal static void Unmount(Compass compass) =>
       compass.GeoView = null;
}

public record BasemapGalleryElement(GeoModel? GeoModel = null) : Element
{
    public UI.BasemapGalleryViewStyle ViewStyle { get; set; }
    internal Action<BasemapGallery>[] Setters { get; init; } = [];

    internal static BasemapGallery Mount(BasemapGalleryElement element) =>
        Reconciler.CreateBasemapGallery(element);

    internal static void Update(BasemapGalleryElement oldElement, BasemapGalleryElement newElement, BasemapGallery gallery)
    {
        if (oldElement.GeoModel != newElement.GeoModel)
            gallery.GeoModel = newElement.GeoModel;
        if (oldElement.ViewStyle != newElement.ViewStyle)
            gallery.GalleryViewStyle = newElement.ViewStyle;
    }

    internal static void Unmount(BasemapGallery gallery) =>
       gallery.GeoModel = null;
}
