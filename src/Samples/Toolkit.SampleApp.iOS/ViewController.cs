using System;

using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    public partial class ViewController : UIViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            mapView.Map = new Mapping.Map(Mapping.Basemap.CreateLightGrayCanvasVector());
            scaleLine.MapView = mapView;
            compass.GeoView = mapView;
            compass.AutoHide = false;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            // Attach the scaleline to always sit right on top of the attribution text
            scaleLine.BottomAnchor.ConstraintEqualTo(mapView.AttributionTopAnchor, -10).Active = true;
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}