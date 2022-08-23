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


using Esri.ArcGISRuntime.Geometry;
using Microsoft.Maui.Controls.Shapes;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    /// <summary>
    /// The ScaleLine Control generates a line representing
    /// a certain distance on the map in both Metric and US units.
    /// </summary>
    public class ScaleLine : TemplatedView
    {
        private Label? _usValue;
        private Label? _usUnit;
        private Label? _metricValue;
        private Label? _metricUnit;
        private Rectangle? _metricScaleLine;
        private Rectangle? _usScaleLine;
        private bool _scaleSetByMapView;
        private static readonly ControlTemplate DefaultControlTemplate;

        static ScaleLine()
        {
            string template = @"<Grid xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" x:Name=""LayoutRoot"" WidthRequest=""{TemplateBinding Width}"">
            <Grid HorizontalOptions=""Fill"">
                <Grid.RowDefinitions>
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                </Grid.RowDefinitions>
                <StackLayout Orientation=""Horizontal"" Grid.Row=""0"">
                    <Border Background=""Transparent"" WidthRequest=""{Binding Width, Source={ x:Reference MetricScaleLine}}"" StrokeThickness=""0""/>
                    <Label x:Name=""MetricValue"" Text=""100"" TextColor=""{TemplateBinding Color}""/>
                    <Label x:Name=""MetricUnit"" Text=""m"" TextColor=""{TemplateBinding Color}""/>
                </StackLayout>
                <StackLayout Orientation=""Horizontal"" Grid.Row=""1"">
                    <Rectangle WidthRequest=""2"" BackgroundColor=""{TemplateBinding Color}"" HeightRequest=""5"" StrokeThickness=""0""/>
                    <Border Background=""Transparent"" WidthRequest=""{Binding Width, Source={ x:Reference MetricScaleLine}}"" StrokeThickness=""0""/>
                    <Rectangle WidthRequest=""2"" BackgroundColor=""{TemplateBinding Color}"" HeightRequest=""5"" StrokeThickness=""0""/>
                </StackLayout>
                <StackLayout Orientation=""Horizontal"" Grid.Row=""3"">
                    <Rectangle WidthRequest=""2"" BackgroundColor=""{TemplateBinding Color}"" HeightRequest=""5"" StrokeThickness=""0""/>
                    <Border Background=""Transparent"" WidthRequest=""{Binding Width, Source={ x:Reference UsScaleLine}}"" StrokeThickness=""0""/>
                    <Rectangle WidthRequest=""2"" BackgroundColor=""{TemplateBinding Color}"" HeightRequest=""5"" StrokeThickness=""0""/>
                </StackLayout>
                <StackLayout Orientation=""Horizontal"" Grid.Row=""4"">
                    <Border Background=""Transparent"" WidthRequest=""{Binding Width, Source={ x:Reference UsScaleLine}}"" StrokeThickness=""0""/>
                    <Label x:Name=""UsValue"" Text=""USValue"" TextColor=""{TemplateBinding Color}""/>
                    <Label x:Name=""UsUnit"" Text=""UsUnit"" TextColor=""{TemplateBinding Color}""/>
                </StackLayout>
                <StackLayout Orientation=""Horizontal"" Grid.Row=""2"">
                    <Rectangle Background=""{TemplateBinding Color}"" HeightRequest=""2"" WidthRequest=""4"" StrokeThickness=""0""/>
                    <Grid>
                        <Rectangle Background=""{TemplateBinding Color}"" HeightRequest=""2"" HorizontalOptions=""Start"" WidthRequest=""200"" x:Name=""MetricScaleLine"" StrokeThickness=""0""/>
                        <Rectangle Background=""{TemplateBinding Color}"" HeightRequest=""2"" HorizontalOptions=""Start"" WidthRequest=""200"" x:Name=""UsScaleLine"" StrokeThickness=""0""/>
                    </Grid>
                </StackLayout>
            </Grid>
        </Grid>";
            DefaultControlTemplate = new ControlTemplate()
            {
                LoadTemplate = () =>
                {
                    return Microsoft.Maui.Controls.Xaml.Extensions.LoadFromXaml(new Grid(), template);
                }
            };
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleLine"/> class.
        /// </summary>
        public ScaleLine()
            : base()
        {
            HorizontalOptions = LayoutOptions.Start;
            VerticalOptions = LayoutOptions.End;
            ControlTemplate = DefaultControlTemplate;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Get a reference to the templated parts
            _usUnit = GetTemplateChild("UsUnit") as Label;
            _usValue = GetTemplateChild("UsValue") as Label;
            _metricUnit = GetTemplateChild("MetricUnit") as Label;
            _metricValue = GetTemplateChild("MetricValue") as Label;
            _usScaleLine = GetTemplateChild("UsScaleLine") as Rectangle;
            _metricScaleLine = GetTemplateChild("MetricScaleLine") as Rectangle;
            Refresh();
        }

        /// <summary>
        /// Identifies the <see cref="MapScale"/> bindable property.
        /// </summary>
        public static readonly BindableProperty MapScaleProperty =
            BindableProperty.Create(nameof(MapScale), typeof(double), typeof(ScaleLine), double.NaN, BindingMode.OneWay, null, OnMapScaleChanged);

        /// <summary>
        /// Gets or sets the scale that the ScaleLine will
        /// use to calculate scale in metric and imperial units.
        /// </summary>
        /// <seealso cref="MapView"/>
        public double MapScale
        {
            get { return (double)GetValue(MapScaleProperty); }
            set { SetValue(MapScaleProperty, value); }
        }

        private static void OnMapScaleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var scaleLine = (ScaleLine)bindable;
            if (scaleLine.MapView != null && !scaleLine._scaleSetByMapView)
            {
                throw new System.InvalidOperationException("The MapScale Property is read-only when the MapView property has been assigned");
            }
            scaleLine.Refresh();
        }

        /// <summary>
        /// Gets or sets the width that will be used to
        /// calculate the length of the ScaleLine.
        /// </summary>
        public double TargetWidth
        {
            get { return (double)GetValue(TargetWidthProperty); }
            set { SetValue(TargetWidthProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TargetWidth"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TargetWidthProperty =
            BindableProperty.Create(nameof(TargetWidth), typeof(double), typeof(ScaleLine), 200d, BindingMode.OneWay, null, OnTargetWidthChanged);

        private static void OnTargetWidthChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var scaleLine = (ScaleLine)bindable;
            scaleLine.Refresh();
        }

        /// <summary>
        /// Gets or sets the Color that will be used for the scale line parts.
        /// </summary>
        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Color"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ColorProperty =
            BindableProperty.Create(nameof(Color), typeof(Color), typeof(ScaleLine), Colors.Black, BindingMode.OneWay);

        /// <summary>
        /// Gets or sets the MapView property that can be attached to a ScaleLine control to accurately set the scale, instead of
        /// setting the <see cref="ScaleLine.MapScale"/> property directly.
        /// </summary>
        public MapView? MapView
        {
            get { return GetValue(MapViewProperty) as MapView; }
            set { SetValue(MapViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> Dependency Property.
        /// </summary>
        public static readonly BindableProperty MapViewProperty =
            BindableProperty.Create(nameof(ScaleLine.MapView), typeof(MapView), typeof(ScaleLine), null, BindingMode.OneWay, null, OnMapViewPropertyChanged);

        private static void OnMapViewPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
        {
            var scaleLine = (ScaleLine)bindable;
            var inpc = oldValue as INotifyPropertyChanged;
            //TODO: Make weak
            if (inpc != null)
            {
                inpc.PropertyChanged -= scaleLine.MapView_PropertyChanged;
            }

            inpc = newValue as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged += scaleLine.MapView_PropertyChanged;
            }

            scaleLine.UpdateScalelineFromMapView(newValue as MapView);
        }

        /// <summary>
        /// Sets the imperial units section of the scale line.
        /// </summary>
        /// <param name="value">map scale in imperial units.</param>
        /// <param name="unit">imperial unit.</param>
        private void SetUsUnit(double value, string? unit)
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
                _usScaleLine.WidthRequest = TargetWidth * roundedValue / value;
            }
        }

        /// <summary>
        /// Sets the metric units section of the scale line.
        /// </summary>
        /// <param name="value">map scale in metric units.</param>
        /// <param name="unit">metric unit.</param>
        private void SetMetricUnit(double value, string? unit)
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
                _metricScaleLine.WidthRequest = TargetWidth * roundedValue / value;
            }
        }

        private void Refresh()
        {
            if ((double.IsNaN(MapScale) || MapScale <= 0) && !DesignTime.IsDesignMode)
            {
                IsVisible = false;
                return;
            }
            IsVisible = true;
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

        private void WireMapViewPropertyChanged(MapView? oldMapView, MapView? newMapView)
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

        private void MapView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView view && (e.PropertyName == nameof(MapView.VisibleArea) || e.PropertyName == nameof(MapView.IsNavigating)) && !view.IsNavigating)
            {
                UpdateScalelineFromMapView(view);
            }
        }

        private void UpdateScalelineFromMapView(MapView? view)
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
        public static double CalculateScale(Esri.ArcGISRuntime.Geometry.Polygon? visibleArea, double unitsPerPixel)
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
            double distanceInInches = result!.Distance;
            return distanceInInches * 96;
        }
    }
}