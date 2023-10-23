using Esri.ArcGISRuntime.Mapping;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.SearchView
{
    [SampleInfoAttribute(ApiKeyRequired = true)]
    public partial class SearchViewSceneSample : UserControl
    {
        public SearchViewSceneSample()
        {
            InitializeComponent();
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);
        }
    }
}
