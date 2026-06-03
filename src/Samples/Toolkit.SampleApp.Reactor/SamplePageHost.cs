using System.Collections.Concurrent;
using System.IO;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Factories;

namespace Toolkit.SampleApp.Reactor;


/// <summary>
/// Reusable helpers for building consistent control demonstration pages
/// in the WinUI Gallery Reactor app.
/// Follows WinUI Gallery / Fluent Design spacing and theming conventions.
/// </summary>
public static class SamplePageHost
{
    private static readonly ConcurrentDictionary<string, string?> SourcePathByTag = new(StringComparer.Ordinal);

    /// <summary>
    /// Renders a themed card containing a live sample, optional options panel,
    /// and a collapsible source code block.
    /// </summary>
    public static Element SampleCard(string title, Element sample, string sourceCode, Element? options = null) =>
        GalleryControls.SampleCard(title, sample, sourceCode, options);

    public static bool HasSource(string tag) =>
        !string.IsNullOrWhiteSpace(GetSourceFilePath(tag));

    public static Element SourceView(string tag)
    {
        var sourceFilePath = GetSourceFilePath(tag);
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return TextBlock("Bundled source file not found.").Foreground(Theme.SecondaryText);
        }

        return SourceBlock(File.ReadAllText(sourceFilePath));
    }

    private static string? FindSampleSourceFile(string tag)
    {
        var sourceRoot = Path.Combine(AppContext.BaseDirectory, "SourceCode");
        if (!Directory.Exists(sourceRoot))
        {
            return null;
        }

        var relativeSourcePath = ControlRegistry.All
            .FirstOrDefault(control => string.Equals(control.Tag, tag, StringComparison.Ordinal))
            ?.SourcePath;

        if (string.IsNullOrWhiteSpace(relativeSourcePath))
        {
            return null;
        }

        var sourceFilePath = Path.Combine(sourceRoot, "Samples", relativeSourcePath);
        return File.Exists(sourceFilePath) ? sourceFilePath : null;
    }

    private static string? GetSourceFilePath(string tag) =>
        SourcePathByTag.GetOrAdd(
            tag,
            routeTag => FindSampleSourceFile(routeTag));

    /// <summary>
    /// Renders a page header with a title and description.
    /// Follows WinUI Gallery page header pattern.
    /// </summary>
    public static Element PageHeader(string title, string description) =>
        GalleryControls.PageHeader(title, description);

    /// <summary>
    /// Renders source code in a bordered monospace block (for inline use).
    /// </summary>
    public static Element SourceBlock(string code) =>
        TextBox(code)
            .IsReadOnly()
            .AcceptsReturn()
            .TextWrapping(Microsoft.UI.Xaml.TextWrapping.NoWrap)
            .Set(tb =>
            {
                tb.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Code, Consolas, monospace");
                tb.IsSpellCheckEnabled = false;
                Microsoft.UI.Xaml.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(tb, Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto);
                Microsoft.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollBarVisibility(tb, Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto);
                Microsoft.UI.Xaml.Controls.ScrollViewer.SetHorizontalScrollMode(tb, Microsoft.UI.Xaml.Controls.ScrollMode.Auto);
                Microsoft.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollMode(tb, Microsoft.UI.Xaml.Controls.ScrollMode.Auto);
            });

    /// <summary>
    /// Renders a bordered options panel with an "Options" label.
    /// </summary>
    public static Element OptionPanel(params Element[] options) =>
        Border(
                VStack(8,
                    new Element[] { Caption("Options").Foreground(Theme.SecondaryText).SemiBold() }
                        .Concat(options)
                        .ToArray()))
            .Background(Theme.SubtleFill)
            .WithBorder(Theme.SurfaceStroke)
            .CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
            .Padding(12);

    /// <summary>
    /// Wraps page content in a ScrollView with proper WinUI Gallery-style margins.
    /// Use this instead of manually wrapping in ScrollView + VStack + Padding.
    /// </summary>
    public static Element PageContent(string title, string description, params Element[] sampleCards) =>
        ScrollView(
            VStack(16,
                new Element[] { PageHeader(title, description) }
                    .Concat(sampleCards)
                    .ToArray()
            )
            .Margin(36, 24, 36, 36)
            .HAlign(HorizontalAlignment.Stretch)
        );
}
