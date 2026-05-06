//-:cnd:noEmit
#if MAUI_APP
using ClickEventArgs = System.EventArgs;
#elif WINUI_APP
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
#elif WPF_APP
using ClickEventArgs = System.Windows.RoutedEventArgs;
#endif
//+:cnd:noEmit

namespace Toolkit.UITests.App.TestPages;

/// <summary>
/// A test page that can be used as a starting point for new UI tests. It contains a MapView with a public web map,
/// but is otherwise empty.
/// </summary>
/// <remarks>
/// This class acts as the code-behind for the TestTemplate.xaml files, which should be mirrored across platforms.
/// The xaml files are located in the Toolkit.UITests.Wpf.App, Toolkit.UITests.WinUI.App, and Toolkit.UITests.Maui.App
/// projects under TestPages/TestTemplate.xaml.
/// </remarks>
public partial class PageName : TestPage
{
    public PageName()
    {
        InitializeComponent();

        // If you use an online map, scene, or layer, make sure it is Public
        var map = new Esri.ArcGISRuntime.Mapping.Map();
        map.BackgroundColor = System.Drawing.Color.White;
        MainMapView.Map = map;
        MainMapView.Grid = null;
        MainMapView.IsAttributionTextVisible = false;
    }

    private void ToggleAutoHideButton_Click(object sender, ClickEventArgs e)
    {
        MapCompass.AutoHide = !MapCompass.AutoHide;
    }
}
