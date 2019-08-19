using Esri.ArcGISRuntime.Symbology;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

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

        private void ComboBox_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            var style = (SimpleMarkerSymbolStyle)(e.AddedItems.FirstOrDefault() ?? SimpleMarkerSymbolStyle.Circle); ;
            if (style != Symbol.Style)
                Symbol.Style = style;
        }
    }
}