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

using System.Collections.Specialized;
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("BookmarksView")]
    [Category("ArcGIS Runtime Controls")]
    public partial class BookmarksView
    {
        private UITableView? _listView;

        /// <inheritdoc />
        public override void LoadView()
        {
            base.LoadView();

            // Create views
            View = new UIView();

            _listView = new UITableView(UIScreen.MainScreen.Bounds)
            {
                ClipsToBounds = true,
                ContentMode = UIViewContentMode.ScaleAspectFill,
                AllowsSelection = true,
                Bounces = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                RowHeight = UITableView.AutomaticDimension,
            };
            _listView.RegisterClassForCellReuse(typeof(UITableViewCell), BookmarksTableSource.CellId);

            // Set up the list view source
            var tableSource = new BookmarksTableSource(_dataSource);
            tableSource.BookmarkSelected += HandleBookmarkSelected;
            _listView.Source = tableSource;

            var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(tableSource)
            {
                OnEventAction = (instance, source, eventArgs) =>
                {
                    _listView.ReloadData();
                },
                OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
            };
            tableSource.CollectionChanged += listener.OnEvent;

            // Show and lay out views
            View.AddSubview(_listView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _listView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _listView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _listView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _listView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
            });

            // Set a title if not already set
            if (NavigationItem != null && string.IsNullOrEmpty(Title))
            {
                Title = "Bookmarks";
            }

            // Do initial data reload
            _listView.ReloadData();
        }

        private void HandleBookmarkSelected(object sender, Bookmark bookmark) => SelectAndNavigateToBookmark(bookmark);
    }
}