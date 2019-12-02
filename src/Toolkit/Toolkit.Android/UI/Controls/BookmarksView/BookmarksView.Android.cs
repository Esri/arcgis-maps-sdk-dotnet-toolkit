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

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Util;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class BookmarksView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BookmarksView"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public BookmarksView(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BookmarksView"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public BookmarksView(Context context, IAttributeSet attr)
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
            SelectAndNavigateToBookmark(CurrentBookmarkList.ElementAt(e.Position));

            SetSelection(-1);
        }

        private void Refresh()
        {
            try
            {
                if (CurrentBookmarkList == null)
                {
                    Adapter = null;
                    return;
                }

                if (Adapter == null)
                {
                    Adapter = new BookmarksAdapter(Context, CurrentBookmarkList);
                }
                else
                {
                    ((BookmarksAdapter)Adapter).SetList(CurrentBookmarkList);
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