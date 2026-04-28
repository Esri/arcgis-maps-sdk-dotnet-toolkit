using Toolkit.UITest.Shared;

namespace Toolkit.UITests.Shared.Compass;

[TestClass]
public class CompassTests : AppiumTestBase
{
    private const string CompassMapPage = "CompassMap";

    /// <summary>
    /// This is a very minimal test to show screenshots and very basic use of ImageMagick
    /// </summary>
    [TestMethod]
    [DataRow(CompassType.MapBound)]
    [DataRow(CompassType.HeadingBound)]
    public async Task Compass_AutoHides(CompassType compassType)
    {
        OpenSample(CompassMapPage);
        var compassElement = FindElement(compassType == CompassType.MapBound ? "MapCompass" : "HeadingCompass");

        // Check that the compass hides when facing north
        var autoHideButton = FindElement("ToggleAutoHideButton");
        SetRotation(0);
        Click(autoHideButton);

        var maxTries = 5;
        var tryCount = 0;
        while (tryCount < maxTries)
        {
            var screenshot = GetScreenshot(compassElement);
            // If nothing renders (ie. the screenshot is a uniform color) the compass successfully hid
            if (screenshot.Histogram().Count < 2)
                break;

            // Otherwise continue waiting
            tryCount++;
            if (tryCount < maxTries)
                await Task.Delay(500);
        }
        Assert.IsLessThan(maxTries, tryCount, "Exceeded timeout while waiting for the compass to hide.");
    }

    private void SetRotation(double rotation)
    {
        var rotationInputElement = FindElement("RotateInput");
        var rotateButtonElement = FindElement("RotateButton");
        SubmitText(rotationInputElement, rotation.ToString());
        Click(rotateButtonElement);
    }

    public enum CompassType
    {
        MapBound,
        HeadingBound
    }
}