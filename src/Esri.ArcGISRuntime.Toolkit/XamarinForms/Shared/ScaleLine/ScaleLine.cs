using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    public class ScaleLine : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleLine"/> class
        /// </summary>
        public ScaleLine() : this(new UI.Controls.ScaleLine()) { }

        internal ScaleLine(UI.Controls.ScaleLine nativeScaleLine)
        {
            NativeScaleLine = nativeScaleLine;

#if NETFX_CORE
            nativeScaleLine.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal readonly UI.Controls.ScaleLine NativeScaleLine;

        /// <summary>
        /// Identifies the <see cref="TargetWidth"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TargetWidthProperty =
            BindableProperty.Create(nameof(TargetWidth), typeof(double), typeof(ScaleLine), double.NaN, BindingMode.OneWay, null, OnTargetWidthChanged);

        /// <summary>
        /// Gets or sets the width that will be used to calculate the length of the ScaleLine
        /// </summary>
        public double TargetWidth
        {
            get { return (double)GetValue(TargetWidthProperty); }
            set { SetValue(TargetWidthProperty, value); }
        }

        private static void OnTargetWidthChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var scaleline = (ScaleLine)bindable;
                scaleline.NativeScaleLine.TargetWidth = (double)newValue;
                scaleline.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="MapView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty MapViewProperty =
            BindableProperty.CreateAttached(nameof(MapView), typeof(MapView), typeof(ScaleLine), null, BindingMode.OneWay, null, OnMapViewChanged);

        /// <summary>
        /// Sets the MapView attached property that can be attached to a ScaleLine control to accurately set the scale, instead of
        /// setting the <see cref="ScaleLine.MapScale"/> property directly.
        /// </summary>
        /// <param name="scaleLine">The scaleline control this would be attached to</param>
        /// <param name="mapView">The mapview to calculate the scale for</param>
        public static void SetMapView(BindableObject scaleline, MapView mapView)
        {
            scaleline.SetValue(MapViewProperty, mapView);
        }

        /// <summary>
        /// Gets the MapView attached property that can be attached to a ScaleLine control to accurately set the scale, instead of
        /// setting the <see cref="ScaleLine.MapScale"/> property directly.
        /// </summary>
        /// <param name="scaleLine">The scaleline control this would be attached to</param>
        /// <returns>The MapView the scaleline is associated with.</returns>
        public static MapView GetMapView(BindableObject scaleline)
        {
            return scaleline.GetValue(MapViewProperty) as MapView;
        }

        private static void OnMapViewChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var scaleline = (ScaleLine)bindable;
            var inpc = oldValue as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged -= scaleline.MapView_PropertyChanged;
            }

            inpc = newValue as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged += scaleline.MapView_PropertyChanged;
            }
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
        /// <seealso cref="SetMapView"/>
        /// <seealso cref="MapViewProperty"/>
        public double MapScale
        {
            get { return (double)GetValue(MapScaleProperty); }
            set { SetValue(MapScaleProperty, value); }
        }

        private static void OnMapScaleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var scaleline = (ScaleLine)bindable;
                scaleline.NativeScaleLine.MapScale = (double)newValue;
                scaleline.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="Foreground"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ForegroundProperty =
            BindableProperty.Create(nameof(Foreground), typeof(Color), typeof(ScaleLine), Color.Black, BindingMode.OneWay, null, OnForegroundChanged);

        /// <summary>
        /// Gets or sets the foreground color.
        /// </summary>
        public Color Foreground
        {
            get { return (Color)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        private static void OnForegroundChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var nativeView = ((ScaleLine)bindable).NativeScaleLine;
            if (newValue != null)
                nativeView.SetForeground(((Color)newValue).ToNativeColor());
        }

        private void MapView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(MapView.VisibleArea) || e.PropertyName == nameof(MapView.IsNavigating)))
            {
                var mapView = GetMapView(this);
                if (mapView.IsNavigating)
                    return;
                MapScale = CalculateScale(mapView.VisibleArea, mapView.UnitsPerPixel);
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
