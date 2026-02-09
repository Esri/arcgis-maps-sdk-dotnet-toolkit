using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var MauiSamplesApp = @"com.esri.toolkit.uitests.maui";
        var deviceUdid = @"YOUR_DEVICE_UDID";
        bool usePreinstalledWDA = true;

        driver = MakeiOSDriver(deviceUdid, MauiSamplesApp, usePreinstalledWDA);

        var screenDensityElement = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("label"));
    }
}