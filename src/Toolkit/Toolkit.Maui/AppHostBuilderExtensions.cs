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
#if WINDOWS || __IOS__ || __ANDROID__
            builder.ConfigureMauiHandlers(handler =>
            {
                handler.AddHandler<HyperlinkLabel, HyperlinkLabelHandler>();
                handler.AddHandler<FieldsTableGrid, FieldsTableGridHandler>();
#if WINDOWS || __IOS__
                handler.AddHandler<MauiMediaElement, MauiMediaElementHandler>();
                handler.AddHandler<HyperlinkContentView, HyperlinkContentViewHandler>();
#endif
#if WINDOWS
                handler.AddHandler<FieldsTableCell, FieldsTableCellHandler>();
#elif __IOS__
                handler.AddHandler<PopupViewer, PopupViewerHandler>();
#elif __ANDROID__
                handler.AddHandler<PopupMediaPager, PopupMediaPagerHandler>();
#endif
#if __IOS__ || __ANDROID__
                handler.AddHandler<FieldsTableLabel, FieldsTableLabelHandler>();
                handler.AddHandler<Primitives.LegacyCollectionView, Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler>();
#endif
            });
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
                        _ = JobManager.Shared.ResumeAllPausedJobsAsync();
                        return true;
                    });
                });
#elif WINDOWS
                events.AddWindows(winLifeCycleBuilder =>
                {
                    winLifeCycleBuilder.OnLaunched((app, b) =>
                    {
                        _ = JobManager.Shared.ResumeAllPausedJobsAsync();
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
