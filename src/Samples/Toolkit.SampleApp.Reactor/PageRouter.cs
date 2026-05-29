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
        // Basic Input
        "map" => Component<Samples.Maps.SimpleMapPage>(),

        _ => TextBlock($"Page not found: {tag}").Foreground(Theme.SecondaryText)
    };
}
