#if WINDOWS || __IOS__ || __ANDROID__
using Microsoft.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

#if WINDOWS
using NativeView = Esri.ArcGISRuntime.Toolkit.Primitives.Image360;
#elif __IOS__ || __ANDROID__
using NativeView = Esri.ArcGISRuntime.Toolkit.Maui.Primitives.Image360;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Handlers
{
    internal class Image360Handler : ViewHandler<Image360, NativeView>
    {
        public static readonly IPropertyMapper<Image360, Image360Handler> Mapper =
            new PropertyMapper<Image360, Image360Handler>(ViewMapper)
            {
                [nameof(Image360.Source)] = MapSource,
#if WINDOWS
                [nameof(Image360.Background)] = MapBackground
#endif
            };

        public Image360Handler() : base(Mapper)
        {
        }

        protected override NativeView CreatePlatformView()
        {
#if WINDOWS || __IOS__
            var view = new NativeView();
#elif ANDROID
            var view = new NativeView(Context);
#endif
            return view;
        }
#if WINDOWS
        static void MapBackground(Image360Handler handler, Image360 view)
        {
            // no-op: Not supported on SwapChainPanel
        }
#endif
        static void MapSource(Image360Handler handler, Image360 view)
        {
            if (handler.PlatformView is not null)
                handler.PlatformView.Source = view.Source;
        }

        protected override void DisconnectHandler(NativeView platformView)
        {
#if ANDROID
            platformView.OnPause();
            platformView.Dispose();
#elif __IOS__
            platformView.Dispose();
#endif
            base.DisconnectHandler(platformView);
        }
    }
}
#endif