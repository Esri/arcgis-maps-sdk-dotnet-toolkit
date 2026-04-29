using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var buildSettings = GetBuildSettings();

        var app = buildSettings["app"];
        var udid = buildSettings["deviceUdid"];
        if (string.IsNullOrEmpty(udid))
            throw new InvalidOperationException("Device UDID not found in build settings. Set this value in src/Tests/UITests/Directory.Build.props");

        driver = MakeiOSDriver(app, udid, buildSettings);

        var screenDensityElement = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("label"));
    }
}