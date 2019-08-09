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

    [SampleInfo(DisplayName = "Barton School House", Description = "A local Scene Layer Package (mesh)")]
    [SampleData(ItemId = "b30f53d65c714054b75a0eb16639529a", Path = "BartonSchoolHouse_3d_mesh.slpk")]
    public partial class BartonViewController : UIViewController
    {
        ARSceneView _sceneView;
        UIView bg;
        UILabel lbl;
        
        public BartonViewController() : base()
        {
        }
        public BartonViewController(IntPtr handle) : base(handle)
        {
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = UIColor.Red;
            bg = new UIView() { BackgroundColor = UIColor.Red };
            
            View.AddSubview(bg);

            // Create a new AR Scene View, set its scene, and provide the coordinates for laying it out
            _sceneView = new ARSceneView();
            // Add the ARSceneView to the Subview
            View.AddSubview(_sceneView);

            lbl = new UILabel();
            lbl.Text = "Initializing...";
            View.AddSubview(lbl);

            _sceneView.Scene = await ARTestScenes.CreateBartonSchoolHouse(_sceneView);
            _sceneView.GeoViewDoubleTapped += SceneView_GeoViewDoubleTapped;
        }

        private void SceneView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            if (_sceneView.SetInitialTransformation(e.Position))
            {
                lbl.Text = "Placed scene";
            }
            else
            {
                lbl.Text = "Couldn't place scene";
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            _sceneView.StartTrackingAsync();
        }

        public override void ViewDidLayoutSubviews()
        {
            bg.Frame = new CoreGraphics.CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);
            // Fill the screen with the map
            _sceneView.Frame = new CoreGraphics.CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);
            lbl.Frame = new CoreGraphics.CGRect(0, 20, View.Bounds.Width, 20);
            
            base.ViewDidLayoutSubviews();
        }
    }
}