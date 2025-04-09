using Microsoft.UI.Xaml;

namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class FeatureFormResources : ResourceDictionary
{
    public FeatureFormResources()
    {
        InitializeComponent();
    }

    public static Visibility VisibilityConverter(object value)
    {
        if (value is string text)
            return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
        if (value is bool visible)
            return visible ? Visibility.Visible : Visibility.Collapsed;
        if(value is null)
            return Visibility.Collapsed;
        return Visibility.Visible;
    }
}