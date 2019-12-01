// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// View controller to simplify the presentation of a modal bookmarks view.
    /// Present this view controller modally to show a list of bookmarks to choose from modally.
    /// Show <see cref="Bookmarks" /> directly in your view if you want to
    /// lay out and manage the display of the bookmarks list yourself.
    /// </summary>
    public class BookmarksVC : UIViewController
    {
        private Bookmarks _bookmarksView;
        private UIBarButtonItem _closeButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="BookmarksVC"/> class for showing the <see cref="Bookmarks" /> view modally.
        /// </summary>
        /// <param name="bookmarksView">The already-created bookmarks view.</param>
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
