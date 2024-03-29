﻿using Esri.ArcGISRuntime.Toolkit.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    /// <summary>
    /// Extensions used to configure ArcGIS Maps SDK for .NET Toolkit
    /// </summary>
    public static class AppHostBuilderExtensions
    {
        /// <summary>
        ///  Initializes the ArcGIS Maps SDK for .NET Toolkit Controls.
        /// </summary>
        /// <param name="builder">The Maui host builder.</param>
        /// <returns>The host builder</returns>
        public static MauiAppBuilder UseArcGISToolkit(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(delegate (IMauiHandlersCollection a)
            {
                a.AddHandler(typeof(Compass), typeof(CompassHandler));
            }).ConfigureFonts(fonts => fonts.AddFont("calcite-ui-icons-24.ttf", "calcite-ui-icons-24"));

            return builder;
        }
    }
}
