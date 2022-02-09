using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace $safeprojectname$
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ARPage : ContentPage
    {
        public ARPage()
        {
            InitializeComponent();
            //We'll set the origin of the scene centered on Mnt Everest so we can use that as the tie-point when we tap to place
            arSceneView.OriginCamera = new Camera(27.988056, 86.925278, 0, 0, 90, 0);
            arSceneView.TranslationFactor = 10000; //1m device movement == 10km
        }

        private async void InitializeScene()
        {
            try
            {
                var scene = new Scene(Basemap.CreateImagery());
                scene.BaseSurface = new Surface();
                scene.BaseSurface.BackgroundGrid.IsVisible = false;
                scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                await scene.LoadAsync();
                arSceneView.Scene = scene;
            }
            catch (System.Exception ex)
            {
                await DisplayAlert("Failed to load scene", ex.Message, "OK");
                await Navigation.PopAsync();
            }

        }

        private async Task StartTrackingAsync()
        {
            Status.Text = "Move your device in a circular motion to detect surfaces";
            try
            {
                await arSceneView.StartTrackingAsync();
            }
            catch(System.Exception ex)
            {
                await DisplayAlert("Error starting tracking", ex.Message, "Ok");
                _ = Navigation.PopAsync();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = StartTrackingAsync();
        }

        protected override void OnDisappearing()
        {
            arSceneView.StopTrackingAsync();
            base.OnDisappearing();
        }

        private void DoubleTap_ToPlace(object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            if (arSceneView.SetInitialTransformation(e.Position))
            {
                if (arSceneView.Scene == null)
                {
                    arSceneView.RenderPlanes = false;
                    Status.Text = string.Empty;
                    InitializeScene();
                }
            }
        }

        private void PlanesDetectedChanged(object sender, bool planesDetected)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (!planesDetected)
                    Status.Text = "Move your device in a circular motion to detect surfaces";
                else if(arSceneView.Scene == null)
                    Status.Text = "Double-tap a plane to place your scene";
                else
                    Status.Text = string.Empty;
            });
        }
    }
}
