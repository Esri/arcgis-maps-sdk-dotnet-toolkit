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
	public partial class EarthSample : ContentPage
	{
		public EarthSample ()
		{
			InitializeComponent ();
            Init();
		}

        private async void Init()
        {
            try
            {
                var Scene = new Scene(Basemap.CreateImagery())
                {
                    InitialViewpoint = new Viewpoint(
                        new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84),
                        new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84), 0, 0, 0))
                };
                Scene.BaseSurface = new Surface();
                Scene.BaseSurface.BackgroundGrid.IsVisible = false;
                Scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("http://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                Scene.BaseSurface.ElevationExaggeration = 10;
                Scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                ARView.TranslationFactor = 100000000;
                await Scene.LoadAsync();
                ARView.Scene = Scene;
            }
            catch (System.Exception ex)
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