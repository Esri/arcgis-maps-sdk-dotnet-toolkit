#if WINDOWS || __IOS__ || ANDROID
using Microsoft.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal
{
    internal partial class MauiMediaElementHandler
    {
        public static readonly IPropertyMapper<MauiMediaElement, MauiMediaElementHandler> PropertyMapper =
        new PropertyMapper<MauiMediaElement, MauiMediaElementHandler>(ViewHandler.ViewMapper)
        {
            [nameof(MauiMediaElement.Source)] = MapSource,
        };

        public MauiMediaElementHandler() : base(PropertyMapper)
        {
        }

        private static void MapSource(MauiMediaElementHandler handler, MauiMediaElement view)
        {
            handler.UpdateSource(view.Source);
        }
    }
}

#endif