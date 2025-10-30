#if ANDROID
using Microsoft.Maui.Handlers;
using Android.Views;
using Android.Widget;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal partial class MauiMediaElementHandler : ViewHandler<MauiMediaElement, VideoView>, IDisposable
{
    private VideoView? _videoView;
    private MediaController? _mediaController;

    protected override VideoView CreatePlatformView()
    {
        _videoView = new VideoView(Context)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // Setup transport controls by default
        _mediaController = new MediaController(Context);
        _mediaController.SetAnchorView(_videoView);
        _videoView.SetMediaController(_mediaController);

        return _videoView;
    }

    public void UpdateSource(Uri source)
    {
        if (source is not null && _videoView is not null)
        {
            _videoView.StopPlayback();

            _videoView.SetVideoURI(Android.Net.Uri.Parse(source.ToString()));

            // Optionally, you can set AutoPlay here if you add that property to MauiMediaElement
            _videoView.Start();
        }
    }

    public void UpdateTransportControlsEnabled(bool enabled)
    {
        if (_videoView is null)
            return;

        if (enabled)
        {
            if (_mediaController == null)
            {
                _mediaController = new MediaController(Context);
                _mediaController.SetAnchorView(_videoView);
            }
            _videoView.SetMediaController(_mediaController);
        }
        else
        {
            _videoView.SetMediaController(null);
            _mediaController?.SetMediaPlayer(null);
            _mediaController = null;
        }
    }

    protected override void DisconnectHandler(VideoView platformView)
    {
        Dispose();
        base.DisconnectHandler(platformView);
    }

    public void Dispose()
    {
        if (_videoView != null)
        {
            _videoView.StopPlayback();
            _videoView.Dispose();
            _videoView = null;
        }
        if (_mediaController != null)
        {
            _mediaController.Dispose();
            _mediaController = null;
        }
    }
}

#endif