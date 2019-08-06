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
    [SampleInfoAttribute(Category = "Compass", DisplayName = "Compass - MapView", Description = "Compass used with a MapView")]
    public partial class CompassMapViewController : UIViewController
    {
        Compass compass;
        UISlider slider;
        MapView mapView;
        NSLayoutConstraint widthConstraint;
        NSLayoutConstraint heightConstraint;

        public CompassMapViewController()
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

            compass = new Compass()
            {
                GeoView = mapView,
                AutoHide = false,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(compass);

            slider = new UISlider()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                MaxValue = 100,
                MinValue = 10,
                Value = 50
            };
            this.View.AddSubview(slider);

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(slider.TopAnchor).Active = true;
            compass.TrailingAnchor.ConstraintEqualTo(mapView.TrailingAnchor, -20).Active = true;
            compass.TopAnchor.ConstraintEqualTo(mapView.TopAnchor, 20).Active = true;
            (widthConstraint = compass.WidthAnchor.ConstraintEqualTo(50)).Active = true;
            (heightConstraint = compass.HeightAnchor.ConstraintEqualTo(50)).Active = true;
            slider.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 20).Active = true;
            slider.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -20).Active = true;
            slider.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, -20).Active = true;
            //slider.HeightAnchor.ConstraintEqualTo(50);
        }

        public override void ViewDidLayoutSubviews()
        {
           
            base.ViewDidLayoutSubviews();
        }

        public override void ViewWillAppear(bool animated)
        {
            slider.ValueChanged += Slider_ValueChanged;
            base.ViewWillAppear(animated);
        }
        public override void ViewDidDisappear(bool animated)
        {
            slider.ValueChanged -= Slider_ValueChanged;
            base.ViewDidDisappear(animated);
        }

        private void Slider_ValueChanged(object sender, EventArgs e)
        {
            widthConstraint.Constant = heightConstraint.Constant = slider.Value;
        }
    }
}