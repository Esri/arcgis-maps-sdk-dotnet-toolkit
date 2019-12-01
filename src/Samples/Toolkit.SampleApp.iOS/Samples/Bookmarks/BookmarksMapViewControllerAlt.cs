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
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks Map (Modal)", Description = "Shows bookmarks with a map; hidden under button")]
    public partial class BookmarksMapViewControllerAlt : UIViewController
    {
        private Bookmarks bookmarks;
        private BookmarksVC _bookmarksVC;
        private MapView mapView;
        private UIBarButtonItem _showBookmarksButton;

        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            mapView = new MapView()
            {
                Map = new Map(new Uri(_mapUrl)),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

             this.View.AddSubview(mapView);

            bookmarks = new Bookmarks()
            {
                GeoView = mapView,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;

            _showBookmarksButton = new UIBarButtonItem("Bookmarks", UIBarButtonItemStyle.Plain, ShowBookmarksClicked);
            NavigationItem.SetRightBarButtonItem(_showBookmarksButton, false);
        }

        private void ShowBookmarksClicked(object sender, EventArgs e)
        {
            if (_bookmarksVC == null)
            {
                _bookmarksVC = new BookmarksVC(bookmarks);
            }
            PresentModalViewController(new UINavigationController(_bookmarksVC), true);
        }
    }
}