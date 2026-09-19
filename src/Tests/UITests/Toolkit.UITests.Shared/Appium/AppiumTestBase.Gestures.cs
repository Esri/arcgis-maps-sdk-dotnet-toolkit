using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    protected void Click(AppiumElement element)
    {
        element.Click();
    }

    protected void ClearEntry(AppiumElement element)
    {
#if !MAC_TEST
        element.Clear();
#else
        // Currently doing this manually is significantly faster on Mac
        Click(element);
        Driver.ExecuteScript("macos: keys", new Dictionary<string, object>
        {
            {"elementId", element.Id},
            {"keys", new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    {"key", "a"},
                    {"modifierFlags", 1<<4}
                },
                new Dictionary<string, object>
                {
                    {"key", "XCUIKeyboardKeyDelete"},
                    {"modifierFlags", 0}
                }
            }}
        });
#endif
    }

    protected void SubmitText(AppiumElement element, string text)
    {
#if ANDROID_TEST
        Click(element);
#endif
        ClearEntry(element);
        element.SendKeys(text);
        PressEnter(element);
    }

#if WINDOWS_TEST
    // Sends a single key press (down then up) as a global keystroke, landing on whatever element
    // currently has focus - e.g. after Click()-ing a button. Used for testing keyboard navigation
    // (arrow keys) that isn't well supported through IWebElement.SendKeys on the Windows driver.
    protected void PressKey(int virtualKeyCode)
    {
        var actions = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                {"virtualKeyCode", virtualKeyCode },
                {"down", true }
            },
            new Dictionary<string, object>
            {
                {"virtualKeyCode", virtualKeyCode },
                {"down", false }
            }
        };

        Driver.ExecuteScript("windows: keys", new Dictionary<string, object>
        {
            {"actions",  actions}
        });
    }
#endif

    protected void PressEnter(
#if IOS_TEST || MAC_TEST
    AppiumElement element
#else
    AppiumElement? element = null
#endif
    )
    {
#if WINDOWS_TEST
        PressKey(0x0D);
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
