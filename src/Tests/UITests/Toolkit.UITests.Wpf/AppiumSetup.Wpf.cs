using OpenQA.Selenium.Appium;
using System.Diagnostics;
using System.Reflection;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var wpfSamplesApp = GetSampleAppPath();

        driver = MakeWindowsDriver(wpfSamplesApp);

        driver.Manage().Window.Maximize();

        Task.Delay(500).Wait();

        var screenDensityElement = driver.FindElement(MobileBy.AccessibilityId("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("Name"));
    }

    private static string GetSampleAppPath()
    {
        var testAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Could not determine test assembly directory.");

        var pathFile = Path.Combine(testAssemblyDir, "PuppetAppPath.txt");
        if (!File.Exists(pathFile))
        {
            throw new FileNotFoundException(
                $"Missing '{pathFile}'. Ensure the 'BuildPuppetApp' MSBuild target ran before tests.");
        }

        var exePath = File.ReadAllText(pathFile).Trim();
        if (string.IsNullOrWhiteSpace(exePath))
        {
            throw new InvalidOperationException($"'{pathFile}' was empty.");
        }

        return exePath;
    }
}