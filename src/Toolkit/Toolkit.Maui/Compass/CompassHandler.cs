using Esri.ArcGISRuntime.Maui.Handlers;
using Microsoft.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Handlers
{
    public class CompassHandler
#if WINDOWS
        : ViewHandler<ICompass, Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass>
#elif __IOS__
        : ViewHandler<ICompass, UIKit.UIView>
#elif __ANDROID__
        : ViewHandler<ICompass, Android.Views.View>
#else
        : ViewHandler<ICompass, object>
#endif

    {
        public static PropertyMapper<ICompass, CompassHandler> CompassMapper = new PropertyMapper<ICompass, CompassHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ICompass.AutoHide)] = MapAutoHide,
            [nameof(ICompass.GeoView)] = MapGeoView,
            [nameof(ICompass.Heading)] = MapHeading,
        };

        public CompassHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper, commandMapper)
        {
        }

        private static void MapAutoHide(CompassHandler handler, ICompass compass)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).AutoHide = compass.AutoHide;
#endif
        }

        private static void MapGeoView(CompassHandler handler, ICompass compass)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).GeoView = (compass.GeoView?.Handler as GeoViewHandler<Esri.ArcGISRuntime.Maui.IGeoView, Esri.ArcGISRuntime.UI.Controls.GeoView>)?.PlatformView;
#endif
        }

        private static void MapHeading(CompassHandler handler, ICompass compass)
        {
#if WINDOWS || __IOS__ || __ANDROID__
            ((Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass)handler.PlatformView).Heading = compass.Heading;
#endif
        }

        /// <inheritdoc />
#if WINDOWS
        protected override Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass();

#elif __IOS__
        protected override UIKit.UIView CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass();
#elif __ANDROID__
        protected override Android.Views.View CreatePlatformView() => new Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass(this.Context);
#else
        protected override object CreatePlatformView()
        {
            throw new NotImplementedException();
        }
#endif
    }
}
