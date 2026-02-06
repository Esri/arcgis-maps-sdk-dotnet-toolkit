namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    protected void PressEnter()
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
#else
        throw new NotImplementedException("PressEnter is not implemented for this platform.");
#endif
    }
}
