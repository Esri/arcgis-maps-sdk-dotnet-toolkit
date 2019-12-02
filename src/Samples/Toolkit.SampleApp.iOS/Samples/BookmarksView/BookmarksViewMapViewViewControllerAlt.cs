using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "BookmarksView", DisplayName = "BookmarksView Map (Modal)", Description = "Shows bookmarks with a map; hidden under button")]
    public partial class BookmarksViewMapViewViewControllerAlt : UIViewController
    {
        private BookmarksView _bookmarksView;
        private BookmarksVC _bookmarksVC;
        private MapView _mapView;
        private UIBarButtonItem _showBookmarksButton;

        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            _mapView = new MapView()
            {
                Map = new Map(new Uri(_mapUrl)),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

             this.View.AddSubview(_mapView);

            _bookmarksView = new BookmarksView()
            {
                GeoView = _mapView,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            _mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            _mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;

            _showBookmarksButton = new UIBarButtonItem("Bookmarks", UIBarButtonItemStyle.Plain, ShowBookmarksClicked);
            NavigationItem.SetRightBarButtonItem(_showBookmarksButton, false);
        }

        private void ShowBookmarksClicked(object sender, EventArgs e)
        {
            if (_bookmarksVC == null)
            {
                _bookmarksVC = new BookmarksVC(_bookmarksView);
            }
            PresentModalViewController(new UINavigationController(_bookmarksVC), true);
        }
    }
}