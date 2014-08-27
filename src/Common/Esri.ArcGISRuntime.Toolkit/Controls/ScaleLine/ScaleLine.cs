// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Geometry;
using System;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
#else
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// The ScaleLine Control generates a line representing 
    /// a certain distance on the map in both Metric and US units.
    /// </summary>
    [TemplatePart(Name = "UsValue", Type = typeof(TextBlock))]
    [TemplatePart(Name = "UsUnit", Type = typeof(TextBlock))]
    [TemplatePart(Name = "MetricValue", Type = typeof(TextBlock))]
    [TemplatePart(Name = "MetricUnit", Type = typeof(TextBlock))]
    [TemplatePart(Name = "MetricScaleLine", Type = typeof(Rectangle))]
    [TemplatePart(Name = "UsScaleLine", Type = typeof(Rectangle))]
    public class ScaleLine : Control
    {
        private TextBlock _usValue;
        private TextBlock _usUnit;
        private TextBlock _metricValue;
        private TextBlock _metricUnit;
        private Rectangle _metricScaleLine;
        private Rectangle _usScaleLine;
        
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleLine"/> class.
        /// </summary>
        public ScaleLine()
        {
#if NETFX_CORE
            DefaultStyleKey = typeof(ScaleLine);
#endif
        }

#if !NETFX_CORE
        static ScaleLine()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScaleLine),
                new FrameworkPropertyMetadata(typeof(ScaleLine)));
        }
#endif
        #endregion Constructor

        #region OnApplyTemplate

        /// <summary>
        /// Invoked whenever application code or internal processes 
        /// (such as a rebuilding layout pass) call ApplyTemplate. 
        /// In simplest terms, this means the method is called just 
        /// before a UI element displays in your app. Override this 
        /// method to influence the default post-template logic of 
        /// a class.
        /// </summary>
#if NETFX_CORE
        protected
#else
        public
#endif
 override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            // Get a reference to the templated parts
            _usUnit = GetTemplateChild("UsUnit") as TextBlock;
            _usValue = GetTemplateChild("UsValue") as TextBlock;
            _metricUnit = GetTemplateChild("MetricUnit") as TextBlock;
            _metricValue = GetTemplateChild("MetricValue") as TextBlock;
            _usScaleLine = GetTemplateChild("UsScaleLine") as Rectangle;
            _metricScaleLine = GetTemplateChild("MetricScaleLine") as Rectangle;

            Refresh(); // update the layout
        }       

        #endregion

        #region Public Properties

        #region Scale

        /// <summary>
        /// Gets or sets the scale that the ScaleLine will 
        /// use to calculate scale in metric and imperial units.
        /// </summary>        
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        /// <summary>
        /// The dependency property for the Scale property.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
           DependencyProperty.Register("Scale", typeof (double), typeof (ScaleLine), new PropertyMetadata(default(double),OnScalePropertyChanged));

        /// <summary>
        /// The property changed event that is raised when 
        /// the value of Scale property changes.
        /// </summary>
        /// <param name="d">ScaleLine</param>
        /// <param name="e">Contains information related to the change to the Scale property.</param>
        private static void OnScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scaleLine = (ScaleLine)d;
            scaleLine.Refresh();
        }        

        #endregion Scale

        #region TargetWidth

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
        /// The dependency property for the TargetWidth property.
        /// </summary>
        public static readonly DependencyProperty TargetWidthProperty =
            DependencyProperty.Register("TargetWidth", typeof (double), typeof (ScaleLine), new PropertyMetadata(default(double),OnTargetWidthPropertyChanged));

        /// <summary>
        /// The property changed event that is raised when 
        /// the value of TargetWidth property changes.
        /// </summary>
        /// <param name="d">ScaleLine</param>
        /// <param name="e">Contains information related to the change to the TargetWidth property.</param>
        private static void OnTargetWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scaleLine = (ScaleLine)d;
            scaleLine.Refresh();
        }
        
        #endregion TargetWidth

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        /// Refreshes the scaleline when the map extent changes.
        /// </summary>
        private void Refresh()
        {
            var miles = GetMile();
            SetUsUnit(
                miles >= 1 ? miles : GetFoot(), 
                miles >= 1 ? "mi" : "ft");

            var kilometers = GetKilometer();
            SetMetricUnit(
                kilometers >= 1 ? kilometers : GetMeter(),
                kilometers >= 1 ? "km" : "m");                       
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
                _usUnit.Text = unit;
            if(_usValue != null)
                _usValue.Text = string.Format("{0}", roundedValue);
            if (_usScaleLine != null)
                _usScaleLine.Width = TargetWidth * roundedValue / value;

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
                _metricUnit.Text = unit;
            if (_metricValue != null)
                _metricValue.Text = string.Format("{0}", (int)roundedValue);
            if (_metricScaleLine != null)
                _metricScaleLine.Width = TargetWidth * roundedValue / value;
        }       

        /// <summary>
        /// 1. (target_width_pixels / 96) = target_width_inches 
        /// 2. target_width_inches * map_scale = map_scale_inches
        /// 3. map_scale_inches converted to kilometers = map_scale_kilometers
        /// </summary>
        /// <returns>value that represents the maps scale in kilometers</returns>        
        private double GetKilometer()
        {
            return LinearUnits.Inches.ConvertTo(((TargetWidth) / 96) * Scale, LinearUnits.Kilometers);            
        }

        /// <summary>
        /// 1. (target_width_pixels / 96) = target_width_inches 
        /// 2. target_width_inches * map_scale = map_scale_inches
        /// 3. map_scale_inches converted to meters = map_scale_meters
        /// </summary>
        /// <returns>value that represents the maps scale in meters</returns>        
        private double GetMeter()
        {
            return LinearUnits.Inches.ConvertTo(((TargetWidth) / 96) * Scale, LinearUnits.Meters);            
        }

        /// <summary>
        /// 1. (target_width_pixels / 96) = target_width_inches 
        /// 2. target_width_inches * map_scale = map_scale_inches
        /// 3. map_scale_inches converted to miles = map_scale_miles
        /// </summary>
        /// <returns>value that represents the maps scale in miles</returns>        
        private double GetMile()
        {
            return LinearUnits.Inches.ConvertTo(((TargetWidth) / 96) * Scale, LinearUnits.Miles);            
        }

        /// <summary>
        /// 1. (target_width_pixels / 96) = target_width_inches 
        /// 2. target_width_inches * map_scale = map_scale_inches
        /// 3. map_scale_inches converted to feet = map_scale_feet
        /// </summary>
        /// <returns>value that represents the maps scale in feet</returns>        
        private double GetFoot()
        {
            return LinearUnits.Inches.ConvertTo(((TargetWidth) / 96) * Scale, LinearUnits.Feet);            
        }

        private static double GetRoundedValue(double value)
        {
            if (double.IsNaN(value)) return 0;
            if (value >= 1000)
                return value - (value % 1000);           
            if (value >= 100)
                return value - (value % 100);           
            if (value >= 10)
                return value - (value % 10);
            if (value >= 1)
                return (int)value;
            return value;
        }

        #endregion Private Methods

    }
}
