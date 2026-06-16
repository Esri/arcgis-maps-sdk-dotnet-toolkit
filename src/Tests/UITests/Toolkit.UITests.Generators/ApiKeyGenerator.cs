using Microsoft.CodeAnalysis;

namespace Toolkit.UITests.Generators;

/// <summary>
/// This generator exposes the TestAppApiKey property for use in the UITest test apps.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ApiKeyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get API key property, see https://github.com/dotnet/roslyn/blob/eb789e2741f6f22d9e283e2049dc1378871323e0/docs/features/incremental-generators.cookbook.md#consume-msbuild-properties-and-metadata
        IncrementalValueProvider<string?> keyProvider = context.AnalyzerConfigOptionsProvider.Select((provider, ct) =>
        {
            provider.GlobalOptions.TryGetValue("build_property.TestAppApiKey", out var key);
            return key;
        });

        context.RegisterSourceOutput(keyProvider, (productionContext, key) =>
        {
            if (string.IsNullOrWhiteSpace(key)) {
                productionContext.ReportDiagnostic(Diagnostic.Create(ApiKeyWarningDescriptor, null, new string[0]));
            }

            productionContext.AddSource("ApiKeyProvider.Generated.cs",
@$"public static partial class ApiKeyProvider
{{
    public const string Key = {(key == null ? "\"\"" : $"@\"{key}\"")};
}}");
        });
    }

    private static readonly DiagnosticDescriptor ApiKeyWarningDescriptor = new(
        id: "TKUITEST001",
        title: "Missing API key for Toolkit.UITests apps",
        messageFormat: "The Toolkit.UITests are missing an API key. Some tests will fail unless the TestAppApiKey msbuild property is set.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false);
}
