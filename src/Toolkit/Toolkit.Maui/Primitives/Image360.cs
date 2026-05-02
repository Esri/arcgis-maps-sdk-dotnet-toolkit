using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public class Image360 : ContentPresenter
{
    public Image360()
    {
    }

    /// <summary>
    /// Identifies the <see cref="Source"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(Uri), typeof(Image360), null, BindingMode.OneWay, null);

    public Uri? Source
    {
        get { return (Uri)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }
}