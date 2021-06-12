using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.OverviewMap
{
    public sealed partial class OverviewMapWithSceneSample : Page
    {
        public OverviewMapWithSceneSample()
        {
            this.InitializeComponent();
            sceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);

        }
    }
}
