using Esri.ArcGISRuntime.Mapping;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Legend
{
    [Obsolete]
    public sealed partial class LayerLegendSample : Page
    {
        public LayerLegendSample()
        {
            this.InitializeComponent();
        }

        public Map Map { get; } = new Map(new Uri("http://www.arcgis.com/home/webmap/viewer.html?webmap=f1ed0d220d6447a586203675ed5ac213"));
    }
}
