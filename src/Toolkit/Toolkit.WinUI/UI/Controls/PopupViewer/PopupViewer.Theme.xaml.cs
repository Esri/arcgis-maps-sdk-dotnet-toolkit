using Esri.ArcGISRuntime.Toolkit.Internal;
using Microsoft.UI.Xaml;

namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class PopupViewerResources : ResourceDictionary
{
    public PopupViewerResources()
    {
        InitializeComponent();
    }

    public static string HtmlToPlainTextConverter(string html)
        => StringExtensions.ToPlainText(html);

    public static Visibility VisibilityConverter(string text)
        => string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
}