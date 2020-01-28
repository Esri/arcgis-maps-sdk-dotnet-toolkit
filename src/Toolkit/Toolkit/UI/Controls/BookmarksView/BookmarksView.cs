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
using System.Collections.Generic;
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIViewController;
#elif __ANDROID__
using Android.App;
using Android.Views;
using Control = Android.Widget.FrameLayout;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The BookmarksView view presents bookmarks, either from a list defined by <see cref="BookmarksOverride" /> or
    /// the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    public partial class BookmarksView : Control
    {
        private BookmarksViewDataSource _dataSource = new BookmarksViewDataSource();

        /// <summary>
        /// Gets or sets the list of bookmarks to display.
        /// Otherwise, the bookmarks from the Map or Scene shown in the associated <see cref="GeoView" /> are displayed.
        /// </summary>
        /// <remarks>If set to a <see cref="System.Collections.Specialized.INotifyCollectionChanged" />, the view will be updated with collection changes.</remarks>
        public IEnumerable<Bookmark> BookmarksOverride
        {
            get => BookmarksOverrideImpl;
            set => BookmarksOverrideImpl = value;
        }

        /// <summary>
        /// Gets or sets the MapView or SceneView associated with this view. When a bookmark is selected, the viewpoint of this
        /// geoview will be set to the bookmark's viewpoint. By default, bookmarks from the geoview's Map or Scene
        /// property will be shown. To show a custom bookmark list, set <see cref="BookmarksOverride" />.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get => GeoViewImpl;
            set => GeoViewImpl = value;
        }

        private void SelectAndNavigateToBookmark(Bookmark bookmark)
        {
            if (bookmark?.Viewpoint == null)
            {
                throw new ArgumentNullException("Bookmark or bookmark viewpoint is null");
            }

            GeoView?.SetViewpointAsync(bookmark.Viewpoint);

            BookmarkSelected?.Invoke(this, bookmark);
        }

        /// <summary>
        /// Event raised when the user selects a bookmark.
        /// </summary>
        public event EventHandler<Bookmark> BookmarkSelected;
    }
}