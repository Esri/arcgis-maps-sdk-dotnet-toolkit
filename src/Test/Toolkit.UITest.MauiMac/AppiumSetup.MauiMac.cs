namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";

        driver = MakeMacDriver(MauiSamplesApp);

        // TODO: Enter fullscreen mode

        Task.Delay(500).Wait();
    }
}