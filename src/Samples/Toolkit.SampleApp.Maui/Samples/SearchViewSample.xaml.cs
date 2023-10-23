using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "SearchView", Description = "Demonstrates SearchView used with a map.", ApiKeyRequired = true)]
    public partial class SearchViewSample : ContentPage
    {
        public SearchViewSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }
    }
}