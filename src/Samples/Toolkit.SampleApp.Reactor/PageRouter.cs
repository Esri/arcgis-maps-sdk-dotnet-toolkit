using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Toolkit.SampleApp.Reactor;

/// <summary>
/// Maps control tags to their page components.
/// </summary>
static class PageRouter
{
    public static Element Route(string tag) => tag switch
    {
        // Basic maps
        "map" => Component<Samples.Maps.SimpleMapPage>(),
        "scene" => Component<Samples.Maps.SimpleScenePage>(),
        "mappicker" => Component<Samples.Maps.MapPickerPage>(),
        "basemappicker" => Component<Samples.Maps.BasemapPickerPage>(),
        "graphicsoverlays" => Component<Samples.Maps.GraphicsOverlayPage>(),

        // Toolkit
        "compass" => Component<Samples.Toolkit.CompassPage>(),
        "basemapgallery" => Component<Samples.Toolkit.BasemapGalleryPage>(),

        _ => TextBlock($"Page not found: {tag}").Foreground(Theme.SecondaryText)
    };
}
