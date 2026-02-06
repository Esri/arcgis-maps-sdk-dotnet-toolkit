using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var mauiAppPackage = @"com.esri.toolkit.uitests.maui";
        var mauiAppIntent = "crc6424445f98e2c29dbe.MainActivity";

        driver = MakeAndroidDriver(mauiAppPackage, mauiAppIntent);

        var screenDensityElement = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("text"));
    }
}