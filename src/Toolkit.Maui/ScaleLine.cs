using Esri.ArcGISRuntime.Maui;
using Microsoft.Maui.Handlers;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public interface IScaleLineView : IView
    {
        public MapView? MapView { get; }
    }

    public class ScaleLine : View, IScaleLineView
    {
        public ScaleLine()
        {
            HorizontalOptions = LayoutOptions.Start;
            VerticalOptions = LayoutOptions.End;
        }

        /// <summary>
        /// Gets or sets the MapView property that can be attached to a ScaleLine control to accurately set the scale, instead of
        /// setting the <see cref="ScaleLine.MapScale"/> property directly.
        /// </summary>
        public MapView? MapView
        {
            get => GetValue(MapViewProperty) as MapView;
            set => SetValue(MapViewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> Dependency Property.
        /// </summary>
        public static readonly BindableProperty MapViewProperty =
            BindableProperty.Create(nameof(MapView), typeof(MapView), typeof(ScaleLine));
    }

    public class ScaleLineHandler : ViewHandler<IScaleLineView, UI.Controls.ScaleLine>
    {
        public static readonly PropertyMapper<IScaleLineView, ScaleLineHandler> PropertyMapper = new(ViewMapper)
        {
            [nameof(IScaleLineView.MapView)] = MapView,
        };

        public ScaleLineHandler()
            : base(PropertyMapper)
        {
        }

        /// <inheritdoc />
        protected override UI.Controls.ScaleLine CreatePlatformView()
        {
#if __ANDROID__
            return new UI.Controls.ScaleLine(Context);
#else
            return new UI.Controls.ScaleLine();
#endif
        }

        private static void MapView(ScaleLineHandler handler, IScaleLineView scaleLine)
        {
            var mapView = scaleLine.MapView?.Handler?.PlatformView as ArcGISRuntime.UI.Controls.MapView;
            handler.PlatformView.MapView = mapView;
        }
    }
}