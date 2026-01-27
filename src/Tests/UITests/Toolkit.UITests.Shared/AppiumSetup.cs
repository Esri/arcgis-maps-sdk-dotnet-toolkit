using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace Toolkit.UITest.Shared;

[TestClass]
public static partial class AppiumSetup
{
#if WINDOWS_TEST
    private static WindowsDriver? driver;
    public static WindowsDriver? Driver => driver;
#endif

    public static double? ScreenDensity { get; private set; }

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

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        driver?.Quit();
        driver?.Dispose();
    }
}
