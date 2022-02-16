using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp.Samples
{
    /// <summary>
    /// Base class for some common functionality used by most of the samples
    /// </summary>
    public abstract class ARActivityBase : AppCompatActivity
    {
        private Esri.ArcGISRuntime.ARToolkit.ARSceneView arView;
        private bool renderPlanes;

        protected Esri.ArcGISRuntime.ARToolkit.ARSceneView ARView { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ARView = arView = SetContentView();
            arView.GeoViewTapped += ARView_GeoViewTapped;
            ARView.PlanesDetectedChanged += ARView_PlanesDetectedChanged;
        }

        private void ARView_PlanesDetectedChanged(object sender, bool planesDetected)
        {
            OnPlanesDetected(planesDetected);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Add(Menu.None, 1, Menu.None, "Toggle camera");
            menu.Add(Menu.None, 2, Menu.None, "Toggle render surfaces");
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == 1)
            {
                ARView.RenderVideoFeed = !ARView.RenderVideoFeed;
            }
            if (item.ItemId == 2)
            {
                ToggleRenderPlanes(!renderPlanes);
            }
            return base.OnOptionsItemSelected(item);
        }

        protected void ToggleRenderPlanes(bool turnOn)
        {
            ARView.ArSceneView.PlaneRenderer.Enabled = turnOn;
            ARView.ArSceneView.PlaneRenderer.Visible = turnOn;
            renderPlanes = turnOn;
        }

        private async void ARView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            var loc = await arView.ScreenToLocationAsync(e.Position);
            if (loc != null && loc.SpatialReference != null)
            {
                Toast.MakeText(this, "Location: " + CoordinateFormatter.ToLatitudeLongitude(loc, LatitudeLongitudeFormat.DegreesDecimalMinutes, 4), ToastLength.Short).Show();
            }
        }

        protected virtual Esri.ArcGISRuntime.ARToolkit.ARSceneView SetContentView()
        {
            SetContentView(Resource.Layout.simplearview);
            return FindViewById<Esri.ArcGISRuntime.ARToolkit.ARSceneView>(Resource.Id.sceneView1);
        }

        protected Esri.ArcGISRuntime.ARToolkit.ARLocationTrackingMode TrackingMode { get; set; } = Esri.ArcGISRuntime.ARToolkit.ARLocationTrackingMode.Ignore;
        protected override void OnResume()
        {
            base.OnResume();
            try
            {
                _ = this.arView.StartTrackingAsync(TrackingMode);
                if (ARView.IsUsingARCore)
                {
                    OnPlanesDetected(false);
                }
            }
            catch(Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                Finish();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            arView.StopTrackingAsync();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            }
        }

        protected virtual void OnPlanesDetected(bool detected)
        {
            RunOnUiThread(() =>
            {
                var statusView = FindViewById<TextView>(Resource.Id.trackingStatus);
                if(statusView != null) statusView.Visibility = detected ? ViewStates.Gone : ViewStates.Visible;
            });
        }
    }
}