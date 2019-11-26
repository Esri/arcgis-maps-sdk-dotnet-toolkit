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
using System.Collections.ObjectModel;
using System.Linq;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks Scene Split (List override)", Description = "Shows bookmarks from manual list with scene")]
    public partial class BookmarksSceneViewWithListController : UIViewController
    {
        private Bookmarks bookmarks;
        private SceneView sceneView;

        private const string _sceneUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=6b6588041965408e84ba319e12d9d7ad";

        private ObservableCollection<Bookmark> _bookmarksTestList = new ObservableCollection<Bookmark>();

        public BookmarksSceneViewWithListController()
        {
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            bookmarks.PrefersBookmarksList = true;
            bookmarks.BookmarkList = _bookmarksTestList;
            configureManualList();
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

        private void configureManualList()
        {
            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            _bookmarksTestList.Add(new Bookmark("0,0", vp));

            Viewpoint vp2 = new Viewpoint(48.850684, 2.347735, 1000, new Camera(48.850684, 2.347735, 1500, 100, 35, 0));
            _bookmarksTestList.Add(new Bookmark("Paris", vp2));

            Viewpoint vp3 = new Viewpoint(48.034682, 13.710577, 1300, new Camera(48.034682, 13.710577, 2000, 100, 35, 0));
            _bookmarksTestList.Add(new Bookmark("Pühret, Austria", vp3));

            Viewpoint vp4 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));
            _bookmarksTestList.Add(new Bookmark("Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
        }
    }
}