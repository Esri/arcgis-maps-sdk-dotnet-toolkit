using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Legend
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
