using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Windows;
using System.Collections.ObjectModel;

namespace Toolkit.UITest.Shared;

internal abstract partial class AppiumTestBase
{
    protected ReadOnlyCollection<AppiumElement> FindElements(string name)
    {
        if (Driver is WindowsDriver)
        {
            return Driver.FindElements(MobileBy.AccessibilityId(name));
        }
        else if (Driver is AndroidDriver)
        {
            return Driver.FindElements(MobileBy.Id(name));
        }
        else
        {
            throw new NotSupportedException($"Driver of type {Driver.GetType().FullName} is not supported");
        }
    }

    protected AppiumElement FindElement(string id)
    {
        if (Driver is WindowsDriver)
        {
            return Driver.FindElement(MobileBy.AccessibilityId(id));
        }
        else if (Driver is AndroidDriver)
        {
            return Driver.FindElement(MobileBy.Id(id));
        }
        else
        {
            throw new NotSupportedException($"Driver of type {Driver.GetType().FullName} is not supported");
        }
    }

    protected AppiumElement FindElement(AppiumElement parent, string id)
    {
        if (Driver is WindowsDriver)
        {
            return parent.FindElement(MobileBy.AccessibilityId(id));
        }
        else if (Driver is AndroidDriver)
        {
            return parent.FindElement(MobileBy.Id(id));
        }
        else
        {
            throw new NotSupportedException($"Driver of type {Driver.GetType().FullName} is not supported");
        }
    }

    protected AppiumElement FindElementByName(string name)
    {
        if (Driver is WindowsDriver)
        {
            return Driver.FindElement(MobileBy.Name(name));
        }
        else if (Driver is AndroidDriver)
        {
            return Driver.FindElement(MobileBy.Name(name));
        }
        else
        {
            throw new NotSupportedException($"Driver of type {Driver.GetType().FullName} is not supported");
        }
    }
}