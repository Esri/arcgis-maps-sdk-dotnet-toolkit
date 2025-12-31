namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"";

        appiumLocalService = StartServer();
        driver = MakeiOSDriver(MauiSamplesApp);

        Task.Delay(500).Wait();
    }
}