using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Threading.Tasks;
using UIKit;

namespace ARToolkit.SampleApp.Samples
{

    [SampleInfo(DisplayName = "Tap-to-place 3D Model",
        Description = "This demonstrates the table-top experience, where you can double-tap a surface to place the scene on that surface")]
    [SampleData(ItemId = "7dd2f97bb007466ea939160d0de96a9d", Path = "philadelphia.mspk")]
    public partial class TapToPlaceViewController : UIViewController
    {
        ARSceneView? ARView;
        UILabel? lbl;
        UISwitch? _planeSwitch;

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (View == null)
            {
                throw new InvalidOperationException("View was unexpectedly null");
            }

            ARView = new ARSceneView { TranslatesAutoresizingMaskIntoConstraints = false };

            lbl = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false };
            lbl.Text = "Move the device in a circular motion to detect surfaces...";

            _planeSwitch = new UISwitch { TranslatesAutoresizingMaskIntoConstraints = false, On = true };

            View.AddSubviews(ARView, lbl, _planeSwitch);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                ARView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                ARView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                ARView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                ARView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                lbl.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 8),
                lbl.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, 8),
                _planeSwitch.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _planeSwitch.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor)
            });

            try
            {
                var p = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
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
                ARView.OriginCamera = new Camera(39.9579126, -75.1705827, 9.64, 0, 90, 0);
                ARView.TranslationFactor = 1000; // By increasing the translation factor, the scene appears as if it's at scale 1:1000
                                                 //Set the initial location 1.5 meter in front of and .5m above the scene
                ARView.SetInitialTransformation(TransformationMatrix.Create(0, 0, 0, 1, 0, .5, 1.5));
                //Set the clipping distance to only render a circular area around the origin
                ARView.ClippingDistance = 350;
                //Listend for double-tap to place

                ARView.NorthAlign = false;
                ARView.RenderPlanes = true;
            }
            catch (Exception ex)
            {
                lbl.Text = $"Error: {ex.Message}";
            }
        }

        private void ARView_PlanesDetectedChanged(object? sender, bool planesDetected)
        {
            CoreFoundation.DispatchQueue.MainQueue.DispatchSync(() =>
            {
                if (lbl != null)
                    lbl.Text = planesDetected ? "" : "Move the device in a circular motion to detect surfaces...";
            });
        }

        private void Sw_ValueChanged(object? sender, EventArgs e)
        {
            if (sender is UISwitch sendingSwitch && ARView != null)
            {
                ARView.RenderPlanes = sendingSwitch.On;
            }
        }

        private void ArView_GeoViewDoubleTapped(object? sender, GeoViewInputEventArgs e)
        {
            if (ARView == null || lbl == null)
            {
                return;
            }

            if (ARView.SetInitialTransformation(e.Position))
            {
                lbl.Text = $"Placed scene {ARView.InitialTransformation.TranslationX.ToString("0.000")},{ARView.InitialTransformation.TranslationY.ToString("0.000")},{ARView.InitialTransformation.TranslationZ.ToString("0.000")} ";
                System.Diagnostics.Debug.WriteLine(lbl.Text);
            }
            else
            {
                lbl.Text = "Couldn't place scene";
            }
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (_planeSwitch != null)
            {
                _planeSwitch.ValueChanged += Sw_ValueChanged;
            }
            if (ARView != null)
            {
                ARView.PlanesDetectedChanged += ARView_PlanesDetectedChanged;
                ARView.GeoViewDoubleTapped += ArView_GeoViewDoubleTapped;
                try
                {
                    await ARView.StartTrackingAsync();
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            if (ARView != null)
            {
                ARView.PlanesDetectedChanged -= ARView_PlanesDetectedChanged;
                ARView.GeoViewDoubleTapped -= ArView_GeoViewDoubleTapped;
                if (_planeSwitch != null)
                {
                    _planeSwitch.ValueChanged -= Sw_ValueChanged;
                }

                try
                {
                    await ARView.StopTrackingAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }
}