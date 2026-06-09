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
        var action = () => Driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().description(\"{name}\")"));
#elif WINDOWS_TEST
        var action = () => Driver.FindElement(MobileBy.Name(name));
#elif MAC_TEST || IOS_TEST
        var action = () => Driver.FindElement(MobileBy.XPath($"//*[@label=\"{name}\" or @name=\"{name}\"]"));
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

    protected AppiumElement FindElementByText(string text, TimeSpan? timeout = null)
    {
#if ANDROID_TEST
        var action = () => Driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{text}\")"));
#elif MAC_TEST || IOS_TEST
        var action = () => Driver.FindElement(MobileBy.XPath($"//*[@label=\"{text}\" or @value=\"{text}\" or @name=\"{text}\"]"));
#else
        var action = () => Driver.FindElement(MobileBy.Name(text));
#endif
        try
        {
            return OptionalWaitCall(action, timeout);
        }
        catch (Exception)
        {
            TestContext.WriteLine($"No elements found with text \"{text}\". See exception for details.");
            throw;
        }
    }

    protected bool ElementExistsByName(string name, TimeSpan? timeout = null)
    {
        try
        {
            FindElementByName(name, timeout);
            return true;
        }
        catch
        {
            return false;
        }
    }
    protected bool ElementExistsById(string id, TimeSpan? timeout = null)
    {
        try
        {
            FindElement(id, timeout);
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected bool ElementExistsByText(string text, TimeSpan? timeout = null)
    {
        try
        {
            FindElementByText(text, timeout);
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected string GetLabelText(AppiumElement element, TimeSpan? timeout = null)
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

    protected string GetEntryText(AppiumElement element, TimeSpan? timeout = null)
    {

#if ANDROID_TEST
        var action = () =>  element.GetAttribute("text");
#elif WINDOWS_TEST
        var action = () => element.GetAttribute("Value.Value");
#elif MAC_TEST || IOS_TEST
        var action = () =>
        {
            var value = element.GetAttribute("value");
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var placeholder = element.GetAttribute("placeholderValue");
            return string.IsNullOrWhiteSpace(placeholder) ? string.Empty : placeholder;
        };
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
}