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
        new("Map", "Simple Map.", "Maps", "\uE73A", "map", "Map.png"),

    }
    .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
    .ToArray();

    public static string[] Categories { get; } = new[]
    {
        "Maps",
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
