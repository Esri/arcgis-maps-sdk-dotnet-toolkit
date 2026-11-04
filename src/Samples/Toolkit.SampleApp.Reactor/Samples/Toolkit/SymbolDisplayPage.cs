using Esri.ArcGISRuntime.Symbology;
using Microsoft.UI.Reactor.Hooks;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class SymbolDisplayPage : Component
{
    public override Element Render()
    {
        var symbols = UseMemo(() => new Symbol[]
        {
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 20),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Red, 24),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Green, 28),
            new SimpleLineSymbol(SimpleLineSymbolStyle.DashDot, System.Drawing.Color.Orange, 3),
            new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Orange, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.White, 2)),
            new PictureMarkerSymbol(new Uri("https://cdn3.iconfinder.com/data/icons/web-and-internet-icons/512/Information-256.png")),
        });

        Element SymbolCard(Symbol symbol) =>
            Border(SymbolDisplay(symbol).Width(64).Height(64))
                .Padding(12)
                .Background(Theme.CardBackground)
                .WithBorder(Theme.CardStroke)
                .CornerRadius(12);

        return ScrollView(
            VStack(12,
                HStack(12, SymbolCard(symbols[0]), SymbolCard(symbols[1]), SymbolCard(symbols[2])),
                HStack(12, SymbolCard(symbols[3]), SymbolCard(symbols[4]), SymbolCard(symbols[5])))
            .Margin(20));
    }
}
