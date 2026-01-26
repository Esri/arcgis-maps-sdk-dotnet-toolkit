using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Windows;

namespace Toolkit.UITest.Shared;

[TestClass]
public static partial class AppiumSetup
{
#if WINDOWS_TEST
    private static WindowsDriver? driver;
    public static WindowsDriver? Driver => driver;
#elif ANDROID_TEST
    private static AndroidDriver? driver;
    public static AndroidDriver? Driver => driver;
#elif IOS_TEST
    private static IOSDriver? driver;
    public static IOSDriver? Driver => driver;
#elif MAC_TEST
    private static MacDriver? driver;
    public static MacDriver? Driver => driver;
#endif

    private static WindowsDriver MakeWindowsDriver(string app, string port = "4723", int timeoutSeconds = 10)
    {
        var serverUri = new Uri(Environment.GetEnvironmentVariable("APPIUM_HOST") ?? "http://127.0.0.1:" + port);
        var driverOptions = new AppiumOptions()
        {
            AutomationName = "Windows",
            PlatformName = "Windows",
            DeviceName = "WindowsPC",
            App = app
        };

        var windowsDriver = new WindowsDriver(serverUri, driverOptions, TimeSpan.FromSeconds(timeoutSeconds));
        windowsDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(timeoutSeconds);

        return windowsDriver;
    }

    private static AndroidDriver MakeAndroidDriver(string appPackage, string port = "4723", int timeoutSeconds = 10)
    {
        var serverUri = new Uri(Environment.GetEnvironmentVariable("APPIUM_HOST") ?? "http://127.0.0.1:" + port);
        var driverOptions = new AppiumOptions()
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2",
        };
        driverOptions.AddAdditionalAppiumOption("appPackage", appPackage);

        var androidDriver = new AndroidDriver(serverUri, driverOptions, TimeSpan.FromSeconds(timeoutSeconds));
        androidDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(timeoutSeconds);

        return androidDriver;
    }

    private static IOSDriver MakeiOSDriver(string deviceUdid, string bundleId, string port = "4723", int timeoutSeconds = 20)
    {
        var serverUri = new Uri(Environment.GetEnvironmentVariable("APPIUM_HOST") ?? "http://127.0.0.1:" + port);
        var driverOptions = new AppiumOptions()
        {
            PlatformName = "iOS",
            AutomationName = "XCUITest",
        };
        driverOptions.AddAdditionalAppiumOption("bundleId", bundleId);
        driverOptions.AddAdditionalAppiumOption("udid", deviceUdid);
        driverOptions.AddAdditionalAppiumOption("showXcodeLog", true);

        var iosDriver = new IOSDriver(serverUri, driverOptions, TimeSpan.FromSeconds(timeoutSeconds));
        iosDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(timeoutSeconds);

        return iosDriver;
    }

    private static MacDriver MakeMacDriver(string bundleId, string port = "4723", int timeoutSeconds = 10)
    {
        var serverUri = new Uri(Environment.GetEnvironmentVariable("APPIUM_HOST") ?? "http://127.0.0.1:" + port);
        var driverOptions = new AppiumOptions()
        {
            PlatformName = "mac",
            AutomationName = "mac2",
        };
        driverOptions.AddAdditionalAppiumOption("bundleId", bundleId);

        var macDriver = new MacDriver(serverUri, driverOptions, TimeSpan.FromSeconds(timeoutSeconds));
        macDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(timeoutSeconds);

        return macDriver;
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        driver?.Quit();
        driver?.Dispose();
    }
}
