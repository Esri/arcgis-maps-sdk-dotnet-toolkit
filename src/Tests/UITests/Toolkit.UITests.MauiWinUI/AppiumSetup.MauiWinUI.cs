using OpenQA.Selenium.Appium;
using System.Reflection;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var mauiSamplesApp = GetBuildSettings()["app"];

        driver = MakeWindowsDriver(mauiSamplesApp);

        driver.Manage().Window.Maximize();

        var screenDensityElement = driver.FindElement(MobileBy.AccessibilityId("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("Name"));
    }
}