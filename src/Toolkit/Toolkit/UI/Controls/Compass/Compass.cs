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

using System.ComponentModel;
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The Compass Control showing the heading on the map when the rotation is not North up / 0.
    /// </summary>
    public partial class Compass : Control
    {
        private const double DefaultSize = 30;
        private bool _headingSetByGeoView;

#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="Compass"/> class.
        /// </summary>
        public Compass()
            : base()
        {
            Initialize();
        }
#endif

        /// <summary>
        /// Gets or sets the Heading for the compass.
        /// </summary>
        /// <remarks>
        /// This property is read-only if the <see cref="GeoView"/> property is assigned.
        /// </remarks>
        public double Heading
        {
            get => HeadingImpl;
            set => HeadingImpl = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0.
        /// </summary>
        public bool AutoHide
        {
            get => AutoHideImpl;
            set => AutoHideImpl = value;
        }

        private void WireGeoViewPropertyChanged(GeoView oldGeoView, GeoView newGeoView)
        {
            var inpc = oldGeoView as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged -= GeoView_PropertyChanged;
            }

            inpc = newGeoView as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged += GeoView_PropertyChanged;
            }

            UpdateCompassFromGeoView(newGeoView);
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var view = GeoView;
            if ((view is MapView && e.PropertyName == nameof(MapView.MapRotation)) ||
                (view is SceneView && e.PropertyName == nameof(SceneView.Camera)))
            {
                UpdateCompassFromGeoView(GeoView);
            }
        }

        private void UpdateCompassFromGeoView(GeoView view)
        {
            _headingSetByGeoView = true;
            Heading = (view is MapView) ? ((MapView)view).MapRotation : (view is SceneView ? ((SceneView)view).Camera.Heading : 0);
            _headingSetByGeoView = false;
        }

        private void ResetRotation()
        {
            var view = GeoView;
            if (view is MapView)
            {
                ((MapView)view).SetViewpointRotationAsync(0);
            }
            else if (view is SceneView)
            {
                var sv = (SceneView)view;
                var c = sv.Camera;
                sv.SetViewpointCameraAsync(c.RotateTo(0, c.Pitch, c.Roll));
            }
        }
    }
}