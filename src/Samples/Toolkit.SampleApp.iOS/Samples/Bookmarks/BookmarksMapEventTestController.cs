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
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks - Interactive Event Tester", Description = "Controls for interactively verifying bookmarks implementation.")]
    public partial class BookmarksMapEventTestController : UIViewController
    {
        private Bookmarks bookmarks;
        private MapView mapView;
        private UISegmentedControl _manualSegment;
        private UIBarButtonItem _AddItemButton;
        private UIBarButtonItem _removeItemButton;
        private UIBarButtonItem _switchMapButton;
        private UIBarButtonItem _setListStaticButton;
        private UIBarButtonItem _setListObservableButton;

        private readonly string[] _mapUrl = new[]
        {
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee",
            "https://runtime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2",
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=e50fafe008ac4ce4ad2236de7fd149c3"
        };
        private int _mapIndex = 0;

        private ObservableCollection<Bookmark> _bookmarksObservable = new ObservableCollection<Bookmark>();
        private List<Bookmark> _bookmarksStatic = new List<Bookmark>();

        public BookmarksMapEventTestController()
        {
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            configureManualList();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            mapView = new MapView()
            {
                Map = new Map(new Uri(_mapUrl[0])),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            this.View.AddSubview(mapView);

            bookmarks = new Bookmarks()
            {
                GeoView = mapView,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(bookmarks);

            UIToolbar toolbar = new UIToolbar();
            toolbar.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(toolbar);

            _manualSegment = new UISegmentedControl("Map", "List");
            _AddItemButton = new UIBarButtonItem(UIBarButtonSystemItem.Add);
            _removeItemButton = new UIBarButtonItem(UIBarButtonSystemItem.Trash);
            _switchMapButton = new UIBarButtonItem(UIBarButtonSystemItem.FastForward);
            _setListObservableButton = new UIBarButtonItem("O", UIBarButtonItemStyle.Plain, ShowObservableList_Click);
            _setListStaticButton = new UIBarButtonItem("S", UIBarButtonItemStyle.Plain, ShowStaticList_Click);

            toolbar.Items = new[]
            {
                new UIBarButtonItem(_manualSegment) { Width = 100 },
                new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                _AddItemButton, _removeItemButton, _switchMapButton, _setListStaticButton, _setListObservableButton
            };

            _manualSegment.ValueChanged += ToggleListCheckbox_Click;
            _AddItemButton.Clicked += AddToObservableButton_Click;
            _removeItemButton.Clicked += RemoveFromObservableButton_Click;
            _switchMapButton.Clicked += SwitchMapButton_Click;

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            bookmarks.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            bookmarks.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            bookmarks.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            bookmarks.BottomAnchor.ConstraintEqualTo(toolbar.TopAnchor).Active = true;

            toolbar.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor).Active = true;
            toolbar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            toolbar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
        }

        private void configureManualList()
        {
            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            _bookmarksStatic.Add(new Bookmark("S: 0,0", vp));
            _bookmarksObservable.Add(new Bookmark("O: 0,0", vp));

            Viewpoint vp2 = new Viewpoint(48.850684, 2.347735, 1000, new Camera(48.850684, 2.347735, 1500, 100, 35, 0));
            _bookmarksStatic.Add(new Bookmark("S: Paris", vp2));
            _bookmarksObservable.Add(new Bookmark("O: Paris", vp2));

            Viewpoint vp3 = new Viewpoint(48.034682, 13.710577, 1300, new Camera(48.034682, 13.710577, 2000, 100, 35, 0));
            _bookmarksStatic.Add(new Bookmark("S: Pühret, Austria", vp3));
            _bookmarksObservable.Add(new Bookmark("O: Pühret, Austria", vp3));


            Viewpoint vp4 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));
            _bookmarksStatic.Add(new Bookmark("S: Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
            _bookmarksObservable.Add(new Bookmark("O: Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
        }

        private void ToggleListCheckbox_Click(object sender, EventArgs e)
        {
            switch (_manualSegment.SelectedSegment)
            {
                case 0:
                    bookmarks.PrefersBookmarksList = false;
                    break;
                case 1:
                    bookmarks.PrefersBookmarksList = true;
                    break;
            }
        }

        private void ShowStaticList_Click(object sender, EventArgs e)
        {
            bookmarks.BookmarkList = _bookmarksStatic;
        }

        private void ShowObservableList_Click(object sender, EventArgs e)
        {
            bookmarks.BookmarkList = _bookmarksObservable;
        }

        private void RemoveFromObservableButton_Click(object sender, EventArgs e)
        {
            _bookmarksObservable.RemoveAt(0);
        }

        private void AddToObservableButton_Click(object sender, EventArgs e)
        {
            Viewpoint vp5 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));

            _bookmarksObservable.Add(new Bookmark($"O: {_bookmarksObservable.Count}", vp5));
        }

        private void SwitchMapButton_Click(object sender, EventArgs e)
        {
            _mapIndex++;
            mapView.Map = new Map(new Uri(_mapUrl[_mapIndex % _mapUrl.Length]));
        }
    }
}