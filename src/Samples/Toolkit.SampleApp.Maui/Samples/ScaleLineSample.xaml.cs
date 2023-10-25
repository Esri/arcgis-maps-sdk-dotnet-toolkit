using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "ScaleLine", Description = "Demonstrates ScaleLine.")]
    public partial class ScaleLineSample : ContentPage
	{
		public ScaleLineSample ()
		{
			InitializeComponent ();
            mapView.Map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        }
	}
}