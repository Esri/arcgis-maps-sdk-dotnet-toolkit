using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.ScaleLine;

[TestClass]
public class ScaleLineTests : AppiumTestBase
{
    private const string ScaleLineMetricValueId = "ScaleLineMetricValue";

    [TestMethod]
    public async Task AdvancedScaleLineUpdatesOnZoomAndPan()
    {
        OpenSample("ScaleLineRenders");

        await Task.Delay(2000);

        // Verify initial render
        var advancedScaleLine = FindElement("AdvancedScaleLine");
        CompareBaseline("AdvancedScaleLineInitialViewTest", advancedScaleLine);

        // Get map center coordinates
        var mapView = FindElement("MainMapView");
        var mapCenterX = mapView.Rect.X + mapView.Rect.Width / 2;
        var mapCenterY = mapView.Rect.Y + mapView.Rect.Height / 2;

        // Starting scale value
        var lastScaleValue = GetScaleUnitsPerPixel(ScaleLineMetricValueId, ScaleLineType.Advanced);

        // Test zoom in
        ZoomIn(mapCenterX, mapCenterY);
        await Task.Delay(2000);

        var currentScaleValue = GetScaleUnitsPerPixel(ScaleLineMetricValueId, ScaleLineType.Advanced);
        Assert.IsGreaterThan(lastScaleValue, currentScaleValue, $"Scale line did not update correctly after zooming in. Last: {lastScaleValue:F}, Current: {currentScaleValue:F}");
        lastScaleValue = currentScaleValue;

        // Test pan up (on mercator map this should distort the scale so that it is larger)
        var dragDistance = mapView.Rect.Height / 3;
        DragCoordinates(mapCenterX, mapCenterY - dragDistance / 2, mapCenterX, mapCenterY + dragDistance / 2);
        await Task.Delay(2000);

        currentScaleValue = GetScaleUnitsPerPixel(ScaleLineMetricValueId, ScaleLineType.Advanced);
        Assert.IsGreaterThan(lastScaleValue, currentScaleValue, $"Scale line did not update correctly after panning up. Last: {lastScaleValue:F}, Current: {currentScaleValue:F}");
    }

    private float GetScaleUnitsPerPixel(string elementId, ScaleLineType type)
    {
        // There are two scale lines in the sample. The first is simple, the second is advanced.
        if (type == ScaleLineType.Advanced)
            return GetScale(FindElement("AdvancedScaleLine"));
        else if (type == ScaleLineType.Simple)
            return GetScale(FindElement("SimpleScaleLine"));
        else
            throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid scale line type.");
    }

    private float GetScale(AppiumElement scaleLineElement)
    {
        var scaleValueElement = FindElement(scaleLineElement, "ScaleLineMetricValue");
        var scaleValue = int.Parse(GetElementText(scaleValueElement));

        var scaleLineLength = scaleValueElement.Location.X - scaleLineElement.Rect.Left;
        TestContext.WriteLine($"Scale line length in pixels: {scaleLineLength}, scale value: {scaleValue}");
        return scaleLineLength / (float)scaleValue;
    }

    private enum ScaleLineType
    {
        Advanced,
        Simple
    }
}
