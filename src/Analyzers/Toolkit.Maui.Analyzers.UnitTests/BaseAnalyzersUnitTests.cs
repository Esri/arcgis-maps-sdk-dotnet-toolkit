using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Toolkit.Analyzers.Test;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Analyzers.UnitTests;
public abstract class BaseAnalyzersUnitTest<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    private static readonly Lazy<ReferenceAssemblies> _lazyNet8_MAUI =
           new Lazy<ReferenceAssemblies>(() =>
           {
               return new ReferenceAssemblies("net8.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.2"), System.IO.Path.Combine("ref", "net8.0"))
                        .AddPackages(ImmutableArray.Create(new PackageIdentity[] {
                                new PackageIdentity("Microsoft.Maui.Controls", "8.0.3"),
                                new PackageIdentity("Esri.ArcGISRuntime.Maui", "200.3.0"),
                                new PackageIdentity("Esri.ArcGISRuntime.Toolkit.Maui", "200.3.0")}));
           });
    public static ReferenceAssemblies Net8_MAUI => _lazyNet8_MAUI.Value;

    protected static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
        {
            ReferenceAssemblies = Net8_MAUI,
            TestCode = source, 
        };
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
    protected static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
    {
        var test = new CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
        {
            ReferenceAssemblies = Net8_MAUI,
            TestCode = source,
            FixedCode = fixedSource
        };
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }
    protected DiagnosticResult Diagnostic(string diagnosticId) => Esri.ArcGISRuntime.Toolkit.Analyzers.Test.CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Diagnostic(diagnosticId);
}