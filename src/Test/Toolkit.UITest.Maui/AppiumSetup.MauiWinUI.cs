using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    internal static float? ScreenDensity;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"91226bae-9931-4128-86d9-0452a67f1bc2_9zz4h110yvjzm!App";

        driver = MakeWindowsDriver(MauiSamplesApp);

        driver.Manage().Window.Maximize();

        var dpiLabel = driver.FindElement(MobileBy.AccessibilityId("ScreenDensity"));
        ScreenDensity = float.Parse(dpiLabel.GetAttribute("Name"));

        Task.Delay(500).Wait();
    }
}