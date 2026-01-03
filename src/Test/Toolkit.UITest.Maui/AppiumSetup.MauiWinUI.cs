using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"91226bae-9931-4128-86d9-0452a67f1bc2_9zz4h110yvjzm!App";

        driver = MakeWindowsDriver(MauiSamplesApp);

        var maximizeButton = Driver!.FindElement(MobileBy.Name("Maximize"));
        maximizeButton.Click();

        Task.Delay(500).Wait();
    }
}