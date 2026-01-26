using OpenQA.Selenium.Appium;
using System.Collections.ObjectModel;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    protected ReadOnlyCollection<AppiumElement> FindElements(string name)
    {
#if WINDOWS_TEST
        return Driver.FindElements(MobileBy.AccessibilityId(name));
#else
        return Driver.FindElements(MobileBy.Id(name));
#endif
    }

    protected AppiumElement FindElement(string id)
    {
#if WINDOWS_TEST
        return Driver.FindElement(MobileBy.AccessibilityId(id));
#else
        return Driver.FindElement(MobileBy.Id(id));
#endif
    }

    protected AppiumElement FindElement(AppiumElement parent, string id)
    {
#if WINDOWS_TEST
        return parent.FindElement(MobileBy.AccessibilityId(id));
#else
        return parent.FindElement(MobileBy.Id(id));
#endif
    }

    protected AppiumElement FindElementByName(string name)
    {
#if ANDROID_TEST
        return Driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{name}\")"));
#else
        return Driver.FindElement(MobileBy.Name(name));
#endif
    }

    protected string GetElementText(AppiumElement element)
    {
#if WINDOWS_TEST
        return element.GetAttribute("Name");
#elif ANDROID_TEST
        return element.GetAttribute("text");
#elif MAC_TEST || IOS_TEST
        return element.GetAttribute("label");
#else
        throw new NotImplementedException("FindElement(AppiumElement,string) is not implemented for this platform.");
#endif
    }
}