using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FeatureDataFieldControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.FeatureDataField;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;
using Microsoft.UI.Reactor.Hooks;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class FeatureDataFieldPage : Component
{
    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var (selectedFeature, setSelectedFeature) = UseState<ArcGISFeature?>(null);
        var map = UseMemo(() =>
        {
            var result = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
            result.OperationalLayers.Add(new FeatureLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0")));
            return result;
        });

        return Grid(columns: [GridSize.Star(), GridSize.Px(320)], rows: [GridSize.Star()],
            MapView(
                map,
                async args =>
                {
                    if (mapViewRef.Current is null)
                    {
                        return;
                    }

                    var results = await mapViewRef.Current.IdentifyLayerAsync(mapViewRef.Current.Map!.OperationalLayers[0], args.Position, 3, false, 1);
                    setSelectedFeature(results.GeoElements.FirstOrDefault() as ArcGISFeature);
                })
                .Ref(mapViewRef),

            Border(
                selectedFeature is null
                    ? Caption("Tap a feature to edit its attributes.")
                    : VStack(
                        12,
                        Caption("Damage Type"),
                        FeatureDataField(selectedFeature, "typdamage"),
                        Caption("Occupants"),
                        FeatureDataField(selectedFeature, "numoccup"),
                        Caption("Description"),
                        FeatureDataField(selectedFeature, "descdamage") with
                        {
                            OnValueChanging = args =>
                            {
                                if (args.NewValue as string == "TEST")
                                {
                                    throw new ArgumentException("Custom validation: the value 'TEST' is not allowed.");
                                }
                            },
                        }))
            .Padding(16)
            .Background(Theme.CardBackground)
            .WithBorder(Theme.CardStroke)
            .CornerRadius(12)
            .Margin(12)
            .Grid(column: 1)
        );
    }
}
