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

using System;
using System.ComponentModel;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class Compass : System.ComponentModel.INotifyPropertyChanged
    {
        private double _heading;

        private double HeadingImpl
        {
            get => _heading;
            set
            {
                if (_heading != value)
                {
                    if (GeoView != null && !_headingSetByGeoView)
                    {
                        throw new InvalidOperationException("The Heading Property is read-only when the GeoView property has been assigned");
                    }

                    _heading = value;
                    UpdateCompassRotation(true);
                    OnPropertyChanged(nameof(Heading));
                }
            }
        }

        private bool _autoHide = true;

        private bool AutoHideImpl
        {
            get => _autoHide;
            set
            {
                _autoHide = value;
                SetVisibility(!(value && _heading == 0));
                OnPropertyChanged(nameof(AutoHide));
            }
        }

        private GeoView _geoView;

        /// <summary>
        /// Gets or sets the GeoView for which the heading is displayed. This will accurately reflect the heading at the center of the GeoView.
        /// </summary>
        /// <seealso cref="Heading"/>
        public GeoView GeoView
        {
            get => _geoView;
            set
            {
                var oldView = _geoView;
                _geoView = value;
                WireGeoViewPropertyChanged(oldView, _geoView);
                OnPropertyChanged(nameof(GeoView));
            }
        }

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
#endif