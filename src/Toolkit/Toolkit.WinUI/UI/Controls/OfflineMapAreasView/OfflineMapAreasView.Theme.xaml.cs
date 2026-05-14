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
}