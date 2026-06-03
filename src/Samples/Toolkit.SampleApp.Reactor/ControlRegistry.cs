using System;
using System.Linq;

namespace Toolkit.SampleApp.Reactor;

public record ControlInfo(string Title, string Description, string Category, string IconGlyph, string Tag, string ImageFile = "Placeholder.png")
{
    /// <summary>Path for use with Reactor Image() element.</summary>
    public string ImagePath => $"ms-appx:///Assets/ControlImages/{ImageFile}";
}

public static class ControlRegistry
{
    public static ControlInfo[] All { get; } = new ControlInfo[]
    {
        // Basic Maps
        new("Map", "The simplest bit of code to display a map.", "Maps", "\uE707", "map", "Map.png"),
        new("Scene", "The simplest bit of code to display a scene.", "Maps", "\uE774", "scene", "Scene.png"),
        new("Local Scene", "The simplest bit of code to display a local scene.", "Maps", "\uE774", "localscene", "LocalScene.png"),
        new("Location Display", "Demonstrates enabling and configuring the location display.", "Maps", "\uE707", "locationdisplay", "LocationDisplay.png"),
        new("Loading Map", "Delay loading the map until it is loaded.", "Maps", "\uE707", "loadingmap", "Map.png"),

        new("Map Picker", "Demonstrates selecting a map from a dropdown", "Maps", "\uE707", "mappicker", "Map.png"),
        new("Basemap Picker", "Demonstrates selecting a basemap style from a dropdown and updating the active map", "Maps", "\uE707", "basemappicker", "Basemap.png"),
        new("Grid Picker", "Demonstrates selecting a coordinate grid and assigning it to the active map view.", "Maps", "\uE707", "gridpicker", "Map.png"),
        new("Identify Callout", "Identifies features on the map and shows either a feature callout or a custom no-results callout.", "Maps", "\uE707", "identifycallout", "Map.png"),
        new("Scene Lighting", "Demonstrates changing sun, atmosphere, space, and ambient lighting on a scene view.", "Maps", "\uE774", "scenelighting", "Scene.png"),

        new("Graphics Overlays", "Working with graphics overlays", "Maps", "\uE707", "graphicsoverlays", "Graphicsoverlays.png"),

        // Toolkit
        new("Compass", "Compass.", "Toolkit", "\uEBE6", "compass", "Compass.png"),
        new("Basemap Gallery", "Basemap Gallery.", "Toolkit", "\uEBE6", "basemapgallery", "BasemapGallery.png"),
        new("Bookmarks View", "Displays bookmarks from a geoview or custom collection.", "Toolkit", "\uEBE6", "bookmarksview"),
        new("Feature Data Field", "Displays and edits a single feature attribute.", "Toolkit", "\uEBE6", "featuredatafield"),
        new("Feature Form View", "Displays a feature form definition for editing feature attributes.", "Toolkit", "\uEBE6", "featureformview"),
        new("Floor Filter", "Browses floor-aware maps and filters by site, facility, and level.", "Toolkit", "\uEBE6", "floorfilter"),
        new("Legend", "Displays the legend for the current geoview.", "Toolkit", "\uEBE6", "legend"),
        new("Measure Toolbar", "Measures distance, area, and features in a map view.", "Toolkit", "\uEBE6", "measuretoolbar"),
        new("Overview Map", "Shows an inset map for the current viewpoint.", "Toolkit", "\uEBE6", "overviewmap"),
        new("Popup Viewer", "Displays popup content for identified features.", "Toolkit", "\uEBE6", "popupviewer"),
        new("Scale Line", "Displays a dynamic map scale indicator.", "Toolkit", "\uEBE6", "scaleline"),
        new("Search View", "Searches locations and shows results on the map.", "Toolkit", "\uEBE6", "searchview"),
        new("Symbol Display", "Renders ArcGIS symbols in the UI.", "Toolkit", "\uEBE6", "symboldisplay"),
        new("Time Slider", "Filters time-aware layers using a temporal slider.", "Toolkit", "\uEBE6", "timeslider"),
        new("Utility Network Trace Tool", "Adds starting points and runs named utility traces.", "Toolkit", "\uEBE6", "utilitynetworktracetool"),
    }
    .ToArray();

    public static string[] Categories { get; } = new[]
    {
        "Maps", "Toolkit"
    };

    public static ControlInfo[] Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return All;

        return All
            .Where(c =>
                c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
