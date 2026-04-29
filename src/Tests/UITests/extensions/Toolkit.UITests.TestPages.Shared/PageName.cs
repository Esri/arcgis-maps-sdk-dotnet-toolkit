
namespace Toolkit.UITests.App.TestPages;

/// <summary>
/// A test page that can be used as a starting point for new UI tests. It contains a MapView with a public web map,
/// but is otherwise empty.
/// </summary>
/// <remarks>
/// This class acts as the code-behind for the PageName.xaml files, which should be mirrored across platforms.
/// The xaml files are located at Toolkit.UITests.Wpf.App, Toolkit.UITests.WinUI.App, and Toolkit.UITests.Maui.App
/// under TestPages/PageName.xaml.
/// </remarks>
public partial class PageName : TestPage
{
    public PageName()
    {
        InitializeComponent();

        // Make sure any online map, scene, or layer used in a test is Public
        var map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        MainMapView.Map = map;
    }
}