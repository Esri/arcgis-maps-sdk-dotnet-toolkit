#if __ANDROID__
using Android.Content;
using Android.Opengl;
using Android.Views;
using System.Diagnostics;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

internal class Image360 : GLSurfaceView
{
    private readonly Image360Renderer _renderer;
    private float _lastX;
    private float _lastY;

    public Image360(Context context) : base(context)
    {
        SetEGLContextClientVersion(2);

        _renderer = new Image360Renderer(context);
        SetRenderer(_renderer);

        RenderMode = Rendermode.Continuously;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _renderer?.Dispose();
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null)
            return true;

        var x = e.GetX();
        var y = e.GetY();

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                _lastX = x;
                _lastY = y;
                return true;

            case MotionEventActions.Move:
                var dx = x - _lastX;
                var dy = y - _lastY;

                _lastX = x;
                _lastY = y;

                QueueEvent(() => _renderer.AddRotation(dx, dy));
                return true;
        }

        return true;
    }

    private Uri? _source;

    // Image source
    public Uri? Source
    {
        get { return _source; }
        set
        {
            if (_source == value)
            {
                return;
            }

            _source = value;
            LoadSource();
        }
    }

    private async void LoadSource()
    {
        Stream? stream = null;
        if (!string.IsNullOrEmpty(Source?.OriginalString))
        {
            try
            {
                if (Source.IsFile)
                {
                    stream = File.Open(Source.ToString(), FileMode.Open, FileAccess.Read);
                }
                else if (Source.Scheme == Uri.UriSchemeHttp || Source.Scheme == Uri.UriSchemeHttps)
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    var response = await httpClient.GetAsync(Source);
                    if (!response.IsSuccessStatusCode)
                    {
                        Trace.WriteLine("Failed to load image from URL: " + Source + " - " + response.ReasonPhrase);
                    }
                    else
                    {
                        stream = await response.Content.ReadAsStreamAsync();
                    }
                }
            }
            catch { }
        }
        if (stream is not null)
        {
            QueueEvent(() => _renderer.SetImage(stream));
        }


    }
}
#endif