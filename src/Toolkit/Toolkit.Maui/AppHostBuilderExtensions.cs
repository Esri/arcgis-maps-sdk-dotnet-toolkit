using Esri.ArcGISRuntime.Toolkit.Maui.Internal;

﻿namespace Esri.ArcGISRuntime.Toolkit.Maui
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
#if WINDOWS || __IOS__ || ANDROID
            builder.ConfigureMauiHandlers(handler => handler.AddHandler<MauiMediaElement, MauiMediaElementHandler>());
#endif
            builder.ConfigureFonts(fonts => fonts
                .AddEmbeddedResourceFont(typeof(AppHostBuilderExtensions).Assembly, "toolkit-icons.ttf", ToolkitIcons.FontFamilyName)
                );
            return builder;
        }
    }
}
