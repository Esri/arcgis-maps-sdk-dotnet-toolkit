namespace Esri.ArcGISRuntime.Toolkit.Maui;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UseArcGISRuntimeToolkit(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(Compass), typeof(CompassHandler));
            handlers.AddHandler(typeof(ScaleLine), typeof(ScaleLineHandler));
        });
        return builder;
    }
}