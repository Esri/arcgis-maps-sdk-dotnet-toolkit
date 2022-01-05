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
        ARSceneView ARView;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ARView = new ARSceneView { TranslatesAutoresizingMaskIntoConstraints = false };

            View.AddSubview(ARView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                ARView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                ARView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                ARView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                ARView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var scene = new Scene(Basemap.CreateImagery());
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

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (ARView != null)
            {
                ARView.LocationDataSource = new SystemLocationDataSource();
                _ = ARView.StartTrackingAsync();
            }
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            if (ARView != null)
            {
                _ = ARView.StopTrackingAsync();
            }
        }
    }
}