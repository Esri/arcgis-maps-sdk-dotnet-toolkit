using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Microsoft.UI.Reactor.Hooks;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class OverviewMapPage : Component
{
    private readonly Map map = new Map(BasemapStyle.ArcGISImagery);

    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<Esri.ArcGISRuntime.UI.Controls.GeoView>();
        var (alternateSymbols, setAlternateSymbols) = UseState(false);
        var (wideOverview, setWideOverview) = UseState(false);

        var areaSymbol = alternateSymbols
            ? new SimpleFillSymbol(SimpleFillSymbolStyle.DiagonalCross, System.Drawing.Color.Orange, null)
            : new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 1));

        var pointSymbol = alternateSymbols
            ? new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 16)
            : new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Red, 16);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .Ref(mapViewRef),

            (OverviewMap(mapViewRef) with
            {
                AreaSymbol = areaSymbol,
                PointSymbol = pointSymbol,
                ScaleFactor = wideOverview ? 50 : 25,
            })
            .Width(260)
            .Height(180)
            .Margin(20)
            .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Right)
            .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Bottom),

            GalleryControls.ControlPanel(
                VStack(8,
                    ToggleSwitch(alternateSymbols, value => setAlternateSymbols(value), onContent: "Alternate symbols", offContent: "Default symbols", header: "Viewport symbols"),
                    ToggleSwitch(wideOverview, value => setWideOverview(value), onContent: "Wide overview extent", offContent: "Default extent", header: "Scale factor")))
        );
    }
}
