// /*******************************************************************************
//  * Copyright 2012-2019 Esri
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

#if __ANDROID__
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using Esri.ArcGISRuntime.Mapping;
using System.Collections.Generic;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class Bookmarks
    {
        private ListView _listView;
        private IList<Bookmark> _currentBookmarkList;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bookmarks"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public Bookmarks(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bookmarks"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public Bookmarks(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        internal void Initialize()
        {
            _listView = new ListView(Context)
            {
                ClipToOutline = true,
                Clickable = true,
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
                ScrollingCacheEnabled = false,
                PersistentDrawingCache = PersistentDrawingCaches.NoCache,
            };

            _listView.ItemClick += ListView_ItemClick;

            AddView(_listView);
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            NavigateToBookmark(_currentBookmarkList[e.Position]);

            _listView.SetSelection(-1);
        }

        private void Refresh()
        {
            if (_listView == null)
            {
                return;
            }

            _currentBookmarkList = GetCurrentBookmarkList();

            if (_currentBookmarkList == null)
            {
                _listView.Adapter = null;
                return;
            }

            try
            {
                _listView.Adapter = new BookmarksAdapter(Context, (IReadOnlyList<Bookmark>)_currentBookmarkList);
                _listView.SetHeightBasedOnChildren();
            }
            catch (ObjectDisposedException)
            {
                // Happens when navigating away on Forms Android - GeoView is disposed before bookmarks control
                _listView.Adapter = null;
                return;
            }
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            // Initialize dimensions of root layout
            MeasureChild(_listView, widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

            // Calculate the ideal width and height for the view
            var desiredWidth = PaddingLeft + PaddingRight + _listView.MeasuredWidth;
            var desiredHeight = PaddingTop + PaddingBottom + _listView.MeasuredHeight;

            // Get the width and height of the view given any width and height constraints indicated by the width and height spec values
            var width = ResolveSize(desiredWidth, widthMeasureSpec);
            var height = ResolveSize(desiredHeight, heightMeasureSpec);

            SetMeasuredDimension(width, height);
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // Forward layout call to the root layout
            _listView.Layout(PaddingLeft, PaddingTop, _listView.MeasuredWidth + PaddingLeft, _listView.MeasuredHeight + PaddingBottom);
        }
    }
}
#endif