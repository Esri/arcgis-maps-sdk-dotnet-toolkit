
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
}
