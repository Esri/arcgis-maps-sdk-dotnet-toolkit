using Esri.ArcGISRuntime.Maui;
using Microsoft.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public interface ICompassView : IView
    {
        public bool AutoHide { get; set; }

        public GeoView? GeoView { get; set; }

        public double Heading { get; set; }
    }

    public class Compass : View, ICompassView
    {
        public Compass()
        {
            HorizontalOptions = LayoutOptions.End;
            VerticalOptions = LayoutOptions.Start;
            WidthRequest = 30;
            HeightRequest = 30;
        }

        /// <summary>
        /// Identifies the <see cref="Heading"/> bindable property.
        /// </summary>
        public static readonly BindableProperty HeadingProperty =
            BindableProperty.Create(nameof(Heading), typeof(double), typeof(Compass), 0d);

        /// <summary>
        /// Gets or sets the Heading for the compass.
        /// </summary>
        public double Heading
        {
            get => (double)GetValue(HeadingProperty);
            set => SetValue(HeadingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoHide"/> bindable property.
        /// </summary>
        public static readonly BindableProperty AutoHideProperty =
            BindableProperty.Create(nameof(AutoHide), typeof(bool), typeof(Compass), true);

        /// <summary>
        /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0.
        /// </summary>
        public bool AutoHide
        {
            get => (bool)GetValue(AutoHideProperty);
            set => SetValue(AutoHideProperty, value);
        }

        /// <summary>
        /// Gets or sets the GeoView property that can be attached to a Compass control to accurately set the heading, instead of
        /// setting the <see cref="Compass.Heading"/> property directly.
        /// </summary>
        public GeoView? GeoView
        {
            get => GetValue(GeoViewProperty) as GeoView;
            set => SetValue(GeoViewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> Dependency Property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(Compass));
    }

    public class CompassHandler : ViewHandler<ICompassView, UI.Controls.Compass>
    {
        public static readonly PropertyMapper<ICompassView, CompassHandler> PropertyMapper = new(ViewMapper)
        {
            [nameof(ICompassView.AutoHide)] = MapAutoHide,
            [nameof(ICompassView.GeoView)] = MapGeoView,
            [nameof(ICompassView.Heading)] = MapHeading,
        };

        public CompassHandler()
            : base(PropertyMapper)
        {
        }

        /// <inheritdoc />
        protected override UI.Controls.Compass CreatePlatformView()
        {
#if __ANDROID__
            return new UI.Controls.Compass(Context);
#else
            return new UI.Controls.Compass();
#endif
        }

        private static void MapAutoHide(CompassHandler handler, ICompassView compass)
        {
            handler.PlatformView.AutoHide = compass.AutoHide;
        }

        private static void MapGeoView(CompassHandler handler, ICompassView compass)
        {
            var geoView = compass.GeoView?.Handler?.PlatformView as ArcGISRuntime.UI.Controls.GeoView;
            handler.PlatformView.GeoView = geoView;
        }

        private static void MapHeading(CompassHandler handler, ICompassView compass)
        {
            handler.PlatformView.Heading = compass.Heading;
        }
    }
}