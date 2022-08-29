using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp.Samples
{
    [Activity(
        Label = "VPS",
        Theme = "@style/Theme.AppCompat",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    [SampleInfo(DisplayName = "Continuous VPS AR", Description = "Uses the device's GPS to continously snap the origin to your current location. Best results are achieved with a very high-accuracy GPS, and a good compass alignment.")]
    public class GeoSpatialSample : ARActivityBase
    {
        private Scene Scene;
        private double defaultDeviceElevationAboveTerrain = 1.5;
        TextView headingReadout;
        CheckBox vpsEnabledCheckbox;
        VPSLocationDataSource _lds;

        protected override ARSceneView SetContentView()
        {
            SetContentView(Resource.Layout.fullscalear);            
            return FindViewById<ARSceneView>(Resource.Id.sceneView1);
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            vpsEnabledCheckbox = FindViewById<CheckBox>(Resource.Id.geospatialCheckbox);

            //Configure ARView for 1:1 AR
            ARView.TranslationFactor = 1; // 1:1 AR Scale
            //ARView.UseCompass = true; // Attempt to align with north
            ARView.ViewpointChanged += ARView_ViewpointChanged; // Used for reporting the current heading on the UI
            _lds = new VPSLocationDataSource(this, ARView);
            ARView.LocationDataSource = _lds;
            TrackingMode = ARLocationTrackingMode.ContinuousWithVPS;
            (ARView.LocationDataSource as VPSLocationDataSource).PropertyChanged += GeoSpatialSample_PropertyChanged;

            headingReadout = FindViewById<TextView>(Resource.Id.headingText);

            //Configure scene
            Scene = new Scene(Basemap.CreateStreets());
            Scene.Basemap.BaseLayers[0].Opacity = .75;
            Scene.BaseSurface = new Esri.ArcGISRuntime.Mapping.Surface();
            Scene.BaseSurface.BackgroundGrid.IsVisible = false;
            Scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            Scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));

            try
            {
                await Scene.LoadAsync();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
                return;
            }
            ARView.Scene = Scene;
            ARView.NorthAlign = false;
            vpsEnabledCheckbox.Checked = ARView.SupportsVPS.HasValue ? ARView.SupportsVPS.Value : false;
        }

        private void GeoSpatialSample_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            headingReadout.Text = $"E: {_lds.EarthAltitude}, A:{_lds.AppliedAltitude}, O: {_lds.Separation}";
        }

        private void ARView_ViewpointChanged(object sender, EventArgs e)
        {
            //headingReadout.Text = $"Heading: {ARView.Camera.Heading.ToString("0")}°";
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            vpsEnabledCheckbox.Checked = ARView.SupportsVPS.HasValue ? ARView.SupportsVPS.Value : false;
            menu.Add(Menu.None, 1, Menu.None, "Toggle Calibration Settings");
            menu.Add(Menu.None, 2, Menu.None, "Toggle Basemap");

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == 1)
            {
                var settingsView = FindViewById<LinearLayout>(Resource.Id.settingsView);
                settingsView.Visibility = (settingsView.Visibility == ViewStates.Visible) ? ViewStates.Invisible : ViewStates.Visible;                
            }
            if (item.ItemId == 2)
            {
                Scene.Basemap.BaseLayers[0].IsVisible = !Scene.Basemap.BaseLayers[0].IsVisible;
            }
            return true;
        }

        [Java.Interop.Export("btnSurfaceClick")]
        public void btnSurfaceClick(View v) => SnapToSurface(ARView.Camera?.Location);
        [Java.Interop.Export("btnUpClick")]
        public void btnUpClick(View v) => AdjustElevation(1);
        [Java.Interop.Export("btnDownClick")]
        public void btnDownClick(View v) => AdjustElevation(-1);

        private async void SnapToSurface(MapPoint location)
        {
            if (location == null) return;
            if (Scene?.Basemap?.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                try
                {
                    double deviceElevationAboveTerrain = defaultDeviceElevationAboveTerrain;
                    // Perform hittest at center of screen against detected surfaces to estimate device elevation above terrain
                    var mp = ARView.ARScreenToLocation(new Android.Graphics.PointF(ARView.Width * .5f, ARView.Height * .5f));
                    if (mp != null)
                        deviceElevationAboveTerrain = ARView.Camera.Location.Z - mp.Z;

                    double elevation = await Scene.BaseSurface.GetElevationAsync(location);
                    location = new MapPoint(location.X, location.Y, elevation + deviceElevationAboveTerrain, location.SpatialReference);
                    ARView.OriginCamera = ARView.OriginCamera.MoveTo(location);
                    ARView.ResetTracking();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "Failed to snap location to terrain\n" + ex.Message, ToastLength.Short).Show();
                }
            }
        }

        private void AdjustElevation(double deltaElevation)
        {
            ARView.SetInitialTransformation(ARView.InitialTransformation + TransformationMatrix.Create(0, 0, 0, 1, 0, -deltaElevation, 0));
        }
    }
}