using Esri.ArcGISRuntime.Toolkit.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public static class AppHostBuilderExtensions
    {
        /// <summary>
        ///  Initializes the ArcGIS Runtime Toolkit Controls.
        /// </summary>
        /// <param name="builder">The Maui host builder.</param>
        /// <returns>The host builder</returns>
        public static MauiAppBuilder UseArcGISToolkit(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(delegate (IMauiHandlersCollection a)
            {
                a.AddHandler(typeof(Compass), typeof(CompassHandler));
            });

            return builder; 
        }
    }
}
