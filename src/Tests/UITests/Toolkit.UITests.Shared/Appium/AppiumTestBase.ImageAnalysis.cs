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

    /// <summary>
    /// Normalizes pixel values based on platform and device DPI. Normalized values are approximately equivalent to what renders
    /// on Windows at a screen density of 1 (96 DPI).
    /// </summary>
    protected double GetNormalizedPixelValue(int pixels)
    {
#if WINDOWS_TEST || ANDROID_TEST
        return pixels / ScreenDensity;
#elif IOS_TEST || MAC_TEST
        // XCUI tests already returns points rather than pixels
        return pixels;
#else
        throw new NotImplementedException("GetNormalizedPixelValue(int) is not implemented for this platform.");
#endif
    }

    /// <summary>
    /// Returns the value corresponding to the target platform of the test runner.
    /// </summary>
    protected int PlatformSpecificPixelValue(int windows, int android, int ios, int mac)
    {
#if WINDOWS_TEST
        return windows;
#elif ANDROID_TEST
        return android;
#elif IOS_TEST
        return ios;
#elif MAC_TEST
        return mac;
#endif
    }
}
