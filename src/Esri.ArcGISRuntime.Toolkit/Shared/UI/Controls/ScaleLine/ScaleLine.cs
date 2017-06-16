// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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

using System.Windows;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
#else
using System.Windows.Controls;
using System.Windows.Shapes;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The ScaleLine Control generates a line representing
    /// a certain distance on the map in both Metric and US units.
    /// </summary>
    public class ScaleLine : Control
    {
        private TextBlock _usValue;
        private TextBlock _usUnit;
        private TextBlock _metricValue;
        private TextBlock _metricUnit;
        private Rectangle _metricScaleLine;
        private Rectangle _usScaleLine;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleLine"/> class.
        /// </summary>
        public ScaleLine()
        {
            DefaultStyleKey = typeof(ScaleLine);
        }

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
        /// Gets or sets the scale that the ScaleLine will
        /// use to calculate scale in metric and imperial units.
        /// </summary>
        /// <seealso cref="SetMapView"/>
        /// <seealso cref="MapViewProperty"/>
        public double MapScale
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
        /// Gets or sets the width that will be used to
        /// calculate the length of the ScaleLine
        /// </summary>
        public double TargetWidth
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
        /// Sets the imperial units section of the scale line
        /// </summary>
        /// <param name="value">map scale in imperial units</param>
        /// <param name="unit">imperial unit</param>
        private void SetUsUnit(double value, string unit)
        {
            var roundedValue = GetRoundedValue(value);

            if (_usUnit != null)
            {
                _usUnit.Text = unit;
            }

            if (_usValue != null)
            {
                _usValue.Text = string.Format("{0}", roundedValue);
            }

            if (_usScaleLine != null)
            {
                _usScaleLine.Width = TargetWidth * roundedValue / value;
            }
        }

        /// <summary>
        /// Sets the metric units section of the scale line
        /// </summary>
        /// <param name="value">map scale in metric units</param>
        /// <param name="unit">metric unit</param>
        private void SetMetricUnit(double value, string unit)
        {
            var roundedValue = GetRoundedValue(value);

            if (_metricUnit != null)
            {
                _metricUnit.Text = unit;
            }

            if (_metricValue != null)
            {
                _metricValue.Text = string.Format("{0}", (int)roundedValue);
            }

            if (_metricScaleLine != null)
            {
                _metricScaleLine.Width = TargetWidth * roundedValue / value;
            }
        }

        private void Refresh()
        {
            if ((double.IsNaN(MapScale) || MapScale <= 0) && !DesignTime.IsDesignMode)
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            Visibility = Visibility.Visible;
            var miles = ConvertInchesTo(LinearUnits.Miles);
            SetUsUnit(
                miles >= 1 ? miles : ConvertInchesTo(LinearUnits.Feet),
                miles >= 1 ? Properties.Resources.GetString("MilesAbbreviation") : Properties.Resources.GetString("FeetAbbreviation"));

            var kilometers = ConvertInchesTo(LinearUnits.Kilometers);
            SetMetricUnit(
                kilometers >= 1 ? kilometers : ConvertInchesTo(LinearUnits.Meters),
                kilometers >= 1 ? Properties.Resources.GetString("KilometerAbbreviation") : Properties.Resources.GetString("MeterAbbreviation"));
        }

        /// <summary>
        /// 1. (target_width_pixels / 96) = target_width_inches
        /// 2. target_width_inches * map_scale = map_scale_inches
        /// 3. map_scale_inches converted to meters = map_scale_meters
        /// </summary>
        /// <returns>value that represents the maps scale in meters</returns>
        private double ConvertInchesTo(LinearUnit unit)
        {
            return LinearUnits.Inches.ConvertTo(unit, (TargetWidth / 96) * GetScale());
        }

        private double GetScale()
        {
            if (DesignTime.IsDesignMode && double.IsNaN(MapScale))
            {
                return 50000; // In design-mode we'll just return a dummy 1:50000 if the scale isn't set
            }

            return MapScale;
        }

        private static double GetRoundedValue(double value)
        {
            if (double.IsNaN(value))
            {
                return 0;
            }
            else if (value >= 1000)
            {
                return value - (value % 1000);
            }
            else if (value >= 100)
            {
                return value - (value % 100);
            }
            else if (value >= 10)
            {
                return value - (value % 10);
            }
            else if (value >= 1)
            {
                return (int)value;
            }
            else
            {
                return value;
            }
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
            var view = e.NewValue as MapView;
            var inpc = view as System.ComponentModel.INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged += (s, args) => MapView_PropertyChanged(view, args.PropertyName, d as ScaleLine);
            }
        }

        private static void MapView_PropertyChanged(MapView view, string propertyName, ScaleLine scaleLine)
        {
            if ((propertyName == nameof(MapView.VisibleArea) || propertyName == nameof(MapView.IsNavigating)) && view.IsNavigating == false)
            {
                var scale = CalculateScale(view.VisibleArea, view.UnitsPerPixel);
                scaleLine.MapScale = scale;
            }
        }

        /// <summary>
        /// Calculates the scale at the center of a polygon, at a given pixel size
        /// </summary>
        /// <remarks>
        /// A pixel is a device independent logical pixel - ie 1/96 inches.
        /// </remarks>
        /// <param name="visibleArea">The area which center the scale will be calculated for.</param>
        /// <param name="unitsPerPixel">The size of a device indepedent pixel in the units of the spatial reference</param>
        /// <returns>The MapScale for the center of the view</returns>
        public static double CalculateScale(Esri.ArcGISRuntime.Geometry.Polygon visibleArea, double unitsPerPixel)
        {
            if (visibleArea == null)
            {
                return double.NaN;
            }

            if (visibleArea.SpatialReference == null)
            {
                return double.NaN;
            }

            if (double.IsNaN(unitsPerPixel) || unitsPerPixel <= 0)
            {
                return double.NaN;
            }

            var center = visibleArea.Extent.GetCenter();
            var centerOnePixelOver = new Geometry.MapPoint(center.X + unitsPerPixel, center.Y, center.SpatialReference);

            // Calculate the geodedetic distance between two points one 'pixel' apart
            var result = Geometry.GeometryEngine.DistanceGeodetic(center, centerOnePixelOver, Geometry.LinearUnits.Inches, Geometry.AngularUnits.Degrees, Geometry.GeodeticCurveType.Geodesic);
            double distanceInInches = result.Distance;
            return distanceInInches * 96;
        }
    }
}
