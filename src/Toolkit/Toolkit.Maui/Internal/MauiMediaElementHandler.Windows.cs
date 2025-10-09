#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal
{
    internal partial class MauiMediaElementHandler : ViewHandler<MauiMediaElement, MediaPlayerElement>
    {
        protected override MediaPlayerElement CreatePlatformView() => new();

        public void UpdateSource(Uri source)
        {
            if (source is not null && PlatformView is MediaPlayerElement platformView)
            {
                platformView.Source = MediaSource.CreateFromUri(source);
                platformView.AreTransportControlsEnabled = true;
                platformView.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;
            }
        }

        protected override void DisconnectHandler(MediaPlayerElement platformView)
        {
            platformView.Source = null;
            base.DisconnectHandler(platformView);
        }
    }
} 
#endif