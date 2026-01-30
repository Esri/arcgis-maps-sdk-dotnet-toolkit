using Esri.ArcGISRuntime;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Toolkit.UITests.Wpf.Puppet;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Type[] _testTypes;

    public MainWindow()
    {
        Loaded += MainWindow_Loaded;

        InitializeComponent();

        var testPages = from t in Assembly.GetExecutingAssembly().ExportedTypes
                        where t.GetTypeInfo().IsSubclassOf(typeof(UserControl)) && t.FullName!.Contains(".TestPages.")
                        select t;
        _testTypes = testPages.ToArray();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        ScreenDensityTextBlock.Text = dpi.DpiScaleX.ToString();

        NetVersionTextBlock.Text = Environment.Version.ToString();
        
        var runtimeTypeInfo = typeof(ArcGISRuntimeEnvironment).GetTypeInfo();
        var runtimeVersion = FileVersionInfo.GetVersionInfo(runtimeTypeInfo.Assembly.Location);
        RuntimeVersionTextBlock.Text = runtimeVersion.FileVersion;

        var toolkitTypeInfo = typeof(Esri.ArcGISRuntime.Toolkit.UI.Controls.ScaleLine).GetTypeInfo();
        var toolkitVersion = FileVersionInfo.GetVersionInfo(runtimeTypeInfo.Assembly.Location);
        ToolkitVersionTextBlock.Text = toolkitVersion.FileVersion;
    }

    private void TestSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && !String.IsNullOrEmpty(TestSearchBox.Text))
        {
            var testType = _testTypes.FirstOrDefault(t => t.FullName!.EndsWith(TestSearchBox.Text, StringComparison.OrdinalIgnoreCase));
            if (testType != null)
            {
                TestContentPresenter.Content = Activator.CreateInstance(testType);
            }
        }
    }
}