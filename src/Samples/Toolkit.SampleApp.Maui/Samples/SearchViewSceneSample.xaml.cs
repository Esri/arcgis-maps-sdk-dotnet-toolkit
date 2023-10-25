using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "SearchView", Description = "Demonstrates SearchView used with a scene.", ApiKeyRequired = true)]
    public partial class SearchViewSceneSample : ContentPage
    {
        public SearchViewSceneSample()
        {
            InitializeComponent();
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);
        }
    }
}