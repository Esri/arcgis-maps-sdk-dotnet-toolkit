using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    protected void SubmitText(AppiumElement element, string text)
    {
#if ANDROID_TEST
        element.Click();
        element.Clear();
#elif MAC_TEST
        element.Click();
        Driver.ExecuteScript("macos: keys", new Dictionary<string, object>
        {
            {"elementId", element.Id},
            {"keys", new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    {"key", "a"},
                    {"modifierFlags", 1<<4}
                }
            }}
        });
#else
        element.Clear();
#endif
        element.SendKeys(text);
        PressEnter(element);
    }

    protected void PressEnter(
#if IOS_TEST || MAC_TEST
    AppiumElement element
#else
    AppiumElement? element = null
#endif
    )
    {
#if WINDOWS_TEST
        var actions = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                {"virtualKeyCode", 0x0D },
                {"down", true }
            },
            new Dictionary<string, object>
            {
                {"virtualKeyCode", 0x0D },
                {"down", false }
            }
        };

        Driver.ExecuteScript("windows: keys", new Dictionary<string, object>
        {
            {"actions",  actions}
        });
#elif ANDROID_TEST
        // This only works if the keyboard is open, you might need to click on the input first to open it
        Driver.ExecuteScript("mobile: performEditorAction", new Dictionary<string, object>
        {
            {"action", "done" }
        });
#elif IOS_TEST
        element.SendKeys("\n");
#elif MAC_TEST
        Driver.ExecuteScript("macos: keys", new Dictionary<string, object>
        {
            {"keys", new List<string> {"XCUIKeyboardKeyReturn"} }
        });
#else
        throw new NotImplementedException("PressEnter is not implemented for this platform.");
#endif
    }
}
