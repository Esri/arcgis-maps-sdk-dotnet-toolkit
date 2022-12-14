using Esri.ArcGISRuntime.Symbology;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.SymbolDisplay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SymbolDisplaySample : Page
    {
        public SymbolDisplaySample()
        {
            this.InitializeComponent();
            LoadSymbols();
        }

        private void LoadSymbols()
        {
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 10));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 20));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 30));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 40));

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

        private void AddSymbol(Symbology.Symbol symbol)
        {
            int columnCount = SymbolGrid.ColumnDefinitions.Count;
            var sd = new UI.Controls.SymbolDisplay() { Symbol = symbol };
            var f = new Border()
            {
#if NETFX_CORE
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black),
#else
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black),
#endif
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0)
            };
            f.Child = sd;
            int count = SymbolGrid.Children.Count;
            var row = count / columnCount;
            var column = count % columnCount;
            if (column == 0)
                SymbolGrid.RowDefinitions.Add(new RowDefinition());
            Grid.SetRow(f, row);
            Grid.SetColumn(f, column);
            SymbolGrid.Children.Add(f);
        }
    }
}
