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
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class ScaleLine
    {
        private double _mapScale;

        /// <summary>
        /// Gets or sets the platform-specific implementation of the <see cref="MapScale"/> property.
        /// </summary>
        private double MapScaleImpl
        {
            get => _mapScale;
            set
            {
                if (MapView != null && !_scaleSetByMapView)
                {
                    throw new System.InvalidOperationException("The MapScale Property is read-only when the MapView property has been assigned");
                }

                _mapScale = value;
                Refresh();
            }
        }

        private double _targetWidth = 200;

        /// <summary>
        /// Gets or sets the platform-specific implementation of the <see cref="TargetWidth"/> property.
        /// </summary>
        private double TargetWidthImpl
        {
            get => _targetWidth;
            set
            {
                _targetWidth = value;
                Refresh();
            }
        }

        private MapView _mapView;

        /// <summary>
        /// Gets or sets the MapView for which the scale is displayed. This will accurately reflect the scale at the center of the MapView.
        /// </summary>
        public MapView MapView
        {
            get => _mapView;

            set
            {
                var oldView = _mapView;
                _mapView = value;
                WireMapViewPropertyChanged(oldView, _mapView);
            }
        }

        private MapView GetMapView(ScaleLine scaleline) => scaleline.MapView;
    }
}
#endif