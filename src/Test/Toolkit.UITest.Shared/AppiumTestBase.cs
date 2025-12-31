using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

internal abstract partial class AppiumTestBase
{
    protected AppiumDriver Driver => AppiumSetup.Driver;
    protected TestPlatform Platform => TestPlatform.WPF;

    protected enum TestPlatform
    {
        WinUI,
        WPF,
        MauiWinUI,
        MauiAndroid,
        MauiIOS
    }
}
