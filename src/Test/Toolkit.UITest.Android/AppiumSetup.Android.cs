namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"";

        appiumLocalService = StartServer();
        driver = MakeAndroidDriver(MauiSamplesApp);

        Task.Delay(500).Wait();
    }
}