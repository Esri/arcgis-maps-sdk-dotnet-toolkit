using System.Diagnostics;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";

        driver = MakeMacDriver(MauiSamplesApp);

        try
        {
            driver.Manage().Window.FullScreen();
        }
        catch
        {
            Debug.WriteLine("Could not fullscreen app. It may have already opened in fullscreen mode.");
        }

        Task.Delay(500).Wait();
    }
}