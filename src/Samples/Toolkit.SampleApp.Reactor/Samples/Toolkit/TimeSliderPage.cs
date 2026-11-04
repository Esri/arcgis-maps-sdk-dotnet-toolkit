using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Reactor.Hooks;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;
using TimeSliderControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class TimeSliderPage : Component
{
    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var layer = UseMemo(() => new FeatureLayer(new Uri("https://services9.arcgis.com/RHVPKKiFTONKtxq3/arcgis/rest/services/Historical_Quakes/FeatureServer/0")));
        var map = UseMemo(() =>
        {
            var result = new Map(BasemapStyle.ArcGISLightGray);
            result.OperationalLayers.Add(layer);
            return result;
        });

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star(), GridSize.Auto],
            MapView(map)
                .Ref(mapViewRef),

            (TimeSlider() with
            {
                OnCurrentExtentChanged = args =>
                {
                    if (mapViewRef.Current is not null)
                    {
                        mapViewRef.Current.TimeExtent = args.NewExtent;
                    }
                },
            })
            .OnMount(async element =>
            {
                var slider = (TimeSliderControl)element;
                await slider.InitializeTimePropertiesAsync(layer);
                if (mapViewRef.Current is not null)
                {
                    mapViewRef.Current.TimeExtent = slider.CurrentExtent;
                }
            })
            .Margin(20)
            .Grid(row: 1)
        );
    }
}
