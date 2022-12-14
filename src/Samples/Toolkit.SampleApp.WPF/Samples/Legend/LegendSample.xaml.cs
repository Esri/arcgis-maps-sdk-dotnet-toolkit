using System;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Legend
{
    public partial class LegendSample : UserControl
    {
        private const string defaultMapUrl = "http://www.arcgis.com/home/webmap/viewer.html?webmap=f1ed0d220d6447a586203675ed5ac213";
        private const string alternateMapUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2";
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
