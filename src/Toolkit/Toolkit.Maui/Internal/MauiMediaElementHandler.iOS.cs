#if __IOS__
using AVFoundation;
using AVKit;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal partial class MauiMediaElementHandler : ViewHandler<MauiMediaElement, UIView>, IDisposable
{
    private AVPlayer? _player;
    private AVPlayerViewController? _playerController;
    private UIView _playerView;

    protected override UIView CreatePlatformView()
    {
        _player = new AVPlayer();
        _playerController = new AVPlayerViewController
        {
            Player = _player,
        };
        _playerView = _playerController.View;
        _playerView.Frame = new CGRect(0, 0, 320, 180); // Default size, The aspect ratio is still maintained if video has different dimensions

        return _playerView;
    }

    public void UpdateSource(Uri source)
    {
        if (source is not null)
        {
            var url = NSUrl.FromString(source.ToString());
            _player.ReplaceCurrentItemWithPlayerItem(new AVPlayerItem(url));
        }
    }

    protected override void DisconnectHandler(UIView platformView)
    {
        Dispose();
        base.DisconnectHandler(platformView);
    }

    public void Dispose()
    {
        // Cleanup resources
        _player?.Pause();
        _player.ReplaceCurrentItemWithPlayerItem(null);
        _player?.Dispose();
        _player = null;
        _playerController?.Dispose();
        _playerController = null;
        _playerView?.RemoveFromSuperview();
        _playerView?.Dispose();
        _playerView = null;
    }
}

#endif