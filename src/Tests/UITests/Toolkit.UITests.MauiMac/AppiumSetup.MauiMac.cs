using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext testContext)
    {
        var MauiSamplesApp = @"com.esri.toolkit.uitests.maui";

        driver = MakeMacDriver(MauiSamplesApp);

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