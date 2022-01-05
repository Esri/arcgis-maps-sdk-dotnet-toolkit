using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ARToolkit.SampleApp.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(DisplayName = "Camera Tracking Disabled", 
        Description = "A sample that doesn't rely on ARCore/ARKit but only features the ability to look around based on the device's motion sensors")]
    public partial class LookAroundSample : ContentPage
	{
		public LookAroundSample()
		{
			InitializeComponent ();
            _ = Init();
		}

        private async Task Init()
        {
            try
            {
                ARView.OriginCamera = new Camera(new MapPoint(-119.622075, 37.720650, 2105), 0, 90, 0); //Yosemite

                Surface sceneSurface = new Surface();
                sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                Scene scene = new Scene(Basemap.CreateImagery())
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
            _ = ARView.StartTrackingAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _ = ARView.StopTrackingAsync();
        }
    }
}