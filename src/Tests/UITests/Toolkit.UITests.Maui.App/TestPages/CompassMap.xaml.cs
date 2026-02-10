namespace Toolkit.UITests.Maui.App.TestPages;

public partial class CompassMap : ContentView
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

        HeadingCompass.BindingContext = this;
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

    private async void RotateButton_Clicked(object sender, EventArgs e)
    {
        var heading = double.Parse(RotateInput.Text);
        Heading = heading;
        await MainMapView.SetViewpointRotationAsync(heading);
    }

    private void ToggleAutoHideButton_Clicked(object sender, EventArgs e)
    {
        var hide = !MapCompass.AutoHide;
        MapCompass.AutoHide = hide;
        HeadingCompass.AutoHide = hide;
    }
}