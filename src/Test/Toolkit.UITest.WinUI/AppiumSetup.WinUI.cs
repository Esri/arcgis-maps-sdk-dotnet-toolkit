using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Windows;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    internal static float? ScreenDensity;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var WinUISamplesApp = @"1a3f5c3d-27ca-45dd-bbe3-5e2fad821f9d_btzmr6n615d7a!App";

        driver = MakeWindowsDriver(WinUISamplesApp);

        driver.Manage().Window.Maximize();

        var dpiLabel = driver.FindElement(MobileBy.AccessibilityId("ScreenDensity"));
        ScreenDensity = float.Parse(dpiLabel.GetAttribute("Name"));

        Task.Delay(500).Wait();
    }
}