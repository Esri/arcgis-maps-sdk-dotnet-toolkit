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

#if !XAMARIN
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class ScaleLine
    {
        private void Initialize() => DefaultStyleKey = typeof(ScaleLine);

        /// <inheritdoc/>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            // Get a reference to the templated parts
            _usUnit = GetTemplateChild("UsUnit") as TextBlock;
            _usValue = GetTemplateChild("UsValue") as TextBlock;
            _metricUnit = GetTemplateChild("MetricUnit") as TextBlock;
            _metricValue = GetTemplateChild("MetricValue") as TextBlock;
            _usScaleLine = GetTemplateChild("UsScaleLine") as Rectangle;
            _metricScaleLine = GetTemplateChild("MetricScaleLine") as Rectangle;
            Refresh();
        }

        /// <summary>
        /// Gets or sets the platform-specific implementation of the <see cref="MapScale"/> property
        /// </summary>
        private double MapScaleImpl
        {
            get { return (double)GetValue(MapScaleProperty); }
            set { SetValue(MapScaleProperty, value); }
        }

        /// <summary>
        /// The dependency property for the Scale property.
        /// </summary>
        public static readonly DependencyProperty MapScaleProperty =
           DependencyProperty.Register(nameof(MapScale), typeof(double), typeof(ScaleLine), new PropertyMetadata(default(double), OnMapScalePropertyChanged));

        /// <summary>
        /// The property changed event that is raised when
        /// the value of Scale property changes.
        /// </summary>
        /// <param name="d">ScaleLine</param>
        /// <param name="e">Contains information related to the change to the Scale property.</param>
        private static void OnMapScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scaleLine = (ScaleLine)d;
            scaleLine.Refresh();
        }

        /// <summary>
        /// Gets or sets the platform-specific implementation of the <see cref="TargetWidth"/> property
        /// </summary>
        private double TargetWidthImpl
        {
            get { return (double)GetValue(TargetWidthProperty); }
            set { SetValue(TargetWidthProperty, value); }
        }

        /// <summary>
        /// Identifies the dependency property for the <see cref="TargetWidth"/> property.
        /// </summary>
        public static readonly DependencyProperty TargetWidthProperty =
            DependencyProperty.Register(nameof(TargetWidth), typeof(double), typeof(ScaleLine), new PropertyMetadata(default(double), OnTargetWidthPropertyChanged));

        /// <summary>
        /// The property changed handler that is called when
        /// the value of TargetWidth property changes.
        /// </summary>
        /// <param name="d">ScaleLine</param>
        /// <param name="e">Contains information related to the change to the TargetWidth property.</param>
        private static void OnTargetWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scaleLine = (ScaleLine)d;
            scaleLine.Refresh();
        }

        /// <summary>
        /// Gets the MapView attached property that can be attached to a ScaleLine control to accurately set the scale, instead of
        /// setting the <see cref="ScaleLine.MapScale"/> property directly.
        /// </summary>
        /// <param name="scaleLine">The scaleline control this would be attached to</param>
        /// <returns>The MapView the scaleline is associated with.</returns>
        public static MapView GetMapView(DependencyObject scaleLine)
        {
            return (MapView)scaleLine.GetValue(MapViewProperty);
        }

        /// <summary>
        /// Sets the MapView attached property that can be attached to a ScaleLine control to accurately set the scale, instead of
        /// setting the <see cref="ScaleLine.MapScale"/> property directly.
        /// </summary>
        /// <param name="scaleLine">The scaleline control this would be attached to</param>
        /// <param name="mapView">The mapview to calculate the scale for</param>
        public static void SetMapView(DependencyObject scaleLine, MapView mapView)
        {
            scaleLine.SetValue(MapViewProperty, mapView);
        }

        /// <summary>
        /// Identifies the MapView Dependency Property
        /// </summary>
        public static readonly DependencyProperty MapViewProperty =
            DependencyProperty.RegisterAttached("MapView", typeof(MapView), typeof(ScaleLine), new PropertyMetadata(null, OnMapViewPropertyChanged));

        private static void OnMapViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scaleLine = (ScaleLine)d;
            scaleLine.WireMapViewPropertyChanged(e.OldValue as MapView, e.NewValue as MapView);
        }

        private void SetVisibility(bool isVisible)
        {
            Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
#endif