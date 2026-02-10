using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace Toolkit.UITests.WinUI.Puppet.TestPages;

public sealed partial class CompassMap : UserControl, INotifyPropertyChanged
{
    private double _heading;

    public CompassMap()
    {
        InitializeComponent();

        var map = new Map();
        map.BackgroundColor = System.Drawing.Color.White;
        MainMapView.Map = map;
        MainMapView.Grid = null;
        MainMapView.IsAttributionTextVisible = false;

        HeadingCompass.DataContext = this;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public double Heading
    {
        get => _heading;
        set
        {
            _heading = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Heading)));
        }
    }

    private async void RotateButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var heading = double.Parse(RotateInput.Text);
        Heading = heading;
        await MainMapView.SetViewpointRotationAsync(heading);
    }

    private void ToggleAutoHideButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var hide = !MapCompass.AutoHide;
        MapCompass.AutoHide = hide;
        HeadingCompass.AutoHide = hide;
    }
}