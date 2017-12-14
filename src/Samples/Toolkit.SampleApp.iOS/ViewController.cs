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
            mapView.ViewpointChanged += (s, e) => { scaleLine.MapScale = mapView.MapScale; };
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}