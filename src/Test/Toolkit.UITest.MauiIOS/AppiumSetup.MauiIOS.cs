namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var MauiSamplesApp = @"com.companyname.toolkit.sampleapp.maui";
        var deviceUdid = @"YOUR_DEVICE_UDID";

        driver = MakeiOSDriver(deviceUdid, MauiSamplesApp);

        Task.Delay(500).Wait();
    }
}