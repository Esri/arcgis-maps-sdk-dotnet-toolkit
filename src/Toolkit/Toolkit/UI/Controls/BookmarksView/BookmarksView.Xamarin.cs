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
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class BookmarksView
    {
        private GeoView _geoView;

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

        private IEnumerable<Bookmark> _bookmarksOverrideImpl;

        private IEnumerable<Bookmark> BookmarksOverrideImpl
        {
            get
            {
                return _bookmarksOverrideImpl;
            }

            set
            {
                if (value != _bookmarksOverrideImpl)
                {
                    _bookmarksOverrideImpl = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Configures events and updates display when the <see cref="GeoView" /> property changes.
        /// </summary>
        /// <param name="oldView">The previously set view.</param>
        /// <param name="newView">The new view.</param>
        private void OnViewChanged(GeoView oldView, GeoView newView)
        {
            if (oldView != null)
            {
                (oldView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
            }

            if (newView != null)
            {
                (newView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
            }

            Refresh();
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView mv && e.PropertyName == nameof(mv.Map))
            {
                ConfigureGeoDocEvents(mv);
            }
            else if (sender is SceneView sv && e.PropertyName == nameof(sv.Scene))
            {
                ConfigureGeoDocEvents(sv);
            }
            Refresh();
        }
    }
}
#endif