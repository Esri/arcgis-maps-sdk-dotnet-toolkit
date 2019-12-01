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
            VerticalScrollBarEnabled = true;

            ItemClick += ListView_ItemClick;
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            NavigateToBookmark(_currentBookmarkList[e.Position]);

            SetSelection(-1);
        }

        private void Refresh()
        {
            try
            {
                _currentBookmarkList = GetCurrentBookmarkList();
                if (_currentBookmarkList == null)
                {
                    Adapter = null;
                    return;
                }

                if (Adapter == null)
                {
                    Adapter = new BookmarksAdapter(Context, _currentBookmarkList);
                }
                else
                {
                    ((BookmarksAdapter)Adapter).SetList(_currentBookmarkList);
                }
            }
            catch (ObjectDisposedException)
            {
                // Happens when navigating away on Forms Android
                return;
            }
        }
    }
}
#endif