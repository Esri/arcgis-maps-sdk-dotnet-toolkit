using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// View controller to simplify the presentation of a modal bookmarks view.
    /// </summary>
    public class BookmarksVC : UIViewController
    {
        private Bookmarks _bookmarksView;
        private UIBarButtonItem _closeButton;

        public BookmarksVC(Bookmarks bookmarksView)
            : base()
        {
            if (bookmarksView == null)
            {
                throw new ArgumentNullException(nameof(bookmarksView), "Must supply bookmarks view.");
            }

            Title = "Bookmarks";
            _bookmarksView = bookmarksView;
            ModalPresentationStyle = UIModalPresentationStyle.FormSheet;
        }

        public override void LoadView()
        {
            base.LoadView();

            View.AddSubview(_bookmarksView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _bookmarksView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _bookmarksView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _bookmarksView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _bookmarksView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });

            _closeButton = new UIBarButtonItem("Close", UIBarButtonItemStyle.Plain, null);

            if (NavigationItem != null)
            {
                NavigationItem.RightBarButtonItem = _closeButton;
            }
        }

        private void BookmarkSelected(object sender, Bookmarks.BookmarkSelectedEventArgs e)
        {
            DismissViewController(true, null);
        }

        private void CloseClicked(object sender, EventArgs e)
        {
            DismissViewController(true, null);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (_bookmarksView != null)
            {
                _bookmarksView.BookmarkSelected -= BookmarkSelected;
                _bookmarksView.BookmarkSelected += BookmarkSelected;
            }

            if (_closeButton != null)
            {
                _closeButton.Clicked -= CloseClicked;
                _closeButton.Clicked += CloseClicked;
            }
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            if (_bookmarksView != null)
            {
                _bookmarksView.BookmarkSelected -= BookmarkSelected;
            }

            if (_closeButton != null)
            {
                _closeButton.Clicked -= CloseClicked;
            }
        }
    }
}
