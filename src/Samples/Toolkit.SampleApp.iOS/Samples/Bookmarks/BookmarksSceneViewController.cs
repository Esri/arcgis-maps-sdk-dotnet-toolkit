using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
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
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks Scene Split", Description = "Shows bookmarks with a map")]
    public partial class BookmarksSceneViewController : UIViewController
    {
        private Bookmarks bookmarks;
        private SceneView sceneView;

        private const string _sceneUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=6b6588041965408e84ba319e12d9d7ad";

        public BookmarksSceneViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            sceneView = new SceneView()
            {
                Scene = new Scene(new Uri(_sceneUrl)),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            configureSlides();

            this.View.AddSubview(sceneView);

            bookmarks = new Bookmarks()
            {
                GeoView = sceneView,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(bookmarks);

            sceneView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            sceneView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            sceneView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            sceneView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            bookmarks.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            bookmarks.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            bookmarks.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            bookmarks.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
        }

        private async void configureSlides()
        {
            // Note: adding bookmarks manually because RT doesn't support reading bookmarks from web scenes as of 100.7
            Scene scene = sceneView.Scene;
            await scene.LoadAsync();

            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            scene.Bookmarks.Add(new Bookmark("0,0", vp));

            Viewpoint vp2 = new Viewpoint(47.876126, -121.779435, 1000, new Camera(47.876126, -121.779435, 1500, 100, 35, 0));
            scene.Bookmarks.Add(new Bookmark("🍎 Apples", vp2));
        }
    }
}