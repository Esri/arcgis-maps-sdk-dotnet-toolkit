using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;
namespace Toolkit.SampleApp.Reactor;

class HomePage : Component<Action<string>>
{
    public override Element Render()
    {
        var navigate = Props;

        var categoryControls = ControlRegistry.All
            .GroupBy(c => c.Category)
            .OrderBy(g => g.Key)
            .Select(g => new ControlInfo(
                g.Key,
                $"{g.Count()} controls",
                g.Key,
                g.First().IconGlyph,
                g.Key.ToLowerInvariant().Replace(" ", "-"),
                g.First().ImageFile))
            .ToArray();

        var recentControls = ControlRegistry.All.Take(8).ToArray();

        return (ScrollViewer(
            VStack(0,
                // ── Hero section ────────────────────────────────────────
                Border(
                    VStack(12,
                        TextBlock("ArcGIS Maps SDK for .NET - Reactor Gallery")
                            .ApplyStyle("TitleTextBlockStyle")
                            .Bold(),
                        TextBlock("A showcase of ArcGIS Maps SDK controls used with Reactor — a declarative,\ncomponent-based UI framework for WinUI.")
                            .Foreground(Theme.SecondaryText)
                            .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                            .MaxWidth(600)
                    )
                    .Margin(0, 0, 0, 36)
                    .HAlign(HorizontalAlignment.Left)
                ),

                TextBlock("Enter API Key"),
                PasswordBox(Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey, (key) => {
                        Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = key;
                        _ = SecureStorage.SetAsync("APIKey", key);
                    },
                    "Enter API Key").AutomationName("API Key"),
                Caption("Most samples require an ArcGIS Services API Key").Margin(0, 0, 0, 16),

                // ── Category cards section ──────────────────────────────
                VStack(16,
                    TextBlock("Browse by Category")
                        .ApplyStyle("BodyStrongTextBlockStyle"),

                    GalleryControls.ControlCardGrid(categoryControls, navigate),

                    // Recently added section
                    TextBlock("Recently Added")
                        .ApplyStyle("BodyStrongTextBlockStyle"),

                    GalleryControls.ControlCardGrid(recentControls, navigate)
                )
            ).Margin(36, 40, 36, 36)
        ) with
        {
            HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Disabled,
            HorizontalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Disabled,
        });
    }
}
