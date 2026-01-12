using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    internal static float? ScreenDensity;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";
        var deviceUdid = @"YOUR_DEVICE_UDID";

        driver = MakeiOSDriver(deviceUdid, MauiSamplesApp);

        Task.Delay(500).Wait();

        var densityLabel = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(densityLabel.GetAttribute("label"));
    }
}