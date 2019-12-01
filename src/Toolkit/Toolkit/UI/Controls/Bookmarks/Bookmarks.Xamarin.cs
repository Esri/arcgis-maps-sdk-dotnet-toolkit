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

#if XAMARIN

using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class Bookmarks
    {
        private GeoView _geoView;

        /// <summary>
        /// Gets or sets the geoview that will be controlled by this view. Bookmarks will be shown from the
        /// <see cref="GeoView" />'s Map or Scene unless <see cref="PrefersBookmarksList" /> and <see cref="BookmarkList" /> are set.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        private GeoView GeoViewImpl
        {
            get => _geoView;
            set
            {
                if (_geoView != value)
                {
                    var oldView = _geoView;
                    _geoView = value;
                    OnViewChanged(oldView, _geoView);
                }
            }
        }

        private IList<Bookmark> BookmarkListImpl { get; set; }

        private bool PrefersBookmarkListImpl { get; set; } = false;
    }
}
#endif