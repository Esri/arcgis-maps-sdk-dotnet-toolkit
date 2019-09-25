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
    [SampleInfo(DisplayName = "Earth", Description = "Shows the entire earth hovering in front of you, allowing you to walk around it")]
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
                var scene = new Scene(Basemap.CreateImagery());
                scene.BaseSurface = new Surface();
                scene.BaseSurface.BackgroundGrid.IsVisible = false;
                scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                scene.BaseSurface.ElevationExaggeration = 10;
                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                ARView.TranslationFactor = 100000000;
                // Set pitch to 0 so looking forward looks "down" on earth from space
                ARView.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84), 0, 0, 0);

                await scene.LoadAsync();
                ARView.Scene = scene;
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