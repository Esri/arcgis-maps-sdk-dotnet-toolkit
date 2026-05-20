using OpenQA.Selenium.Appium;
using System.Diagnostics;
using System.Reflection;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var envAppPath = Environment.GetEnvironmentVariable("UITEST_APP_PATH");
        var testApp = String.IsNullOrWhiteSpace(envAppPath) ? GetBuildSettings()["app"] : envAppPath;

        driver = MakeWindowsDriver(testApp);

        driver.Manage().Window.Maximize();

        var screenDensityElement = driver.FindElement(MobileBy.AccessibilityId("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("Name"));
    }
}