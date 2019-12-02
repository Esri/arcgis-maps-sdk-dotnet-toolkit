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
using System.ComponentModel;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("BookmarksView")]
    [Category("ArcGIS Runtime Controls")]
    public partial class BookmarksView : IComponent
    {
        private UITableView _listView;

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only. Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public BookmarksView(IntPtr handle)
            : base(handle)
        {
            Initialize();
        }

        /// <inheritdoc />
        public override void AwakeFromNib()
        {
            var component = (IComponent)this;
            DesignTime.IsDesignMode = component.Site != null && component.Site.DesignMode;

            Initialize();

            base.AwakeFromNib();
        }

        private void Initialize()
        {
            BackgroundColor = UIColor.Clear;

            _listView = new UITableView(UIScreen.MainScreen.Bounds)
            {
                ClipsToBounds = true,
                ContentMode = UIViewContentMode.ScaleAspectFill,
                AllowsSelection = true,
                Bounces = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                RowHeight = UITableView.AutomaticDimension
            };
            _listView.RegisterClassForCellReuse(typeof(UITableViewCell), BookmarksTableSource.CellId);
            AddSubview(_listView);

            _listView.LeftAnchor.ConstraintEqualTo(LeftAnchor).Active = true;
            _listView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            _listView.RightAnchor.ConstraintEqualTo(RightAnchor).Active = true;
            _listView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;

            InvalidateIntrinsicContentSize();
        }

        /// <inheritdoc />
        public override CGSize IntrinsicContentSize => _listView.ContentSize;

        /// <inheritdoc />
        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
        }

        /// <inheritdoc cref="IComponent.Site" />
        ISite IComponent.Site { get; set; }

        private EventHandler _disposed;

        /// <summary>
        /// Internal use only
        /// </summary>
        event EventHandler IComponent.Disposed
        {
            add => _disposed += value;
            remove => _disposed -= value;
        }

        private void Refresh()
        {
            if (_listView == null)
            {
                return;
            }

            if (CurrentBookmarkList == null)
            {
                _listView.Source = null;
                _listView.ReloadData();
                InvalidateIntrinsicContentSize();
                return;
            }

            if (_listView.Source is BookmarksTableSource oldTableSource)
            {
                oldTableSource.CollectionChanged -= UpdateSourceCollection;
                oldTableSource.BookmarkSelected -= HandleBookmarkSelected;
            }

            var newTableSource = new BookmarksTableSource(CurrentBookmarkList);
            _listView.Source = newTableSource;
            newTableSource.CollectionChanged += UpdateSourceCollection;
            newTableSource.BookmarkSelected += HandleBookmarkSelected;

            _listView.ReloadData();
            InvalidateIntrinsicContentSize();
            SetNeedsUpdateConstraints();
            UpdateConstraints();
        }

        private void HandleBookmarkSelected(object sender, BookmarkSelectedEventArgs e) => SelectAndNavigateToBookmark(e.Bookmark);

        private void UpdateSourceCollection(object sender, EventArgs e)
        {
            InvokeOnMainThread(() =>
            {
                _listView.ReloadData();
                InvalidateIntrinsicContentSize();
                SetNeedsUpdateConstraints();
                UpdateConstraints();
            });
        }
    }
}