using Foundation;
using System;
using UIKit;
using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;

namespace $safeprojectname$
{
    public partial class ViewController : UIViewController
    {
        private ARSceneView arSceneView;
        private UILabel trackingStatus;

        public ViewController(IntPtr handle) : base(handle)
        {
        }
        
        public ViewController()
        {
        }
        
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Create a new AR Scene View, set its scene, and provide the coordinates for laying it out
            arSceneView = new ARSceneView() { TranslatesAutoresizingMaskIntoConstraints = false };
            // Add the ARSceneView to the Subview
            View.AddSubview(arSceneView);
            arSceneView.TopAnchor.ConstraintEqualTo(this.View.TopAnchor, 0).Active = true;
            arSceneView.LeftAnchor.ConstraintEqualTo(this.View.LeftAnchor, 0).Active = true;
            arSceneView.BottomAnchor.ConstraintEqualTo(this.View.BottomAnchor, 0).Active = true;
            arSceneView.RightAnchor.ConstraintEqualTo(this.View.RightAnchor, 0).Active = true;
            trackingStatus = new UILabel() { TranslatesAutoresizingMaskIntoConstraints = false };
            trackingStatus.Text = "Move the device in a circular motion to detect surfaces...";
            View.AddSubview(trackingStatus);
            trackingStatus.TopAnchor.ConstraintEqualTo(this.View.SafeAreaLayoutGuide.TopAnchor, 0).Active = true;
            trackingStatus.LeftAnchor.ConstraintEqualTo(this.View.LeftAnchor, 0).Active = true;

            arSceneView.OriginCamera = new Camera(27.988056, 86.925278, 0, 0, 90, 0);
            arSceneView.TranslationFactor = 10000; //1m device movement == 10km
            arSceneView.PlanesDetectedChanged += ArSceneView_PlanesDetectedChanged;
            arSceneView.GeoViewDoubleTapped += ArSceneView_GeoViewDoubleTapped;
            arSceneView.RenderPlanes = true;
            _ = arSceneView.StartTrackingAsync();
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
                UIAlertView _error = new UIAlertView("Failed to load scene", ex.Message, null as IUIAlertViewDelegate, "Ok");
                _error.Show();
            }
        }

        private void ArSceneView_GeoViewDoubleTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (arSceneView.SetInitialTransformation(e.Position))
            {
                if (arSceneView.Scene == null)
                {
                    arSceneView.RenderPlanes = false;
                    trackingStatus.Text = string.Empty;
                    InitializeScene();
                }
            }
        }

        private void ArSceneView_PlanesDetectedChanged(object sender, bool planesDetected)
        {
            CoreFoundation.DispatchQueue.MainQueue.DispatchSync(() =>
            {
                if (!planesDetected)
                    trackingStatus.Text = "Move your device in a circular motion to detect surfaces";
                else if (arSceneView.Scene == null)
                    trackingStatus.Text = "Double-tap a plane to place your scene";
                else
                    trackingStatus.Text = string.Empty;
            });
        }

        public override void ViewDidDisappear(bool animated)
        {
            arSceneView.StopTrackingAsync();
            base.ViewDidDisappear(animated);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}