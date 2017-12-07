using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
#if __ANDROID__
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using Xamarin.Forms.Platform.iOS;
#elif NETFX_CORE
using Xamarin.Forms.Platform.UWP;
#endif

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Compass), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.CompassRenderer))]
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class CompassRenderer : ViewRenderer<Compass, Esri.ArcGISRuntime.Toolkit.UI.Controls.Compass>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Compass> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(e.NewElement?.NativeCompass);
            }
        }

#if !NETFX_CORE
        /// <summary>
        /// Determines whether the native control is disposed of when this renderer is disposed
        /// Can be overridden in deriving classes (default: true).
        /// </summary>
        protected override bool ManageNativeControlLifetime => false;
#endif
    }
}
