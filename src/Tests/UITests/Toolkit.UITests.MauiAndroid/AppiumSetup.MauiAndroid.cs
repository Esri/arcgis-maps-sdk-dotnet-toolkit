using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    private static string MauiAppPackage = @"com.esri.toolkit.uitests.maui";

    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var settings = GetBuildSettings();

        if (settings["usePreinstalledApp"] == "true")
        {
            var appPackage = settings["appPackage"] ?? MauiAppPackage;
            driver = MakeAndroidDriver(true, appPackage, settings);
        }
        else
        {
            var appDirectory = settings["appDirectory"];
            var appPath = FindApp(appDirectory);
            driver = MakeAndroidDriver(false, appPath, settings);
        }

        var screenDensityElement = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("text"));
    }

    private static string FindApp(string appDirectory)
    {
        var signedApks = Directory.GetFiles(appDirectory, $"{MauiAppPackage}*Signed.apk", SearchOption.TopDirectoryOnly);
        if (signedApks.Length > 0)
            return signedApks[0];


        var unsignedApks = Directory.GetFiles(appDirectory, $"{MauiAppPackage}*.apk", SearchOption.TopDirectoryOnly);
        if (unsignedApks.Length > 0)
            return unsignedApks[0];

        throw new FileNotFoundException($"Could not find any apk files in given directory. Please make sure the BuildTestApp build target ran successfully. Directory searched: {appDirectory}");
    }
}