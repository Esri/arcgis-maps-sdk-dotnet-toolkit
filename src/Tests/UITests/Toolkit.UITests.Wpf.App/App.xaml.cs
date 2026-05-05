using System.Windows;

namespace Toolkit.UITests.Wpf.Puppet;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        ControlPatcher.ApplyPatches();
        Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
    }
}
