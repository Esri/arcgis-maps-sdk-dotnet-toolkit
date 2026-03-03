using Esri.ArcGISRuntime;
using System.Diagnostics;
using System.Reflection;

namespace Toolkit.UITests.Maui.App;

public partial class MainPage : ContentPage
{
    private Type[] _testPageTypes;
    private Command TestSearchBoxReturnCommand;

    public MainPage()
    {
        Loaded += MainWindow_Loaded;

        InitializeComponent();

        Application.Current!.UserAppTheme = AppTheme.Light;

#pragma warning disable IL2026 // Suppresses a reflection warning
        var testPages = from t in Assembly.GetExecutingAssembly().ExportedTypes
                        where t.GetTypeInfo().IsSubclassOf(typeof(ContentView)) && t.FullName!.Contains(".TestPages.")
                        select t;
#pragma warning restore IL2026
        _testPageTypes = testPages.ToArray();

        TestSearchBoxReturnCommand = new Command(() =>
        {
            var testType = _testPageTypes.FirstOrDefault<Type>(t => t.FullName!.EndsWith(TestSearchBox.Text, StringComparison.OrdinalIgnoreCase));
            if (testType != null)
            {
#pragma warning disable IL2072 // Suppresses a reflection trim warning
                TestContentPresenter.Content = (ContentView)Activator.CreateInstance(testType)!;
#pragma warning disable IL2072
            }
        });
        TestSearchBox.ReturnCommand = TestSearchBoxReturnCommand;
    }

    private void MainWindow_Loaded(object? sender, EventArgs e)
    {
        ScreenDensityLabel.Text = DeviceDisplay.MainDisplayInfo.Density.ToString();

        NetVersionLabel.Text = Environment.Version.ToString();

#if DEBUG || !ANDROID // The trimming on android causes crashes here. This might be fixed when we fully update the android toolkit and this app to .NET 10
#pragma warning disable IL3000 // Suppresses a reflection trim warning
        var runtimeTypeInfo = typeof(ArcGISRuntimeEnvironment).GetTypeInfo();
        var runtimeVersion = FileVersionInfo.GetVersionInfo(runtimeTypeInfo.Assembly.Location);
        RuntimeVersionLabel.Text = runtimeVersion.FileVersion;

        var toolkitTypeInfo = typeof(Esri.ArcGISRuntime.Toolkit.Maui.ScaleLine).GetTypeInfo();
        var toolkitVersion = FileVersionInfo.GetVersionInfo(toolkitTypeInfo.Assembly.Location);
        ToolkitVersionLabel.Text = toolkitVersion.FileVersion;
#pragma warning restore IL3000
#else
        RuntimeVersionLabel.Text = "Not available in Android Release mode";
        ToolkitVersionLabel.Text = "Not available in Android Release mode";
#endif
    }

    private void SwitchThemeButton_Clicked(object sender, EventArgs e)
    {
        Application.Current!.UserAppTheme = Application.Current.RequestedTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
    }
}
