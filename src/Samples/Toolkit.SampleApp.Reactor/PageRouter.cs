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
        "localscene" => Component<Samples.Maps.SimpleLocalScenePage>(),
        "locationdisplay" => Component<Samples.Maps.LocationDisplayPage>(),

        "mappicker" => Component<Samples.Maps.MapPickerPage>(),
        "basemappicker" => Component<Samples.Maps.BasemapPickerPage>(),
        "gridpicker" => Component<Samples.Maps.GridPickerPage>(),
        "scenelighting" => Component<Samples.Maps.SceneLightingPage>(),
        "graphicsoverlays" => Component<Samples.Maps.GraphicsOverlayPage>(),
        "loadingmap" => Component<Samples.Maps.LoadingMapPage>(),        

        // Toolkit
        "compass" => Component<Samples.Toolkit.CompassPage>(),
        "basemapgallery" => Component<Samples.Toolkit.BasemapGalleryPage>(),
        "bookmarksview" => Component<Samples.Toolkit.BookmarksViewPage>(),
        "featuredatafield" => Component<Samples.Toolkit.FeatureDataFieldPage>(),
        "featureformview" => Component<Samples.Toolkit.FeatureFormViewPage>(),
        "floorfilter" => Component<Samples.Toolkit.FloorFilterPage>(),
        "legend" => Component<Samples.Toolkit.LegendPage>(),
        "measuretoolbar" => Component<Samples.Toolkit.MeasureToolbarPage>(),
        "overviewmap" => Component<Samples.Toolkit.OverviewMapPage>(),
        "popupviewer" => Component<Samples.Toolkit.PopupViewerPage>(),
        "scaleline" => Component<Samples.Toolkit.ScaleLinePage>(),
        "searchview" => Component<Samples.Toolkit.SearchViewPage>(),
        "symboldisplay" => Component<Samples.Toolkit.SymbolDisplayPage>(),
        "timeslider" => Component<Samples.Toolkit.TimeSliderPage>(),
        "utilitynetworktracetool" => Component<Samples.Toolkit.UtilityNetworkTraceToolPage>(),

        _ => TextBlock($"Page not found: {tag}").Foreground(Theme.SecondaryText)
    };
}
