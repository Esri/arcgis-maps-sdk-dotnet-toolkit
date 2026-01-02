using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Windows;

namespace Toolkit.UITest.Shared;

[SetUpFixture]
internal partial class AppiumSetup
{
    private static AppiumLocalService? appiumLocalService;

#if WINDOWS_TEST
    private static WindowsDriver? driver;
    internal static WindowsDriver? Driver => driver;
#elif ANDROID_TEST
    private static AndroidDriver? driver;
    internal static AndroidDriver? Driver => driver;
#elif IOS_TEST
    private static IOSDriver? driver;
    internal static IOSDriver? Driver => driver;
#endif

    private WindowsDriver MakeWindowsDriver(string app, string port = "4723", int timeoutSeconds = 10)
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

    private AndroidDriver MakeAndroidDriver(string appPackage, string port = "4723", int timeoutSeconds = 10)
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

    private IOSDriver MakeiOSDriver(string bundleId, string port = "4723", int timeoutSeconds = 10)
    {
        throw new NotImplementedException("iOS tests are not yet implemented.");
    }

    private IOSDriver MakeMacDriver(string bundleId, string port = "4723", int timeoutSeconds = 10)
    {
        throw new NotImplementedException("MacCatalyst tests are not yet implemented.");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        driver?.Quit();
        driver?.Dispose();
    }
}
