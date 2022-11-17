using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp.Maui.Samples;

[XamlCompilation(XamlCompilationOptions.Compile)]
[SampleInfo(DisplayName = "Camera Tracking Disabled",
    Description = "A sample that doesn't rely on ARCore/ARKit but only features the ability to look around based on the device's motion sensors")]
public partial class LookAroundSample : ContentPage
{
    public LookAroundSample()
    {
        InitializeComponent();
        Init();
    }

    private async void Init()
    {
        try
        {
            var layer = new ArcGISTiledLayer(new Uri("https://www.arcgis.com/home/item.html?id=10df2279f9684e4a9f6a7f08febac2a9"));
            await layer.LoadAsync();
            var basemap = new Basemap(layer);
            var scene = new Scene(basemap);
            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            ARView.Scene = scene;
            await scene.LoadAsync();
            ARView.OriginCamera = new Camera(new MapPoint(-119.622075, 37.720650, 2105), 0, 90, 0); //Yosemite
        }
        catch (Exception ex)
        {
            await DisplayAlert("Failed to load scene", ex.Message, "OK");
            await Navigation.PopAsync();
        }
    }

    private async Task LoadWhenReady()
    {
        bool hasLoaded = false;
        do
        {
            try
            {
                await ARView.StartTrackingAsync(Esri.ArcGISRuntime.ARToolkit.ARLocationTrackingMode.Ignore);
                hasLoaded = true;
            }
            catch (Exception)
            {
                await Task.Delay(300);
            }
        } while (!hasLoaded);

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadWhenReady();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ARView.StopTrackingAsync();
    }
}