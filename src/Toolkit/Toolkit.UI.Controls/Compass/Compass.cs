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

#if !MAUI

using System.ComponentModel;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The Compass Control showing the heading on the map when the rotation is not North up / 0.
    /// </summary>
    public partial class Compass : Control
    {
        private const double DefaultSize = 30;
        private bool _headingSetByGeoView;
        private bool _isVisible;

        /// <summary>
        /// Initializes a new instance of the <see cref="Compass"/> class.
        /// </summary>
        public Compass()
            : base()
        {
            DefaultStyleKey = typeof(Compass);
        }
        /// <inheritdoc/>
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            _isVisible = false;
            UpdateCompassRotation(false);
        }

#if WINDOWS_XAML
        /// <inheritdoc />
        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            base.OnTapped(e);
            ResetRotation();
        }
#else
        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            ResetRotation();
        }
#endif

        private void UpdateCompassRotation(bool useTransitions)
        {
            double heading = Heading;
            if (double.IsNaN(heading))
            {
                heading = 0;
            }

            var transform = GetTemplateChild("RotateTransform") as RotateTransform;
            if (transform != null)
            {
                transform.Angle = -heading;
            }

            bool autoHide = AutoHide && !DesignTime.IsDesignMode;
            if (Math.Round(heading % 360) == 0 && autoHide)
            {
                if (_isVisible)
                {
                    _isVisible = false;
                    VisualStateManager.GoToState(this, "HideCompass", useTransitions);
                }
            }
            else if (!_isVisible)
            {
                _isVisible = true;
                VisualStateManager.GoToState(this, "ShowCompass", useTransitions);
            }
        }

        /// <summary>
        /// Gets or sets the Heading for the compass.
        /// </summary>
        /// <remarks>
        /// This property is read-only if the <see cref="GeoView"/> property is assigned.
        /// </remarks>
        public double Heading
        {
            get { return (double)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Heading"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register(nameof(Heading), typeof(double), typeof(Compass), new PropertyMetadata(0d, OnHeadingPropertyChanged));


        /// <summary>
        /// The property changed event that is raised when the value of Heading property changes.
        /// </summary>
        /// <param name="d">Compass.</param>
        /// <param name="e">Contains information related to the change to the Heading property.</param>
        private static void OnHeadingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = (Compass)d;
            if (compass.GeoView != null && !compass._headingSetByGeoView)
            {
                throw new InvalidOperationException("The Heading Property is read-only when the GeoView property has been assigned");
            }

            compass.UpdateCompassRotation(true);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0.
        /// </summary>
        public bool AutoHide
        {
            get { return (bool)GetValue(AutoHideProperty); }
            set { SetValue(AutoHideProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoHide"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoHideProperty =
            DependencyProperty.Register(nameof(AutoHide), typeof(bool), typeof(Compass), new PropertyMetadata(true, OnAutoHidePropertyChanged));

        private static void OnAutoHidePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = (Compass)d;
            compass.UpdateCompassRotation(false);
        }

        /// <summary>
        /// Gets or sets the GeoView property that can be attached to a Compass control to accurately set the heading, instead of
        /// setting the <see cref="Compass.Heading"/> property directly.
        /// </summary>
        public GeoView? GeoView
        {
            get { return GetValue(GeoViewProperty) as GeoView; }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> Dependency Property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(Compass.GeoView), typeof(GeoView), typeof(Compass), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = (Compass)d;
            compass.WireGeoViewPropertyChanged(e.OldValue as GeoView, e.NewValue as GeoView);
        }

        private void WireGeoViewPropertyChanged(GeoView? oldGeoView, GeoView? newGeoView)
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

        private void GeoView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var view = GeoView;
            if ((view is MapView && e.PropertyName == nameof(MapView.MapRotation)) ||
                (view is SceneView && e.PropertyName == nameof(SceneView.Camera)))
            {
                UpdateCompassFromGeoView(GeoView);
            }
        }

        private void UpdateCompassFromGeoView(GeoView? view)
        {
            _headingSetByGeoView = true;
            Heading = (view is MapView mv) ? mv.MapRotation : (view is SceneView sv ? sv.Camera.Heading : 0);
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
#endif