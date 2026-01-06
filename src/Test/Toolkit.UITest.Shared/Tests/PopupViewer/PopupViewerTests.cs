

namespace Toolkit.UITest.Shared.Tests.PopupViewer;

internal class PopupViewerTests : AppiumTestBase
{
#if WPF_TEST || WINUI_TEST
    [Test]
    public async Task PopupViewerRendersText()
    {
        OpenSample("PopupViewer");
        await Task.Delay(1000);

        FindElement("ZoomWhiteMountainButton").Click();

        await Task.Delay(1000);

        var mapView = FindElement("MainMapView");
        var mapCenterX = mapView.Rect.X + mapView.Rect.Width / 2;
        var mapCenterY = mapView.Rect.Y + mapView.Rect.Height / 2;

        TapCoordinates(mapCenterX, mapCenterY);

#if WINUI_TEST
        var paragraph1Text = "White Mountain Peak is a peak in California's White Mountains range. It ranks #3 among the California Fourteeners.";
        var description = FindElementByName(paragraph1Text);
#elif WPF_TEST
        var description = FindElement("TextArea");
#endif
        CompareBaseline("PopupViewerTextRenderTest", description);

        await Task.Delay(2000);
    }
#endif
}
