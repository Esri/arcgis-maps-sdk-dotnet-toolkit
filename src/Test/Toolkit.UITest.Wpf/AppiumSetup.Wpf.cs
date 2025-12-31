using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared;

internal partial class AppiumSetup
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var wpfSamplesApp = @"YOUR_PATH\arcgis-maps-sdk-dotnet-toolkit\src\Samples\Toolkit.SampleApp.WPF\bin\Debug\net8.0-windows10.0.19041.0\Toolkit.SampleApp.WPF.exe";

        appiumLocalService = StartServer();
        driver = MakeWindowsDriver(wpfSamplesApp);

        var maximizeButton = Driver!.FindElement(MobileBy.Name("Maximize"));
        maximizeButton.Click();

        Console.WriteLine("WPF OneTimeSetup complete.");

        Task.Delay(500).Wait();
    }
}