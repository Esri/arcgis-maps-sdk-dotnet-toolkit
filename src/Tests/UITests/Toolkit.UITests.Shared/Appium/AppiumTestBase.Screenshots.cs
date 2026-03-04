using OpenQA.Selenium.Appium;
using ImageMagick;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    protected MagickImage GetScreenshot(AppiumElement element)
    {
        var screenshot = element.GetScreenshot();
        return new MagickImage(screenshot.AsByteArray);
    }
}
