using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Threading.Tasks;
using UIKit;

namespace ARToolkit.SampleApp.Samples
{

    [SampleInfo(DisplayName = "Earth", Description = "Shows the entire earth hovering in front of you allowing you to walk around it")]
    public partial class EarthViewController : UIViewController
    {
        ARSceneView? ARView;

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (View == null)
            {
                throw new InvalidOperationException("View was unexpectedly null");
            }

            ARView = new ARSceneView { TranslatesAutoresizingMaskIntoConstraints = false };

            View.AddSubview(ARView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                ARView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                ARView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                ARView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                ARView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });

            var basemap = new Basemap(new ArcGISTiledLayer(new Uri("https://www.arcgis.com/home/item.html?id=10df2279f9684e4a9f6a7f08febac2a9")));
            await basemap.LoadAsync();
            var scene = new Scene(basemap);

            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.ElevationExaggeration = 50;
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            ARView.TranslationFactor = 100000000;
            // Set pitch to 0 so looking forward looks "down" on earth from space
            ARView.OriginCamera = new Camera(new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84), 0, 0, 0);
            ARView.NorthAlign = false;
            await scene.LoadAsync();
            ARView.Scene = scene;
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (ARView != null)
            {
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
                try
                {
                    await ARView.StopTrackingAsync();
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }
}