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

        new("Map Picker", "Demonstrates selecting a map from a dropdown", "Maps", "\uE707", "mappicker", "Map.png"),
        new("Basemap Picker", "Demonstrates selecting a basemap style from a dropdown and updating the active map", "Maps", "\uE707", "basemappicker", "Basemap.png"),

        new("Graphics Overlays", "Working with graphics overlays", "Maps", "\uE707", "graphicsoverlays", "Graphicsoverlays.png"),

        // Toolkit
        new("Compass", "Compass.", "Toolkit", "\uEBE6", "compass", "Compass.png"),
        new("Basemap Gallery", "Basemap Gallery.", "Toolkit", "\uEBE6", "basemapgallery", "BasemapGallery.png"),
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
