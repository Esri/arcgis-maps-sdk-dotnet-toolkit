using Esri.ArcGISRuntime.Mapping;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.OverviewMap
{
    [SampleInfoAttribute(Category = "OverviewMap", DisplayName = "OverviewMap - Scene", Description = "OverviewMap sample")]
    public partial class OverviewMapWithSceneSample : UserControl
    {
        public OverviewMapWithSceneSample()
        {
            InitializeComponent();

            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);
        }
    }
}
