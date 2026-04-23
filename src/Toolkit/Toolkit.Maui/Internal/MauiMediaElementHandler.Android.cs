#if ANDROID
using Android.Widget;
using Microsoft.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal partial class MauiMediaElementHandler : ViewHandler<MauiMediaElement, VideoView>
{
    private MediaController? _mediaController;

    protected override VideoView CreatePlatformView()
    {
        var context = MauiContext?.Context ?? Android.App.Application.Context;
        var videoView = new VideoView(context);
        _mediaController = new MediaController(context);
        _mediaController.SetAnchorView(videoView);
        videoView.SetMediaController(_mediaController);
        return videoView;
    }

    public void UpdateSource(Uri source)
    {
        if (PlatformView is null)
        {
            return;
        }

        if (source is null)
        {
            PlatformView.StopPlayback();
            return;
        }

        PlatformView.SetVideoURI(Android.Net.Uri.Parse(source.ToString()));
        PlatformView.RequestFocus();
    }

    protected override void DisconnectHandler(VideoView platformView)
    {
        platformView.StopPlayback();
        platformView.SetMediaController(null);
        _mediaController?.Dispose();
        _mediaController = null;
        base.DisconnectHandler(platformView);
    }
}

#endif
