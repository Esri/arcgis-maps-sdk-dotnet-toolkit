#if WINDOWS_TEST
using OpenQA.Selenium.Appium.Windows;
#elif ANDROID_TEST
using OpenQA.Selenium.Appium.Android;
#elif IOS_TEST
using OpenQA.Selenium.Appium.iOS;
#elif MAC_TEST
using OpenQA.Selenium.Appium.Mac;
#endif

namespace Toolkit.UITest.Shared;

internal abstract partial class AppiumTestBase
{
#if WINDOWS_TEST
    protected WindowsDriver Driver => AppiumSetup.Driver;
#elif ANDROID_TEST
    protected AndroidDriver Driver => AppiumSetup.Driver;
#elif IOS_TEST
    protected IOSDriver Driver => AppiumSetup.Driver;
#elif MAC_TEST
    protected MacDriver Driver => AppiumSetup.Driver;
#endif
}
