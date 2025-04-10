namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class OverviewMapResources : ResourceDictionary
{
    public OverviewMapResources()
    {
        InitializeComponent();
    }

    public static Visibility LoadStatusToVisibility(LoadStatus status, string visibleStatus)
        => status.ToString() == visibleStatus ? Visibility.Visible : Visibility.Collapsed;
}