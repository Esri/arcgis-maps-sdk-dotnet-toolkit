using System.Collections;
using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public static partial class AppiumSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        var buildFile = GetBuildFile();
        var buildSettings = Path.Exists(buildFile) ? GetBuildSettings() : new Dictionary<string, string>();

        var app = Environment.GetEnvironmentVariable("TKUITEST_APP") ?? buildSettings["app"];
        var udid = Environment.GetEnvironmentVariable("TKUITEST_DEVICE") ?? buildSettings["deviceUdid"];
        if (string.IsNullOrEmpty(udid))
            throw new InvalidOperationException("Device UDID not found in build settings. Set this value in src/Tests/UITests/Directory.Build.props");

        var environmentVariables = Environment.GetEnvironmentVariables();
        foreach (DictionaryEntry variable in environmentVariables)
        {
            if (variable.Key is string key && key.StartsWith("TKUITEST_PARAM_"))
            {
                buildSettings[key.Substring(15)] = (string)variable.Value;
            }
        }

        driver = MakeiOSDriver(app, udid, buildSettings);

        var screenDensityElement = driver.FindElement(MobileBy.Id("ScreenDensity"));
        ScreenDensity = float.Parse(screenDensityElement.GetAttribute("label"));
    }
}