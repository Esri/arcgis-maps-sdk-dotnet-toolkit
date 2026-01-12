using OpenQA.Selenium.Appium;
using System.Diagnostics;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    internal static float? ScreenDensity;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";

        driver = MakeMacDriver(MauiSamplesApp);

        try
        {
            driver.Manage().Window.FullScreen();
            Task.Delay(500).Wait();
        }
        catch
        {
            TestContext.Out.WriteLine("Could not fullscreen app. It may have already opened in fullscreen mode.");
        }

        var densityLabel = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(densityLabel.GetAttribute("label"));
    }
}