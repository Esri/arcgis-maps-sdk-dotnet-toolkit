using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using UIKit;
using Esri.ArcGISRuntime.ARToolkit;
using ARKit;

namespace ARToolkit.SampleApp.Samples
{

    [SampleInfo(DisplayName = "Tap-to-place 3D Model",
        Description = "This demonstrates the table-top experience, where you can double-tap a surface to place the scene on that surface")]
    [SampleData(ItemId = "7dd2f97bb007466ea939160d0de96a9d", Path = "philadelphia.mspk")]
    public partial class TapToPlaceViewController : UIViewController
    {
        ARSceneView ARView;
        UILabel lbl;
        
        public TapToPlaceViewController() : base()
        {
        }
        public TapToPlaceViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidDisappear(bool animated)
        {
            ARView.StopTrackingAsync();
            base.ViewDidDisappear(animated);
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Create a new AR Scene View, set its scene, and provide the coordinates for laying it out
            ARView = new ARSceneView();
            // Add the ARSceneView to the Subview
            View.AddSubview(ARView);

            lbl = new UILabel() { TranslatesAutoresizingMaskIntoConstraints = false };
            lbl.Text = "Move the device in a circular motion to detect surfaces...";
            View.AddSubview(lbl);
            lbl.TopAnchor.ConstraintEqualTo(this.View.TopAnchor, 0).Active = true;
            lbl.LeftAnchor.ConstraintEqualTo(this.View.LeftAnchor, 0).Active = true;

            var p = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var path = System.IO.Path.Combine(p, "philadelphia.mspk");
            MobileScenePackage package = await MobileScenePackage.OpenAsync(path);
            // Load the package.
            await package.LoadAsync();
            // Show the first scene.
            var scene = package.Scenes[0];
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            ARView.Scene = scene;
            //We'll set the origin of the scene in the middle so we can use that as the tie-point
            ARView.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(39.9579126, -75.1705827, 9.64, 0, 90, 0);
            ARView.TranslationFactor = 1000; // By increasing the translation factor, the scene appears as if it's at scale 1:1000
                                             //Set the initial location 1.5 meter in front of and .5m above the scene
            ARView.SetInitialTransformation(TransformationMatrix.Create(0, 0, 0, 1, 0, .5, 1.5));
            //Set the clipping distance to only render a circular area around the origin
            ARView.ClippingDistance = 350;
            //Listend for double-tap to place
            ARView.GeoViewDoubleTapped += ArView_GeoViewDoubleTapped;
            ARView.NorthAlign = false;
            ARView.RenderPlanes = true;

            UISwitch sw = new UISwitch() { TranslatesAutoresizingMaskIntoConstraints = false };
            sw.ValueChanged += Sw_ValueChanged;
            View.AddSubview(sw);
            sw.TopAnchor.ConstraintEqualTo(this.View.TopAnchor, 0).Active = true;
            sw.RightAnchor.ConstraintEqualTo(this.View.RightAnchor, 0).Active = true;
            sw.WidthAnchor.ConstraintEqualTo(100);
            sw.HeightAnchor.ConstraintEqualTo(30);

            ARView.PlanesDetectedChanged += ARView_PlanesDetectedChanged;
            _ = ARView.StartTrackingAsync();
        }

        private void ARView_PlanesDetectedChanged(object sender, bool planesDetected)
        {
            CoreFoundation.DispatchQueue.MainQueue.DispatchSync(() =>
            {
                lbl.Text = planesDetected ? "" : "Move the device in a circular motion to detect surfaces...";
            });
        }

        private void Sw_ValueChanged(object sender, EventArgs e)
        {
            var isOn = ((UISwitch)sender).On;
            ARView.RenderPlanes = isOn;
        }

        private void ArView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            if (ARView.SetInitialTransformation(e.Position))
            {
                //lbl.Text = "Placed scene";
                lbl.Text = $"Placed scene {ARView.InitialTransformation.TranslationX.ToString("0.000")},{ARView.InitialTransformation.TranslationY.ToString("0.000")},{ARView.InitialTransformation.TranslationZ.ToString("0.000")} ";
                System.Diagnostics.Debug.WriteLine(lbl.Text);
            }
            else
            {
                lbl.Text = "Couldn't place scene";
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
        }

        public override void ViewDidLayoutSubviews()
        {
            // Fill the screen with the map
            ARView.Frame = new CoreGraphics.CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);
            lbl.Frame = new CoreGraphics.CGRect(0, 40, View.Bounds.Width, 60);
            base.ViewDidLayoutSubviews();
        }
    }
}