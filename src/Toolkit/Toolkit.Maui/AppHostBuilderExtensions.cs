using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.LifecycleEvents;

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
#if WINDOWS || __IOS__
            builder.ConfigureMauiHandlers(handler => handler.AddHandler<MauiMediaElement, MauiMediaElementHandler>());
#endif
            builder.ConfigureFonts(fonts => fonts
                .AddEmbeddedResourceFont(typeof(AppHostBuilderExtensions).Assembly, "toolkit-icons.ttf", ToolkitIcons.FontFamilyName)
                );
            builder = builder.ConfigureLifecycleEvents(events =>
            {
#if __IOS__
                events.AddiOS(iosLifeCycleBuilder =>
                {
                    iosLifeCycleBuilder.FinishedLaunching((app, b) =>
                    {
                        JobManager.Shared.ResumeAllPausedJobsAsync();
                        return true;
                    });
                });
#elif WINDOWS
                events.AddWindows(winLifeCycleBuilder =>
                {
                    winLifeCycleBuilder.OnLaunched((app, b) =>
                    {
                        JobManager.Shared.ResumeAllPausedJobsAsync();
                    });
                });
#elif ANDROID
                events.AddAndroid(androidLifeCycleBuilder =>
                {
                    androidLifeCycleBuilder.OnResume((activity) =>
                    {
                        _ = JobManager.Shared.ResumeAllPausedJobsAsync();
                    });
                });
#endif
            });
            return builder;
        }
    }
}
