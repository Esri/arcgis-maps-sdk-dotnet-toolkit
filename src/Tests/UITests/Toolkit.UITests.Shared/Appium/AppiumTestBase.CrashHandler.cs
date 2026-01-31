using OpenQA.Selenium;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    private static bool HasAppCrashed = false;

    [TestInitialize]
    public void FailFastIfAppAlreadyCrashed()
    {
        if (HasAppCrashed)
            Assert.Inconclusive("Skipping because the app previously crashed and the Appium session is no longer valid");
    }

    [TestCleanup]
    public void CheckNotCrashed()
    {
        var ex = TestContext.TestException;
        if (ex is NoSuchWindowException || (ex is WebDriverTimeoutException && ex.InnerException is NoSuchWindowException))
        {
            HasAppCrashed = true;
            TestContext.WriteLine($"Test app window not found. Marked app as crashed.");
        }
    }
}