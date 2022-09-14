using Esri.ArcGISRuntime.Geometry;
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
            ARView.OriginCamera = new Camera(new MapPoint(-119.622075, 37.720650, 2105), 0, 90, 0); //Yosemite

            Surface sceneSurface = new Surface();
            sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            Scene scene = new Scene(new Basemap(new Uri("https://www.arcgis.com/home/item.html?id=52bdc7ab7fb044d98add148764eaa30a")))
            {
                BaseSurface = sceneSurface
            };
            ARView.Scene = scene;
            await scene.LoadAsync();
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