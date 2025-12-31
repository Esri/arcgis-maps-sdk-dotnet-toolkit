using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.ScaleLine;

internal class ScaleLineTests : AppiumTestBase
{
    private const string ScaleLineMetricValueId = "ScaleLineMetricValue";

    [Test]
    public async Task ScaleLineUpdatesOnZoom()
    {
        OpenSample("ScaleLine");

        await Task.Delay(500);
        var mapView = FindElement("MainMapView");
        var mapViewRect = mapView.Rect;

        var lastScaleValue = GetScaleUnitsPerPixel(ScaleLineMetricValueId, ScaleLineType.Advanced);

        var mapCenterX = mapViewRect.X + mapViewRect.Width / 2;
        var mapCenterY = mapViewRect.Y + mapViewRect.Height / 2;

        // Test zoom in
        ZoomIn(mapCenterX, mapCenterY);
        await Task.Delay(1000);

        var currentScaleValue = GetScaleUnitsPerPixel(ScaleLineMetricValueId, ScaleLineType.Advanced);
        if (currentScaleValue >= lastScaleValue)
        {
            Console.WriteLine($"Scale line did not update correctly after zooming in. Last: {lastScaleValue:F}, Current: {currentScaleValue:F}");
        }
        else
        {
            Console.WriteLine($"Scale line updated correctly after zooming in. Last: {lastScaleValue:F}, Current: {currentScaleValue:F}");
        }
        lastScaleValue = currentScaleValue;

        // Test pan up (on mercator map this should distort the scale so that it is larger)
        DragCoordinates(mapCenterX, mapCenterY - 100, mapCenterX, mapCenterY + 100);
        await Task.Delay(1000);

        currentScaleValue = GetScaleUnitsPerPixel(ScaleLineMetricValueId, ScaleLineType.Advanced);
        if (currentScaleValue >= lastScaleValue)
        {
            Console.WriteLine($"Scale line did not update correctly after panning up. Last: {lastScaleValue:F}, Current: {currentScaleValue:F}");
        }
        else
        {
            Console.WriteLine($"Scale line updated correctly after panning up. Last: {lastScaleValue:F}, Current: {currentScaleValue:F}");
        }
        lastScaleValue = currentScaleValue;
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
        var initialScaleValue = int.Parse(scaleValueElement.GetAttribute("Name"));

        var scaleLineLength = scaleValueElement.Location.X - scaleLineElement.Rect.Left;
        return initialScaleValue / (float)scaleLineLength;
    }

    private enum ScaleLineType
    {
        Advanced,
        Simple
    }
}
