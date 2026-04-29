using ImageMagick;
using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.ControlName;

[TestClass]
public class TestsName : AppiumTestBase
{
    private const string PageNamePage = "PageName";

    [TestMethod]
    public async Task ControlName_Renders()
    {
        OpenSample(PageNamePage);

        /*
        Add test logic here

        Common methods provided by AppiumTestBase include FindElement(), Click(), GetScreenshot(), etc.
        See existing tests for reference on how these methods are used.
        */
    }
}