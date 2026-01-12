using System;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Legend
{
    public partial class LegendSample : UserControl
    {
        private const string defaultMapUrl = "https://www.arcgis.com/home/item.html?id=df8bcc10430f48878b01c96e907a1fc3";
        private const string alternateMapUrl = "https://www.arcgis.com/home/item.html?id=cf35269c59cd45cc9bfae3480c0a64d7";
        public LegendSample()
        {
            InitializeComponent();
        }

        private void SwitchToOriginalMap_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mapView.Map = new Mapping.Map(new Uri(defaultMapUrl));
        }

        private void SwitchToAlternateMap_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mapView.Map = new Mapping.Map(new Uri(alternateMapUrl));
        }
    }
}
