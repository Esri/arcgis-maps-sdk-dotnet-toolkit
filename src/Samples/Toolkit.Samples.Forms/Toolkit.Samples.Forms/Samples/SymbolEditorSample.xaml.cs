using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "SymbolDisplay", Description = "Dynamically edit a symbol")]
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