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
        SubmitText(searchBox, sampleName);
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

    protected ReadOnlyCollection<AppiumElement> FindElements(string id, TimeSpan? timeout = null)
    {
#if WINDOWS_TEST
        var action = () => Driver.FindElements(MobileBy.AccessibilityId(id));
#else
        var action = () => Driver.FindElements(MobileBy.Id(id));
#endif
        try
        {
            return OptionalWaitCall(action, timeout);
        }
        catch (Exception)
        {
            TestContext.WriteLine($"No elements found with id \"{id}\". See exception for details.");
            throw;
        }
    }

    protected AppiumElement FindElement(string id, TimeSpan? timeout = null)
    {
#if WINDOWS_TEST
        var action = () => Driver.FindElement(MobileBy.AccessibilityId(id));
#else
        var action = () => Driver.FindElement(MobileBy.Id(id));
#endif
        try
        {
            return OptionalWaitCall(action, timeout);
        }
        catch (Exception)
        {
            TestContext.WriteLine($"No elements found with id \"{id}\". See exception for details.");
            throw;
        }
    }

    protected AppiumElement FindElement(AppiumElement parent, string id, TimeSpan? timeout = null)
    {
#if WINDOWS_TEST
        var action = () => parent.FindElement(MobileBy.AccessibilityId(id));
#else
        var action = () => parent.FindElement(MobileBy.Id(id));
#endif
        try
        {
            return OptionalWaitCall(action, timeout);
        }
        catch (Exception)
        {
            TestContext.WriteLine($"No child elements found with id \"{id}\". See exception for details.");
            throw;
        }
    }

    protected AppiumElement FindElementByName(string name, TimeSpan? timeout = null)
    {
#if ANDROID_TEST
        var action = () => Driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{name}\")"));
#else
        var action = () => Driver.FindElement(MobileBy.Name(name));
#endif
        try
        {
            return OptionalWaitCall(action, timeout);
        }
        catch (Exception)
        {
            TestContext.WriteLine($"No elements found with name \"{name}\". See exception for details.");
            throw;
        }
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
        try
        {
            return OptionalWaitCall(action, timeout);
        }
        catch (Exception)
        {
            TestContext.WriteLine($"Could not get text for element \"{element.Id}\". See exception for details.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes pixel values based on platform and device DPI. Normalized values are approximately equivalent to what renders
    /// on Windows at a screen density of 1 (96 DPI).
    /// </summary>
    protected double GetNormalizedPixelValue(int pixels)
    {
#if WINDOWS_TEST || ANDROID_TEST
        return pixels / ScreenDensity;
#elif IOS_TEST || MAC_TEST
        // XCUI tests already returns points rather than pixels
        return pixels;
#else
        throw new NotImplementedException("GetNormalizedPixelValue(int) is not implemented for this platform.");
#endif
    }

    protected int PlatformSpecificPixelValue(int windows, int android, int ios, int mac)
    {
#if WINDOWS_TEST
        return windows;
#elif ANDROID_TEST
        return android;
#elif IOS_TEST
        return ios;
#elif MAC_TEST
        return mac;
#endif
    }
}