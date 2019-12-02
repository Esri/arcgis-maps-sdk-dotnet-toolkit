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

using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The BookmarksView view presents bookmarks, either from a list defined by <see cref="BookmarkList" /> or
    /// the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    public class Bookmarks : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bookmarks"/> class, which shows bookmarks and handles
        /// <see cref="GeoView" /> navigation to the selected bookmark.
        /// </summary>
        public Bookmarks()
        {
            HorizontalOptions = LayoutOptions.FillAndExpand;
            VerticalOptions = LayoutOptions.FillAndExpand;

            MinimumHeightRequest = 50;
            MinimumWidthRequest = 100;
        }

        /// <summary>
        /// Gets or sets the list of bookmarks to display.
        /// Otherwise, the bookmarks from the Map or Scene shown in the associated <see cref="GeoView" /> are displayed.
        /// </summary>
        /// <remarks>If set to a <see cref="System.Collections.Specialized.INotifyCollectionChanged" />, the view will be updated with collection changes.</remarks>
        /// <seealso cref="BookmarksOverrideProperty" />
        public IList<Bookmark> BookmarksOverride
        {
            get { return (IList<Bookmark>)GetValue(BookmarksOverrideProperty); }
            set { SetValue(BookmarksOverrideProperty, value); }
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

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(Bookmarks), null, BindingMode.OneWay, null);

        /// <summary>
        /// Identifies the <see cref="BookmarksOverride"/> bindable property.
        /// </summary>
        public static readonly BindableProperty BookmarksOverrideProperty =
            BindableProperty.Create(nameof(BookmarksOverride), typeof(IList<Bookmark>), typeof(Bookmarks), null, BindingMode.OneWay, null);
    }
}
