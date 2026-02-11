#if MAUI_APP
using ControlType = Microsoft.Maui.Controls.ContentView;
#elif WINUI_APP
using System.ComponentModel;
using ControlType = Microsoft.UI.Xaml.Controls.UserControl;
#elif WPF_APP
using System.ComponentModel;
using ControlType = System.Windows.Controls.UserControl;
#endif

namespace Toolkit.UITests.App.TestPages;

#if MAUI_APP
public class TestPage : ControlType
{ }
#else
public class TestPage : ControlType, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
#endif