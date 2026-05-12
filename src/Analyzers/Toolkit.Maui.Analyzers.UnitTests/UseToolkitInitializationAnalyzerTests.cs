using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Toolkit.Analyzers.Test;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Analyzers.UnitTests;

[TestClass]
public class UseToolkitInitializationAnalyzerTests : BaseAnalyzersUnitTest<Analyzers.UseToolkitInitializationAnalyzer, Analyzers.UseToolkitInitializationAnalyzerCodeFixProvider>
{
    [TestMethod]
	public void UseToolkitInitializationAnalyzerId()
	{
		Assert.AreEqual("ArcGISMaps9001", UseToolkitInitializationAnalyzer.DiagnosticId);
	}

    [TestMethod]
    public async Task DetectMissingUseCall()
    {
        var testCode = @"
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
namespace MyMauiApp;
public class App : global::Microsoft.Maui.Controls.Application { }
public static class MauiProgram
{    
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        {|#0:builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
            fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
        })|};//.UseArcGISRuntime().UseArcGISToolkit();
        return builder.Build();
    }
}
";

        var expected = Diagnostic("ArcGISMaps9001").WithLocation(0);
        //var expected = Diagnostic("ArcGISMaps9001");
        await VerifyAnalyzerAsync(testCode, expected);
    }

    [TestMethod]
    public async Task DetectUseCallPresent()
    {
        var testCode = @"
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Toolkit.Maui;
namespace MyMauiApp;
public class App : global::Microsoft.Maui.Controls.Application { }
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
                fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
            }).UseArcGISRuntime().UseArcGISToolkit();
        return builder.Build();
    }
}";
        await VerifyAnalyzerAsync(testCode);
    }

    [TestMethod]
    public async Task CodeFixer()
    {
        var testCode = @"
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
namespace MyMauiApp;
public class App : global::Microsoft.Maui.Controls.Application { }
public static class MauiProgram
{    
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        {|#0:builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
            fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
        })|};
        return builder.Build();
    }
}
";


        var fixedCode = @"
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Esri.ArcGISRuntime.Toolkit.Maui;

namespace MyMauiApp;
public class App : global::Microsoft.Maui.Controls.Application { }
public static class MauiProgram
{    
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
            fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
        }).UseArcGISToolkit();
        return builder.Build();
    }
}
";

        var expected = Diagnostic("ArcGISMaps9001").WithLocation(0);
        await VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

}