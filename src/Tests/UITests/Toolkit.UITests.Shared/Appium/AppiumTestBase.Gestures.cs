
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
#else
        throw new NotImplementedException("PressEnter is not implemented for this platform.");
#endif
    }
}
