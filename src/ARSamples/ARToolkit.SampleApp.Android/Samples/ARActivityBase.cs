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

namespace ARToolkit.SampleApp.Samples
{
    public abstract class ARActivityBase : AppCompatActivity
    {
        private Esri.ArcGISRuntime.ARToolkit.ARSceneView arView;
        private bool renderPlanes;
        private Snackbar mLoadingMessageSnackbar = null;

        protected Esri.ArcGISRuntime.ARToolkit.ARSceneView ARView { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ARView = arView = SetContentView();
            arView.IsTrackingStateChanged += IsTrackingStateChanged;
            arView.GeoViewTapped += ARView_GeoViewTapped;
            arView.GeoViewDoubleTapped += ArView_GeoViewDoubleTapped;
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
                renderPlanes = !renderPlanes;
            }
            return base.OnOptionsItemSelected(item);
        }

        private Renderers.PlaneRenderer pr = null;

        private void ARView_DrawBegin(object sender, Esri.ArcGISRuntime.ARToolkit.DrawEventArgs e)
        {
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
                    planes.Add(plane);
                }
                if (planes.Count > 0)
                {
                    pr.DrawPlanes(planes, camera.DisplayOrientedPose, projmtx);
                }
            }
        }

        private void ArView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            if (arView.SetInitialTransformation(e.Position))
            {
                Toast.MakeText(this, "Placed scene", ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(this, "Couldn't place scene", ToastLength.Short).Show();
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

        private void IsTrackingStateChanged(object sender, bool isTracking)
        {
            if (isTracking)
            {
                arView.IsTrackingStateChanged -= IsTrackingStateChanged;
                hideLoadingMessage();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            // ARCore requires camera permissions to operate. If we did not yet obtain runtime
            // permission on Android M and above, now is a good time to ask the user for it.
            //TODO ArCoreApk.Instance.RequestInstall(this, )
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
            {
                try
                {
                    this.arView.StartTracking();
                }
                catch(System.Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                    Finish();
                }
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.Camera }, 0);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            //if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
            {
                // Note that the order matters - GLSurfaceView is paused first so that it does not try
                // to query the session. If Session is paused before GLSurfaceView, GLSurfaceView may
                // still call mSession.update() and get a SessionPausedException.
                arView.StopTracking();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) != Android.Content.PM.Permission.Granted)
            {
                Toast.MakeText(this, "Camera permission is needed to run this application", ToastLength.Long).Show();
                Finish();
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                Window.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
            }
        }

        private void showLoadingMessage()
        {
            this.RunOnUiThread(() =>
            {
                mLoadingMessageSnackbar = Snackbar.Make(FindViewById(Android.Resource.Id.Content),
                        "Searching for surfaces...", Snackbar.LengthIndefinite);
                mLoadingMessageSnackbar.View.SetBackgroundColor(Android.Graphics.Color.DarkGray);
                mLoadingMessageSnackbar.Show();
            });
        }

        private void hideLoadingMessage()
        {
            if (mLoadingMessageSnackbar == null)
                return;
            this.RunOnUiThread(() =>
            {
                mLoadingMessageSnackbar?.Dismiss();
                mLoadingMessageSnackbar = null;
            });
        }
    }
}