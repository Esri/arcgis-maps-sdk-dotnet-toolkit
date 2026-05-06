using ImageMagick;
using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.ControlName;

[TestClass]
public class TestsName : AppiumTestBase
{
    private const string PageNamePage = "PageName";

    [TestMethod]
    public async Task ControlName_CompassAutoHideExample()
    {
        OpenSample(PageNamePage);
        var compassElement = FindElement("MapCompass");

        // Trigger auto hide
        var autoHideButton = FindElement("ToggleAutoHideButton");
        Click(autoHideButton);

        // Verify the compass fades out
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
}