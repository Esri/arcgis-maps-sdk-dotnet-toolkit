#if MAUI_APP
using ClickEventArgs = System.EventArgs;
#elif WINUI_APP
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
#elif WPF_APP
using ClickEventArgs = System.Windows.RoutedEventArgs;
#endif

namespace Toolkit.UITests.App.TestPages;

public partial class CompassMap : TestPage
{
    private double _heading;

    public CompassMap()
    {
        InitializeComponent();

        var map = new Esri.ArcGISRuntime.Mapping.Map();
        map.BackgroundColor = System.Drawing.Color.White;
        MainMapView.Map = map;
        MainMapView.Grid = null;
        MainMapView.IsAttributionTextVisible = false;
    }

    public double Heading
    {
        get => _heading;
        set
        {
            _heading = value;
            OnPropertyChanged(nameof(Heading));
        }
    }

    private async void RotateButton_Click(object sender, ClickEventArgs e)
    {
        var heading = double.Parse(RotateInput.Text);
        Heading = heading;
        await MainMapView.SetViewpointRotationAsync(heading);
    }

    private void ToggleAutoHideButton_Click(object sender, ClickEventArgs e)
    {
        var hide = !MapCompass.AutoHide;
        MapCompass.AutoHide = hide;
        HeadingCompass.AutoHide = hide;
    }
}