using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace ARToolkit.SampleApp.Maui.Samples;

[XamlCompilation(XamlCompilationOptions.Compile)]
[SampleInfo(DisplayName = "Earth", Description = "Shows the entire earth hovering in front of you, allowing you to walk around it")]
public partial class EarthSample : ContentPage
{
    public EarthSample()
    {
        InitializeComponent();
        Init();
    }

    private async void Init()
    {
        try
        {
            var scene = new Scene(Basemap.CreateImagery());
            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.ElevationExaggeration = 50;
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            ARView.TranslationFactor = 50000000;
            // Set pitch to 0 so looking forward looks "down" on earth from space
            ARView.OriginCamera = new Camera(new MapPoint(0, 0, 10000000, SpatialReferences.Wgs84), 0, 0, 0);

            await scene.LoadAsync();
            ARView.Scene = scene;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Failed to load scene", ex.Message, "OK");
            await Navigation.PopAsync();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ARView.StartTrackingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ARView.StopTrackingAsync();
    }
}