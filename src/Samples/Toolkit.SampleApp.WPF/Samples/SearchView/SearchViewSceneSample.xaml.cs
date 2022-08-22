using Esri.ArcGISRuntime.Mapping;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.SearchView
{
    public partial class SearchViewSceneSample : UserControl
    {
        public SearchViewSceneSample()
        {
            InitializeComponent();
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);
        }
    }
}
