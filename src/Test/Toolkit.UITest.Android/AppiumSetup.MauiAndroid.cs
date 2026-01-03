namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";

        driver = MakeAndroidDriver(MauiSamplesApp);

        Task.Delay(500).Wait();
    }
}