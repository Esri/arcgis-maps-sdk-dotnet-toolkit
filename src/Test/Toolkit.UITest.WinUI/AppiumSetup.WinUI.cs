using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Windows;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var WinUISamplesApp = @"1a3f5c3d-27ca-45dd-bbe3-5e2fad821f9d_btzmr6n615d7a!App";

        driver = MakeWindowsDriver(WinUISamplesApp);

        var maximizeButton = Driver!.FindElement(MobileBy.Name("Maximize"));
        maximizeButton.Click();

        Task.Delay(500).Wait();
    }
}