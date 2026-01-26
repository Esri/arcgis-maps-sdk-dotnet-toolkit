using System.Windows;

namespace Toolkit.UITests.Wpf.Puppet;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private string ApiKey = "API_KEY";

    public App()
    {
        ControlPatcher.ApplyPatches();
        Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
        Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = ApiKey;
    }
}
