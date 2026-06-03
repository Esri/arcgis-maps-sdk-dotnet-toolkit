using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    private const string AppBundleIdentifier = @"com.esri.toolkit.uitests.maui";

    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext testContext)
    {
        var appPath = Environment.GetEnvironmentVariable("TKUITEST_APP");
        if (!string.IsNullOrWhiteSpace(appPath))
            driver = MakeMacDriver(appPath, true);
        else
            driver = MakeMacDriver(AppBundleIdentifier, false);

        try
        {
            driver.Manage().Window.FullScreen();
        }
        catch
        {
            testContext.WriteLine("Could not fullscreen app. It may have already opened in fullscreen mode.");
        }

        var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(2000));
        var screenDensityElement = wait.Until(d => d.FindElement(MobileBy.Id("ScreenDensity")));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("label")!);
    }
}