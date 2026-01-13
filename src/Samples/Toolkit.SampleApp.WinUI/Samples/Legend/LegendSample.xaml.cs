using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Legend
{
    public sealed partial class LegendSample : Page
    {
        public LegendSample()
        {
            this.InitializeComponent();
        }

        public Map Map { get; } = CreateMap();


        private static Map CreateMap()
        {
            return new Map(new Uri("http://www.arcgis.com/home/webmap/viewer.html?webmap=df8bcc10430f48878b01c96e907a1fc3"));
        }
    }
}
