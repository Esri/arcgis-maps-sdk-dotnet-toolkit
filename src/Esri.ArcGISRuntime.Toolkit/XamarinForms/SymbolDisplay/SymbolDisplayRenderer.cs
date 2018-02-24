using Xamarin.Forms;
#if __ANDROID__
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using Xamarin.Forms.Platform.iOS;
#elif NETFX_CORE
using Xamarin.Forms.Platform.UWP;
#endif

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.SymbolDisplay), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.SymbolDisplayRenderer))]
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class SymbolDisplayRenderer : ViewRenderer<SymbolDisplay, UI.Controls.SymbolDisplay>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<SymbolDisplay> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(e.NewElement?.NativeSymbolDisplay);
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
