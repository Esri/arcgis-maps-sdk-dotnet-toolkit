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
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
#elif __IOS__
using Control = UIKit.UIView;
using Rectangle = Esri.ArcGISRuntime.Toolkit.UI.RectangleView;
using TextBlock = UIKit.UILabel;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
using Rectangle = Esri.ArcGISRuntime.Toolkit.UI.RectangleView;
using TextBlock = Android.Widget.TextView;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The ScaleLine Control generates a line representing
    /// a certain distance on the map in both Metric and US units.
    /// </summary>
    public partial class ScaleLine : Control
    {
        private TextBlock _usValue;
        private TextBlock _usUnit;
        private TextBlock _metricValue;
        private TextBlock _metricUnit;
        private Rectangle _metricScaleLine;
        private Rectangle _usScaleLine;
        private bool _scaleSetByMapView;

#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleLine"/> class.
        /// </summary>
        public ScaleLine()
            : base()
        {
            Initialize();
        }
#endif

#pragma warning disable CS1587 // XML comment is not placed on a valid language element
        /// <summary>
        /// Gets or sets the scale that the ScaleLine will
        /// use to calculate scale in metric and imperial units.
        /// </summary>
        /// <seealso cref="MapView"/>
#if !XAMARIN
        /// <seealso cref="MapViewProperty"/>
#endif
#pragma warning restore CS1587 // XML comment is not placed on a valid language element
        public double MapScale
        {
            get { return MapScaleImpl; }
            set { MapScaleImpl = value; }
        }

        /// <summary>
        /// Gets or sets the width that will be used to
        /// calculate the length of the ScaleLine.
        /// </summary>
        public double TargetWidth
        {
            get { return TargetWidthImpl; }
            set { TargetWidthImpl = value; }
        }

        /// <summary>
        /// Sets the imperial units section of the scale line.
        /// </summary>
        /// <param name="value">map scale in imperial units.</param>
        /// <param name="unit">imperial unit.</param>
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
        /// Sets the metric units section of the scale line.
        /// </summary>
        /// <param name="value">map scale in metric units.</param>
        /// <param name="unit">metric unit.</param>
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
                SetVisibility(isVisible: false);
                return;
            }

            SetVisibility(isVisible: true);
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
        /// 3. map_scale_inches converted to meters = map_scale_meters.
        /// </summary>
        /// <returns>value that represents the maps scale in meters.</returns>
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

        private void WireMapViewPropertyChanged(MapView oldMapView, MapView newMapView)
        {
            if (oldMapView is INotifyPropertyChanged inpc1)
            {
                inpc1.PropertyChanged -= MapView_PropertyChanged;
            }

            if (newMapView is INotifyPropertyChanged inpc2)
            {
                inpc2.PropertyChanged += MapView_PropertyChanged;
            }
        }

        private void MapView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView view && (e.PropertyName == nameof(MapView.VisibleArea) || e.PropertyName == nameof(MapView.IsNavigating)) && !view.IsNavigating)
            {
                UpdateScalelineFromMapView(view);
            }
        }

        private void UpdateScalelineFromMapView(MapView view)
        {
            _scaleSetByMapView = true;
            if (view == null)
            {
                MapScale = 0d;
            }
            else
            {
                MapScale = CalculateScale(view.VisibleArea, view.UnitsPerPixel);
            }

            _scaleSetByMapView = false;
        }

        /// <summary>
        /// Calculates the scale at the center of a polygon, at a given pixel size.
        /// </summary>
        /// <remarks>
        /// A pixel is a device independent logical pixel - ie 1/96 inches.
        /// </remarks>
        /// <param name="visibleArea">The area which center the scale will be calculated for.</param>
        /// <param name="unitsPerPixel">The size of a device indepedent pixel in the units of the spatial reference.</param>
        /// <returns>The MapScale for the center of the view.</returns>
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
#if __ANDROID__
            // Need to convert the value to DIPs
            unitsPerPixel /= Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, 1, Internal.ViewExtensions.GetDisplayMetrics());
#endif

            var center = visibleArea.Extent.GetCenter();
            var centerOnePixelOver = new Geometry.MapPoint(center.X + unitsPerPixel, center.Y, center.SpatialReference);

            // Calculate the geodedetic distance between two points one 'pixel' apart
            var result = Geometry.GeometryEngine.DistanceGeodetic(center, centerOnePixelOver, Geometry.LinearUnits.Inches, Geometry.AngularUnits.Degrees, Geometry.GeodeticCurveType.Geodesic);
            double distanceInInches = result.Distance;
            return distanceInInches * 96;
        }
    }
}