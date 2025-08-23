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
            Map map = new Map(new Uri("http://www.arcgis.com/home/webmap/viewer.html?webmap=f1ed0d220d6447a586203675ed5ac213"));
            return map;
        }
    }
}
