using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "ScaleLine", Description = "Demonstrates ScaleLine.")]
    public partial class ScaleLineSample : ContentPage
	{
		public ScaleLineSample ()
		{
			InitializeComponent ();
            mapView.Map = new Map(Basemap.CreateLightGrayCanvasVector());
        }
	}
}