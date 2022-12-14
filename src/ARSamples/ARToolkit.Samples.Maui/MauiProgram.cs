using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.ARToolkit.Maui;
[assembly: System.Runtime.Versioning.UnsupportedOSPlatform("maccatalyst")]
namespace ARToolkit.Samples.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                }).UseArcGISRuntime().UseARToolkit();

            return builder.Build();
        }
    }
}