using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor.Hooks;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class FeatureFormViewPage : Component
{
    private readonly Map map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=f72207ac170a40d8992b7a3507b44fad"));

    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var (featureForm, setFeatureForm) = UseState<FeatureForm?>(null);

        return Grid(columns: [GridSize.Star(), GridSize.Px(360)], rows: [GridSize.Star()],
            MapView(
                map,
                async args =>
                {
                    if (mapViewRef.Current is null)
                    {
                        return;
                    }

                    var result = await mapViewRef.Current.IdentifyLayersAsync(args.Position, 3, false);
                    setFeatureForm(GetFeatureForm(result));
                })
                .Ref(mapViewRef),

            Border(
                featureForm is null
                    ? Caption("Tap a feature with a form definition to open the feature form.")
                    : FeatureFormView(featureForm))
            .Padding(16)
            .Background(Theme.CardBackground)
            .WithBorder(Theme.CardStroke)
            .CornerRadius(12)
            .Margin(12)
            .Grid(column: 1)
        );
    }

    private static FeatureForm? GetFeatureForm(IEnumerable<IdentifyLayerResult> results)
    {
        foreach (var result in results.Where(r => r.LayerContent is FeatureLayer layer && (layer.FeatureFormDefinition is not null || (layer.FeatureTable as ArcGISFeatureTable)?.FeatureFormDefinition is not null)))
        {
            if (result.GeoElements?.OfType<ArcGISFeature>().FirstOrDefault() is ArcGISFeature feature)
            {
                return new FeatureForm(feature);
            }
        }

        return null;
    }
}
