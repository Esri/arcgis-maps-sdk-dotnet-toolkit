#if ANDROID
using Android.Media;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal partial class MauiMediaElementHandler : ViewHandler<MauiMediaElement, MauiVideoPlayer>
{
    protected override MauiVideoPlayer CreatePlatformView() => new(Context, VirtualView);

    protected override void ConnectHandler(MauiVideoPlayer platformView)
    {
        base.ConnectHandler(platformView);
        platformView.UpdateSource(VirtualView.Source);
    }

    protected override void DisconnectHandler(MauiVideoPlayer platformView)
    {
        platformView.Dispose();
        base.DisconnectHandler(platformView);
    }

    public void UpdateSource(Uri? source)
    {
        PlatformView?.UpdateSource(source);
    }
}

internal sealed class MauiVideoPlayer : CoordinatorLayout
{
    private readonly RelativeLayout _relativeLayout;
    private readonly VideoView _videoView;
    private readonly MediaController _mediaController;

    public MauiVideoPlayer(Android.Content.Context context, MauiMediaElement mediaElement) : base(context)
    {
        SetBackgroundColor(Android.Graphics.Color.Black);

        _relativeLayout = new RelativeLayout(context)
        {
            LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
            {
                Gravity = (int)Android.Views.GravityFlags.Center,
            },
        };

        _videoView = new VideoView(context)
        {
            LayoutParameters = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent),
        };

        _relativeLayout.AddView(_videoView);
        AddView(_relativeLayout);

        _mediaController = new MediaController(context);
        _mediaController.SetAnchorView(_videoView);
        _mediaController.SetMediaPlayer(_videoView);
        _videoView.SetMediaController(_mediaController);
    }

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        var density = Context?.Resources?.DisplayMetrics?.Density ?? 1f;
        var heightInPixels = (int)(180 * density);
        var exactHeight = Android.Views.View.MeasureSpec.MakeMeasureSpec(heightInPixels, Android.Views.MeasureSpecMode.Exactly);

        _relativeLayout.Measure(widthMeasureSpec, exactHeight);
        SetMeasuredDimension(Android.Views.View.MeasureSpec.GetSize(widthMeasureSpec), Android.Views.View.MeasureSpec.GetSize(exactHeight));
    }

    protected override void OnLayout(bool changed, int l, int t, int r, int b)
    {
        _relativeLayout.Layout(0, 0, r - l, b - t);
    }

    public void UpdateSource(Uri? source)
    {
        if (source is null)
        {
            _videoView.StopPlayback();
            return;
        }

        _videoView.SetVideoURI(Android.Net.Uri.Parse(source.ToString()));
        _videoView.RequestFocus();
        _videoView.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _videoView.SetMediaController(null);
            _videoView.StopPlayback();
            _mediaController.SetMediaPlayer(null);
            _mediaController.Dispose();
            _videoView.Dispose();
            _relativeLayout.Dispose();
        }

        base.Dispose(disposing);
    }
}

#endif