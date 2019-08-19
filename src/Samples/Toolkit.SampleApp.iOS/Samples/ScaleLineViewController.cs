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
    [SampleInfoAttribute(Category = "ScaleLine")]
    public partial class ScaleLineViewController : UIViewController
    {
        private ScaleLine scaleLine;
        private MapView mapView;

        public ScaleLineViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            mapView = new MapView()
            {
                Map = new Map(Basemap.CreateLightGrayCanvasVector()),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(mapView);

            scaleLine = new ScaleLine()
            {
                MapView = mapView,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Frame = new CoreGraphics.CGRect(0, 0, 240, 52)
            };
            this.View.AddSubview(scaleLine);

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
            scaleLine.LeadingAnchor.ConstraintEqualTo(mapView.LeadingAnchor, 20).Active = true;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            // Attach the scaleline to always sit right on top of the attribution text
            scaleLine.BottomAnchor.ConstraintEqualTo(mapView.AttributionTopAnchor, -10).Active = true;
        }
    }
}