using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.ObjectModel;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "BookmarksView", DisplayName = "BookmarksView - Interactive Event Tester", Description = "Controls for interactively verifying bookmarks implementation.")]
    public partial class BookmarksMapEventTestController : UIViewController
    {
        private BookmarksView _bookmarksView;
        private MapView _mapView;
        private UIBarButtonItem _AddItemButton;
        private UIBarButtonItem _removeItemButton;
        private UIBarButtonItem _switchMapButton;
        private UIBarButtonItem _setListObservableButton;
        private UIBarButtonItem _clearListButton;
        private UIBarButtonItem _addToMapBookmarksButton;

        private readonly string[] _mapUrl = new[]
        {
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee",
            "https://runtime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2",
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=e50fafe008ac4ce4ad2236de7fd149c3"
        };
        private int _mapIndex = 0;

        private ObservableCollection<Bookmark> _bookmarksObservable = new ObservableCollection<Bookmark>();

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            configureManualList();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _mapView = new MapView()
            {
                Map = new Map(new Uri(_mapUrl[0])),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            View.AddSubview(_mapView);

            _bookmarksView = new BookmarksView()
            {
                GeoView = _mapView
            };
            AddChildViewController(_bookmarksView);
            _bookmarksView.View.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(_bookmarksView.View);

            UIToolbar toolbar = new UIToolbar();
            toolbar.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(toolbar);

            _AddItemButton = new UIBarButtonItem(UIBarButtonSystemItem.Add);
            _removeItemButton = new UIBarButtonItem(UIBarButtonSystemItem.Trash);
            _switchMapButton = new UIBarButtonItem(UIBarButtonSystemItem.FastForward);
            _setListObservableButton = new UIBarButtonItem("Set list", UIBarButtonItemStyle.Plain, ShowObservableList_Click);
            _clearListButton = new UIBarButtonItem("Clear list", UIBarButtonItemStyle.Plain, ClearList_Click);
            _addToMapBookmarksButton = new UIBarButtonItem(UIBarButtonSystemItem.Bookmarks);

            toolbar.Items = new[]
            {
                new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                _setListObservableButton, _clearListButton, _AddItemButton, _removeItemButton, _addToMapBookmarksButton, _switchMapButton
            };
            _AddItemButton.Clicked += AddToObservableButton_Click;
            _removeItemButton.Clicked += RemoveFromObservableButton_Click;
            _switchMapButton.Clicked += SwitchMapButton_Click;
            _addToMapBookmarksButton.Clicked += AddBookmarkToMap_Click;

            _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            _mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            _mapView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            _bookmarksView.View.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _bookmarksView.View.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            _bookmarksView.View.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            _bookmarksView.View.BottomAnchor.ConstraintEqualTo(toolbar.TopAnchor).Active = true;

            toolbar.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor).Active = true;
            toolbar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            toolbar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
        }

        private void configureManualList()
        {
            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            _bookmarksObservable.Add(new Bookmark("O: 0,0", vp));

            Viewpoint vp2 = new Viewpoint(48.850684, 2.347735, 1000, new Camera(48.850684, 2.347735, 1500, 100, 35, 0));
            _bookmarksObservable.Add(new Bookmark("O: Paris", vp2));

            Viewpoint vp3 = new Viewpoint(48.034682, 13.710577, 1300, new Camera(48.034682, 13.710577, 2000, 100, 35, 0));
            _bookmarksObservable.Add(new Bookmark("O: Pühret, Austria", vp3));


            Viewpoint vp4 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));
            _bookmarksObservable.Add(new Bookmark("O: Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
        }

        private void ShowObservableList_Click(object sender, EventArgs e) => _bookmarksView.BookmarksOverride = _bookmarksObservable;

        private void ClearList_Click(object sender, EventArgs e) => _bookmarksView.BookmarksOverride = null;

        private void RemoveFromObservableButton_Click(object sender, EventArgs e) => _bookmarksObservable.RemoveAt(0);

        private void AddToObservableButton_Click(object sender, EventArgs e)
        {
            Viewpoint vp5 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));

            _bookmarksObservable.Add(new Bookmark($"O: {_bookmarksObservable.Count}", vp5));
        }

        private void AddBookmarkToMap_Click(object sender, EventArgs e)
        {
            Viewpoint vp5 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));

            _mapView.Map.Bookmarks.Add(new Bookmark($"M: {_mapView.Map.Bookmarks.Count}", vp5));
        }

        private void SwitchMapButton_Click(object sender, EventArgs e) => _mapView.Map = new Map(new Uri(_mapUrl[++_mapIndex % _mapUrl.Length]));
    }
}