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

using Esri.ArcGISRuntime.Geometry;
using System.Windows;
#if NETFX_CORE
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
#else
using System.Windows.Controls;
using System.Windows.Shapes;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI
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
            return LinearUnits.Inches.ConvertTo(unit, (TargetWidth / 96) * MapScale);
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
    }
}
