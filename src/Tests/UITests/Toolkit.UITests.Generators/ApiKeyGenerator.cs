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
            provider.GlobalOptions.TryGetValue("build_property.TestAppApiKey", out var key) ? key : null);

        context.RegisterSourceOutput(keyProvider, (productionContext, key) =>
        {
            if (string.IsNullOrWhiteSpace(key)) {
                var descriptor = new DiagnosticDescriptor(
                    id: "TKUITEST001",
                    title: "Missing Toolkit.UITests api key",
                    messageFormat: "The Toolkit.UITests are missing an API key, some tests will fail.",
                    category: "Toolkit.UITests",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: false);
                productionContext.ReportDiagnostic(Diagnostic.Create(descriptor, null, new string[0]));
            }

            productionContext.AddSource("ApiKeyProvider.Generated.cs",
@$"public static partial class ApiKeyProvider
{{
    public const string Key = {(key == null ? "\"\"" : $"@\"{key}\"")};
}}");
        });
    }
}
