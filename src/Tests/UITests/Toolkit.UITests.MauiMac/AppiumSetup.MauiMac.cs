using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";

        driver = MakeMacDriver(MauiSamplesApp);

        try
        {
            driver.Manage().Window.FullScreen();
        }
        catch
        {
            testContext.WriteLine("Could not fullscreen app. It may have already opened in fullscreen mode.");
        }

        var screenDensityElement = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("label"));
    }
}