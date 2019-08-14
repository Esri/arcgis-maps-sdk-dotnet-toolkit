using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ARToolkit.SampleApp.Forms.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(DisplayName = "Camera Tracking Disabled", Description = "A sample that doesn't rely on ARCore/ARKit but only features the ability to look around based on the device's motion sensors")]
    public partial class LookAroundSample : ContentPage
	{
		public LookAroundSample()
		{
			InitializeComponent ();
            Init();
		}

        private async void Init()
        {
            try
            {
                ARView.UseCameraTracking = false; // Disable ARCore/ARKit SLAM based tracking. We'll just rely on the motion sensors in the device instead
                ARView.UseCompass = true; // Optional: Use the compass to match north with real-world north. If false, the app will launch looking towards the heading defined in the OriginCamera
                ARView.RenderVideoFeed = false;
                ARView.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(-119.622075, 37.720650, 2105), 0, 90, 0); //Yosemite

                Surface sceneSurface = new Surface();
                sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                Scene scene = new Scene(Basemap.CreateImagery())
                {
                    BaseSurface = sceneSurface
                };
                ARView.Scene = scene;
            }
            catch (System.Exception)
            {
                //Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
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
            ARView.StopTracking();
        }
    }
}