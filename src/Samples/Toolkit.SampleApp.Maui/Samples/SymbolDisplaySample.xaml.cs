using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui;
using Microsoft.Maui.Controls;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "SymbolDisplay", Description = "Renders a symbol")]
    public partial class SymbolDisplaySample : ContentPage
    {
        public SymbolDisplaySample()
        {
            InitializeComponent();
            LoadSymbols();
        }

        private void LoadSymbols()
        {
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 10));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 20));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 30));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 40));

            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Red, 10));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Green, 20));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Blue, 30));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Blue, 40));

            AddSymbol(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Orange, 5));
            AddSymbol(new SimpleLineSymbol(SimpleLineSymbolStyle.DashDot, System.Drawing.Color.Orange, 2));
            AddSymbol(new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Orange, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.White, 2)));
            AddSymbol(new SimpleFillSymbol(SimpleFillSymbolStyle.ForwardDiagonal, System.Drawing.Color.Yellow, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Yellow, 2)));

            AddSymbol(new PictureMarkerSymbol(new Uri("https://cdn3.iconfinder.com/data/icons/web-and-internet-icons/512/Information-256.png")));
        }

        private void AddSymbol(Symbol symbol)
        {
            int columnCount = LayoutRoot.ColumnDefinitions.Count;
            var sd = new SymbolDisplay() { Symbol = symbol };
            int count = LayoutRoot.Children.Count;
            var row = count / columnCount;
            var column = count % columnCount;
            if (column == 0)
                LayoutRoot.RowDefinitions.Add(new RowDefinition());
            Grid.SetRow(sd, row);
            Grid.SetColumn(sd, column);
            LayoutRoot.Children.Add(sd);
        }
    }
}