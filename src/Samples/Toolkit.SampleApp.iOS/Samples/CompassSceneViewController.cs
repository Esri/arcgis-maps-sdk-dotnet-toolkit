using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "Compass", DisplayName = "Compass - SceneView", Description = "Compass used with a SceneView")]
    public partial class CompassSceneViewController : UIViewController
    {
        public CompassSceneViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var sceneView = new SceneView()
            {
                Scene = new Scene(Basemap.CreateImagery()),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(sceneView);
            sceneView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            sceneView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            sceneView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            sceneView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;

            var compass = new Compass()
            {
                GeoView = sceneView,
                AutoHide = false,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(compass);
            compass.TrailingAnchor.ConstraintEqualTo(sceneView.TrailingAnchor, -20).Active = true;
            compass.TopAnchor.ConstraintEqualTo(sceneView.TopAnchor, 20).Active = true;
            compass.WidthAnchor.ConstraintEqualTo(50).Active = true;
            compass.HeightAnchor.ConstraintEqualTo(50).Active = true;
        }
    }
}