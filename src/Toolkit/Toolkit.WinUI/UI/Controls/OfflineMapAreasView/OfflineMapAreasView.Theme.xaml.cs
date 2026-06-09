namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class OfflineMapAreasViewResources : ResourceDictionary
{
    public OfflineMapAreasViewResources()
    {
        InitializeComponent();
    }

    public static ImageSource? BytesToImage(byte[]? imageData)
    {
        if (imageData is null || imageData.Length == 0)
            return null;
        var bmi = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
        using var ms = new MemoryStream(imageData);
        bmi.SetSource(ms.AsRandomAccessStream());
        return bmi;
    }

    public static Visibility FalseToVisible(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }

    public static Visibility VisibleIfNotNull(object? value)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }    
}