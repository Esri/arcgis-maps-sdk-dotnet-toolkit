namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class FloorFilterResources : ResourceDictionary
{
    public FloorFilterResources()
    {
        InitializeComponent();
    }

    public static Visibility FalseToVisible(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }
}