using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var WinUIAppPackageId = @"d733bdd1-d63f-45f9-b119-555748d3b3e4_6b5psgtf36ad0!App";
        var envAppPath = Environment.GetEnvironmentVariable("UITEST_APP_PATH");
        var testApp = String.IsNullOrWhiteSpace(envAppPath) ? WinUIAppPackageId : envAppPath;

        driver = MakeWindowsDriver(testApp);

        driver.Manage().Window.Maximize();

        var dpiLabel = driver.FindElement(MobileBy.AccessibilityId("ScreenDensity"));
        ScreenDensity = float.Parse(dpiLabel.GetAttribute("Name"));
    }
}