using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using AndroidX.AppCompat.App;
using Esri.ArcGISRuntime.Mapping;

namespace $safeprojectname$
{
    [Activity(Label = "$safeprojectname$", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait, Theme = "@style/Theme.AppCompat")]
    public class MainActivity : AppCompatActivity
    {
        private Esri.ArcGISRuntime.ARToolkit.ARSceneView arSceneView;
        private TextView trackingStatus;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
            SetContentView(Resource.Layout.main);
            trackingStatus = FindViewById<TextView>(Resource.Id.trackingStatus);
            arSceneView = FindViewById<Esri.ArcGISRuntime.ARToolkit.ARSceneView>(Resource.Id.sceneView1);
            arSceneView.OriginCamera = new Camera(27.988056, 86.925278, 0, 0, 90, 0);
            arSceneView.TranslationFactor = 10000; //1m device movement == 10km
            arSceneView.PlanesDetectedChanged += ArSceneView_PlanesDetectedChanged;
            arSceneView.GeoViewDoubleTapped += ArSceneView_GeoViewDoubleTapped;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async void InitializeScene()
        {
            try
            {
                var scene = new Scene(Basemap.CreateImagery());
                scene.BaseSurface = new Esri.ArcGISRuntime.Mapping.Surface();
                scene.BaseSurface.BackgroundGrid.IsVisible = false;
                scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                await scene.LoadAsync();
                arSceneView.Scene = scene;
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene" + ex.Message, ToastLength.Long).Show();
                Finish();
            }
        }

        private void ArSceneView_GeoViewDoubleTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (arSceneView.SetInitialTransformation(e.Position))
            {
                if (arSceneView.Scene == null)
                {
                    arSceneView.ArSceneView.PlaneRenderer.Enabled = false;
                    trackingStatus.Text = string.Empty;
                    InitializeScene();
                }
            }
        }

        private void ArSceneView_PlanesDetectedChanged(object sender, bool planesDetected)
        {
            RunOnUiThread(() =>
            {
                if (!planesDetected)
                    trackingStatus.Text = "Move your device in a circular motion to detect surfaces";
                else if (arSceneView.Scene == null)
                    trackingStatus.Text = "Double-tap a plane to place your scene";
                else
                    trackingStatus.Text = string.Empty;
            });
        }
        
        protected async override void OnResume()
        {
            base.OnResume();
            try
            {
                trackingStatus.Text = "Move your device in a circular motion to detect surfaces";
                arSceneView.ArSceneView.PlaneRenderer.Enabled = true;
                await arSceneView.StartTrackingAsync();
            }
            catch(System.Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                Finish();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            arSceneView.StopTrackingAsync();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                Window.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
            }
        }
    }
}

