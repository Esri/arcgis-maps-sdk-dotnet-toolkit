using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Reactor.Hooks;
using Windows.Foundation;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class IdentifyCalloutPage : Component
{
    private Map map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad"));

    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapView>();
        var noFeaturesCallout = UseMemo(CreateNoFeaturesCallout);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(
                map,
                async args =>
                {
                    if (mapViewRef.Current is null || args.Location is null)
                    {
                        return;
                    }

                    var identifyResults = await mapViewRef.Current.IdentifyLayersAsync(args.Position, 3, false);
                    var identifiedFeature = identifyResults.FirstOrDefault()?.GeoElements.FirstOrDefault();

                    if (identifiedFeature is not null)
                    {
                        mapViewRef.Current.ShowCalloutForGeoElement(
                            identifiedFeature,
                            args.Position,
                            new CalloutDefinition(identifiedFeature));
                    }
                    else
                    {
                        mapViewRef.Current.ShowCalloutAt(args.Location, noFeaturesCallout);
                    }
                })
                .Ref(mapViewRef),
            GalleryControls.ControlPanel(
                Caption("Tap the map to identify a feature and show its callout. If nothing is found, a custom Reactor callout is displayed.")));
    }

    private static GeoElement? GetGeoElement(IEnumerable<IdentifyLayerResult> results)
    {
        foreach (var result in results)
        {
            var geoElement = GetGeoElement(result);
            if (geoElement is not null)
            {
                return geoElement;
            }
        }

        return null;
    }

    private static GeoElement? GetGeoElement(IdentifyLayerResult result)
    {
        var geoElement = result.GeoElements.FirstOrDefault();
        if (geoElement is not null)
        {
            return geoElement;
        }

        foreach (var sublayerResult in result.SublayerResults)
        {
            var sublayerGeoElement = GetGeoElement(sublayerResult);
            if (sublayerGeoElement is not null)
            {
                return sublayerGeoElement;
            }
        }

        return null;
    }

    private static UIElement CreateNoFeaturesCallout()
    {
        var host = new ReactorHostControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            MinWidth = 220,
        };

        host.Mount(static _ =>
            Border(
                VStack(4,
                    TextBlock("No features found")
                        .SemiBold()
                        .FontSize(15),
                    TextBlock("Try tapping directly on a feature with popups or attributes.")
                        .Foreground(Theme.SecondaryText)
                        .Set(textBlock => textBlock.TextWrapping = TextWrapping.Wrap))
            )
            .Padding(12)
            .Background(Theme.CardBackground)
            .WithBorder(Theme.CardStroke)
            .CornerRadius(12));

        return host;
    }
}
