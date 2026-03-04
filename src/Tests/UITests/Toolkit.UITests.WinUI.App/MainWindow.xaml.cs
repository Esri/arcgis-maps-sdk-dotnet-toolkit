using Esri.ArcGISRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Toolkit.UITests.WinUI.Puppet;

public sealed partial class MainWindow : Window
{
    private Type[] _testTypes;

    public MainWindow()
    {
        Activated += MainWindow_Activated;

        InitializeComponent();

        var testPages = from t in Assembly.GetExecutingAssembly().ExportedTypes
                        where t.GetTypeInfo().IsSubclassOf(typeof(UserControl)) && t.FullName!.Contains(".TestPages.")
                        select t;
        _testTypes = testPages.ToArray();

        var content = this.Content as FrameworkElement;
        content!.RequestedTheme = ElementTheme.Light;
    }

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetDpiForWindow(IntPtr hwnd);

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        // DPI is used in the tests to make size comparisons screen-independent
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        float scale = GetDpiForWindow(hWnd) / 96.0f;
        ScreenDensityTextBlock.Text = scale.ToString();

        // .NET and SDK versions are a convenience for users to know what the tests are running
        NetVersionTextBlock.Text = Environment.Version.ToString();

        var runtimeTypeInfo = typeof(ArcGISRuntimeEnvironment).GetTypeInfo();
        var runtimeVersion = FileVersionInfo.GetVersionInfo(runtimeTypeInfo.Assembly.Location);
        RuntimeVersionTextBlock.Text = runtimeVersion.FileVersion;

        var toolkitTypeInfo = typeof(Esri.ArcGISRuntime.Toolkit.UI.Controls.ScaleLine).GetTypeInfo();
        var toolkitVersion = FileVersionInfo.GetVersionInfo(runtimeTypeInfo.Assembly.Location);
        ToolkitVersionTextBlock.Text = toolkitVersion.FileVersion;
    }

    private void SwitchThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var content = this.Content as FrameworkElement;
        content!.RequestedTheme = content!.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
    }

    private void TestSearchBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !String.IsNullOrEmpty(TestSearchBox.Text))
        {
            var testType = _testTypes.FirstOrDefault(t => t.FullName!.EndsWith(TestSearchBox.Text, StringComparison.OrdinalIgnoreCase));
            if (testType != null)
            {
                TestPagePresenter.Content = Activator.CreateInstance(testType);
            }
        }
    }
}
