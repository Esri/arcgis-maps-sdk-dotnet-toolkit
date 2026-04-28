using OpenQA.Selenium.Appium;
using ImageMagick;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    /// <summary>
    /// Takes a screenshot of the parameter element.
    /// </summary>
    /// <remarks>
    /// Appium screenshots work on the entire screen, not just a single element, or even a single window. This means elements
    /// may be partially or wholly blocked if covered by another element or window. Blue light filters will also affect color
    /// comparisons because of this.
    /// </remarks>
    protected MagickImage GetScreenshot(AppiumElement element)
    {
        var screenshot = element.GetScreenshot();
        return new MagickImage(screenshot.AsByteArray);
    }
}
