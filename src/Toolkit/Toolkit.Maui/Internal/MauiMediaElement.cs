namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

/// <summary>
/// A view that displays audio or video media from a specified source.
/// </summary>
internal partial class MauiMediaElement : View
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(Uri), typeof(MauiMediaElement), null);

    public Uri Source
    {
        get => (Uri)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
}