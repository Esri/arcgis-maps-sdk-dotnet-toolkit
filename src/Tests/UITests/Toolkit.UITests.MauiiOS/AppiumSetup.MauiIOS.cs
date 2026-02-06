using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";
        var deviceUdid = @"YOUR_DEVICE_UDID";

        driver = MakeiOSDriver(deviceUdid, MauiSamplesApp);

        var screenDensityElement = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("label"));
    }
}