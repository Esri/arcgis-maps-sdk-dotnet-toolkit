using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using System;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "BookmarksView", DisplayName = "BookmarksView - Modal presentation", Description = "Demonstrates modal usage - great on iPad and iPhone.")]
    public partial class BookmarksModalSampleViewController : UIViewController
    {
        // Hold references to UI controls.
        private MapView _myMapView;
        private BookmarksView _bookmarksView;
        private UIBarButtonItem _showBookmarksButton;

        public BookmarksModalSampleViewController()
        {
            Title = "Show bookmarks modally";
        }

        private void Initialize()
        {
            // Create and show the map.
            _myMapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2"));
        }

        public override void LoadView()
        {
            // Create the views.
            View = new UIView() { BackgroundColor = UIColor.White };

            _myMapView = new MapView();
            _myMapView.TranslatesAutoresizingMaskIntoConstraints = false;

            _bookmarksView = new BookmarksView();
            _bookmarksView.GeoView = _myMapView;

            _showBookmarksButton = new UIBarButtonItem(UIBarButtonSystemItem.Bookmarks);

            // Note: this won't work if there's no navigation controller.
            NavigationItem.RightBarButtonItem = _showBookmarksButton;

            // Add the views.
            View.AddSubviews(_myMapView);

            // Lay out the views.
            NSLayoutConstraint.ActivateConstraints(new[]{
                _myMapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _myMapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                _myMapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _myMapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor)
            });
        }

        private void _bookmarksView_BookmarkSelected(object sender, Bookmark e)
        {
            DismissModalViewController(true);
        }

        private void ShowBookmarks_Clicked(object sender, EventArgs e)
        {
            // Note: BookmarksView is a UIViewController.
            // This shows bookmarks modally on iPhone, in a popover on iPad
            _bookmarksView.ModalPresentationStyle = UIModalPresentationStyle.Popover;
            if (_bookmarksView.PopoverPresentationController is UIPopoverPresentationController popoverPresentationController)
            {
                popoverPresentationController.Delegate = new ppDelegate();
                popoverPresentationController.BarButtonItem = _showBookmarksButton;
            }
            // Attempting to show a VC while it is already being presented is an issue (can happen with rapid tapping)
            try
            {
                PresentModalViewController(_bookmarksView, true);
            }
            catch (MonoTouchException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                // Ignore
            }
        }

        /// <summary>
        /// Helper class, used to ensure modal appears properly for each presentation style
        /// </summary>
        private class ppDelegate : UIPopoverPresentationControllerDelegate
        {
            public override UIViewController GetViewControllerForAdaptivePresentation(UIPresentationController controller, UIModalPresentationStyle style)
            {
                return new UINavigationController(controller.PresentedViewController);
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            Initialize();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (_showBookmarksButton != null)
            {
                _showBookmarksButton.Clicked -= ShowBookmarks_Clicked;
                _showBookmarksButton.Clicked += ShowBookmarks_Clicked;
            }

            if (_bookmarksView != null)
            {
                // Listen for bookmark selections so that the view can be dismissed.
                _bookmarksView.BookmarkSelected -= _bookmarksView_BookmarkSelected;
                _bookmarksView.BookmarkSelected += _bookmarksView_BookmarkSelected;
            }
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // unsub from events to prevent leaks
            if (_showBookmarksButton != null)
            {
                _showBookmarksButton.Clicked -= ShowBookmarks_Clicked;
            }

            if (_bookmarksView != null)
            {
                _bookmarksView.BookmarkSelected -= _bookmarksView_BookmarkSelected;
            }
        }
    }
}