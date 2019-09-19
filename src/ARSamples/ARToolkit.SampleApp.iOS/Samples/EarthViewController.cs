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

    [SampleInfo(DisplayName = "Earth", Description = "Shows the entire earth hovering in front of you allowing you to walk around it")]
    public partial class EarthViewController : UIViewController
    {
        ARSceneView ARView;
        
        public EarthViewController() : base()
        {
        }
        public EarthViewController(IntPtr handle) : base(handle)
        {
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Create a new AR Scene View, set its scene, and provide the coordinates for laying it out
            ARView = new ARSceneView();
            // Add the ARSceneView to the Subview
            View.AddSubview(ARView);

            var p = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var scene = new Scene(Basemap.CreateImagery());
            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("http://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.ElevationExaggeration = 10;
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            ARView.TranslationFactor = 100000000;
            // Set pitch to 0 so looking forward looks "down" on earth from space
            ARView.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84), 0, 0, 0);
            ARView.NorthAlign = false;
            await scene.LoadAsync();
            ARView.Scene = scene;

            _ = ARView.StartTrackingAsync();
        }

        public override void ViewDidLayoutSubviews()
        {
            // Fill the screen with the map
            ARView.Frame = new CoreGraphics.CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);
            base.ViewDidLayoutSubviews();
        }
    }
}