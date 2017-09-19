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

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.ScaleLine), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.ScaleLineRenderer))]
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class ScaleLineRenderer : ViewRenderer<ScaleLine, Esri.ArcGISRuntime.Toolkit.UI.Controls.ScaleLine>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<ScaleLine> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(e.NewElement?.NativeScaleLine);
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
