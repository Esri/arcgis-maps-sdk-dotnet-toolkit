using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "BookmarksView", DisplayName = "BookmarksView Scene Split", Description = "Shows bookmarks with a map")]
    public partial class BookmarksViewSceneViewViewController : UIViewController
    {
        private BookmarksView _bookmarksView;
        private SceneView _sceneView;

        private const string _sceneUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=6b6588041965408e84ba319e12d9d7ad";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _sceneView = new SceneView()
            {
                Scene = new Scene(new Uri(_sceneUrl)),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            configureSlides();

            this.View.AddSubview(_sceneView);

            _bookmarksView = new BookmarksView()
            {
                GeoView = _sceneView,
            };
            _bookmarksView.View.TranslatesAutoresizingMaskIntoConstraints = false;
            AddChildViewController(_bookmarksView);
            View.AddSubview(_bookmarksView.View);

            _sceneView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _sceneView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            _sceneView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            _sceneView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            _bookmarksView.View.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _bookmarksView.View.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            _bookmarksView.View.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            _bookmarksView.View.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
        }

        private async void configureSlides()
        {
            // Note: adding bookmarks manually because RT doesn't support reading bookmarks from web scenes as of 100.7
            Scene scene = _sceneView.Scene;
            await scene.LoadAsync();

            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            scene.Bookmarks.Add(new Bookmark("0,0", vp));

            Viewpoint vp2 = new Viewpoint(47.876126, -121.779435, 1000, new Camera(47.876126, -121.779435, 1500, 100, 35, 0));
            scene.Bookmarks.Add(new Bookmark("🍎 Apples", vp2));
        }
    }
}