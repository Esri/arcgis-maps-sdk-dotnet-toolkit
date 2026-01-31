using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    protected void OpenSample(string sampleName)
    {
        var searchBox = FindElement("TestSearchBox");
        searchBox.Clear();
        searchBox.SendKeys(sampleName);
        PressEnter();
    }

    private T OptionalWaitCall<T>(Func<T> call, TimeSpan? timeout = null)
    {
        if (timeout is null || timeout.Value.TotalMilliseconds <= 0)
        {
            return call();
        }
        var wait = new WebDriverWait(Driver, timeout.Value);
        return wait.Until(d => call());
    }

    protected ReadOnlyCollection<AppiumElement> FindElements(string name, TimeSpan? timeout = null)
    {
#if WINDOWS_TEST
        var action = () => Driver.FindElements(MobileBy.AccessibilityId(name));
#else
        var action = () => Driver.FindElements(MobileBy.Id(name));
#endif
        return OptionalWaitCall(action, timeout);
    }

    protected AppiumElement FindElement(string id, TimeSpan? timeout = null)
    {
#if WINDOWS_TEST
        var action = () => Driver.FindElement(MobileBy.AccessibilityId(id));
#else
        var action = () => Driver.FindElement(MobileBy.Id(id));
#endif
        return OptionalWaitCall(action, timeout);
    }

    protected AppiumElement FindElement(AppiumElement parent, string id, TimeSpan? timeout = null)
    {
#if WINDOWS_TEST
        var action = () => parent.FindElement(MobileBy.AccessibilityId(id));
#else
        var action = () => parent.FindElement(MobileBy.Id(id));
#endif
        return OptionalWaitCall(action, timeout);
    }

    protected AppiumElement FindElementByName(string name, TimeSpan? timeout = null)
    {
#if ANDROID_TEST
        var action = () => Driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{name}\")"));
#else
        var action = () => Driver.FindElement(MobileBy.Name(name));
#endif
        return OptionalWaitCall(action, timeout);
    }

    protected string GetElementText(AppiumElement element, TimeSpan? timeout = null)
    {
#if WINDOWS_TEST
        var action = () => element.GetAttribute("Name");
#elif ANDROID_TEST
        var action = () =>  element.GetAttribute("text");
#elif MAC_TEST || IOS_TEST
        var action = () =>  element.GetAttribute("label");
#else
        throw new NotImplementedException("FindElement(AppiumElement,string) is not implemented for this platform.");
#endif
        return OptionalWaitCall(action, timeout);
    }

    /// <summary>
    /// Normalizes pixel values based on platform and device DPI. Normalized values are approximately equivalent to what renders
    /// on Windows at a screen density of 1 (96 DPI).
    /// </summary>
    protected double GetNormalizedPixelValue(int pixelValue)
    {
#if WINDOWS_TEST
        return pixelValue / ScreenDensity;
#else
        // Mac baseline is ~72 DPI
        // iOS baseline is ~160 DPI
        // Android baseline is 160 DPI
        throw new NotImplementedException("GetNormalizedPixelValue(int) is not implemented for this platform.");
#endif
    }
}