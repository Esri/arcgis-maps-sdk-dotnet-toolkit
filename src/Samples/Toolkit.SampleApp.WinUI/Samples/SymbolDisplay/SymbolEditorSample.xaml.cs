using Esri.ArcGISRuntime.Symbology;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.SymbolDisplay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SymbolEditorSample : Page
    {
        public SymbolEditorSample()
        {
            this.InitializeComponent();
        }

        public List<SimpleMarkerSymbolStyle> SimpleMarkerSymbolStyles { get; } = Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).OfType<SimpleMarkerSymbolStyle>().ToList();

        public SimpleMarkerSymbol Symbol { get; } = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Red, 20) { Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 2) };

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var style = (SimpleMarkerSymbolStyle)(e.AddedItems.FirstOrDefault() ?? SimpleMarkerSymbolStyle.Circle); ;
            if (style != Symbol.Style)
                Symbol.Style = style;
        }
    }
}