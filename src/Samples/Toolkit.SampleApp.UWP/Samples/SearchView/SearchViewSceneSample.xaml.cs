using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.SearchView
{
    public sealed partial class SearchViewSceneSample : Page
    {
        public SearchViewSceneSample()
        {
            InitializeComponent();
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);
        }
    }
}
