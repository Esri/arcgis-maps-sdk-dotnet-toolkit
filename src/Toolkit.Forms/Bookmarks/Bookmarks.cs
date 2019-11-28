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

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The Legend control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="GeoView"/>.
    /// </summary>
    public class Bookmarks : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bookmarks"/> class
        /// </summary>
        public Bookmarks()
        {
            HorizontalOptions = LayoutOptions.FillAndExpand;
            VerticalOptions = LayoutOptions.FillAndExpand;

            MinimumHeightRequest = 50;
            MinimumWidthRequest = 100;
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(Bookmarks), null, BindingMode.OneWay, null);

        public static readonly BindableProperty BookmarkListProperty =
            BindableProperty.Create(nameof(BookmarkList), typeof(IList<Bookmark>), typeof(Bookmarks), null, BindingMode.OneWay, null);

        public static readonly BindableProperty PrefersBookmarksListProperty =
            BindableProperty.Create(nameof(PrefersBookmarksList), typeof(bool), typeof(Bookmarks), false, BindingMode.OneWay, null);

        public bool PrefersBookmarksList
        {
            get { return (bool)GetValue(PrefersBookmarksListProperty); }
            set { SetValue(PrefersBookmarksListProperty, value); }
        }

        public IList<Bookmark> BookmarkList
        {
            get { return (IList<Bookmark>)GetValue(BookmarkListProperty); }
            set { SetValue(BookmarkListProperty, value); }
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        /// <seealso cref="GeoViewProperty"/>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }
    }
}
