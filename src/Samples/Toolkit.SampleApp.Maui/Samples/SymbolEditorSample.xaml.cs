using Esri.ArcGISRuntime.Symbology;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "SymbolDisplay", Description = "Dynamically edit a symbol")]
    public partial class SymbolEditorSample : ContentPage
    {
        public SymbolEditorSample()
        {
            InitializeComponent();
            BindingContext = this;
        }
        public List<SimpleMarkerSymbolStyle> SimpleMarkerSymbolStyles { get; } = Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).OfType<SimpleMarkerSymbolStyle>().ToList();

        public SimpleMarkerSymbol Symbol { get; } = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Red, 20) { Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 2) };

    }
}