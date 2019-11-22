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
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks", Description = "Shows bookmarks with a map")]
    public partial class BookmarksViewController : UIViewController
    {
        private Bookmarks bookmarks;
        private MapView mapView;

        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";

        public BookmarksViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

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
            this.View.AddSubview(bookmarks);

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            bookmarks.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            bookmarks.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            bookmarks.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            bookmarks.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
        }
    }
}