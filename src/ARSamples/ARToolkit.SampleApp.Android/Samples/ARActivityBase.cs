using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Mapping;
using Android.Opengl;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using System;
using Javax.Microedition.Khronos.Egl;
using Javax.Microedition.Khronos.Opengles;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Android.Support.Design.Widget;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Geometry;
using System.Threading.Tasks;
using Android.Views;
using System.Linq;

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
            ARView.DrawBegin += ARView_DrawBegin;
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
            renderPlanes = turnOn;
        }

        private Renderers.PlaneRenderer pr = null;

        private void ARView_DrawBegin(object sender, Esri.ArcGISRuntime.ARToolkit.DrawEventArgs e)
        {
            if (!ARView.UseARCore) return;
            bool planesDetected = false;
            if (renderPlanes)
            {
                // Use a custom renderer to render the planes
                // prior to rendering the scene
                if (pr == null)
                {
                    pr = new Renderers.PlaneRenderer();
                    pr.CreateOnGlThread(/*context=*/this, "trigrid.png");
                }
                var camera = e.Frame.Camera;
                float[] projmtx = new float[16];
                camera.GetProjectionMatrix(projmtx, 0, 0.1f, 100.0f);
                var planes = new List<Plane>();
                foreach (var p in e.Session.GetAllTrackables(Java.Lang.Class.FromType(typeof(Plane))))
                {
                    var plane = (Plane)p;
                    if(plane.TrackingState == TrackingState.Tracking)
                        planes.Add(plane);
                }
                if (planes.Count > 0)
                {
                    pr.DrawPlanes(planes, camera.DisplayOrientedPose, projmtx);
                    planesDetected = true;
                }
            }
            else if(!isSurfaceDetectionComplete)
            {
                planesDetected = e.Session.GetAllTrackables(Java.Lang.Class.FromType(typeof(Plane))).OfType<Plane>().Where(p => p.TrackingState == TrackingState.Tracking).Any();
            }
            if (!isSurfaceDetectionComplete && planesDetected)
            {
                OnPlanesDetected();
                isSurfaceDetectionComplete = true;
            }
        }

        private async void ARView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            var loc = await arView.ScreenToLocationAsync(e.Position);
            if (loc != null && loc.SpatialReference != null)
            {
                Toast.MakeText(this, "Location: " + Esri.ArcGISRuntime.Geometry.CoordinateFormatter.ToLatitudeLongitude(loc, LatitudeLongitudeFormat.DegreesDecimalMinutes, 4), ToastLength.Short).Show();
            }
        }

        protected virtual Esri.ArcGISRuntime.ARToolkit.ARSceneView SetContentView()
        {
            SetContentView(Resource.Layout.simplearview);
            return FindViewById<Esri.ArcGISRuntime.ARToolkit.ARSceneView>(Resource.Id.sceneView1);
        }
        bool isSurfaceDetectionComplete;
        protected override void OnResume()
        {
            base.OnResume();
            try
            {
                isSurfaceDetectionComplete = false;
                this.arView.StartTrackingAsync();
                if(ARView.UseARCore)
                {
                    ShowLookingForSurfaces();
                }
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
            arView.StopTracking();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                Window.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
            }
        }

        private void ShowLookingForSurfaces()
        {
            this.RunOnUiThread(() =>
            {
                var statusView = FindViewById<TextView>(Resource.Id.trackingStatus);
                if (statusView != null) statusView.Visibility = ViewStates.Visible;
            });
        }

        protected virtual void OnPlanesDetected()
        {
            this.RunOnUiThread(() =>
            {
                var statusView = FindViewById<TextView>(Resource.Id.trackingStatus);
                if(statusView != null) statusView.Visibility = ViewStates.Gone;
            });
        }
    }
}