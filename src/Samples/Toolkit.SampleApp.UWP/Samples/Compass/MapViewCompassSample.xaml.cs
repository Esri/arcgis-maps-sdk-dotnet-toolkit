using Esri.ArcGISRuntime.Mapping;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Compass
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MapViewCompassSample : Page
    {
        public MapViewCompassSample()
        {
            this.InitializeComponent();
        }

        public Map Map { get; } = new Map(new Uri("http://www.arcgis.com/home/webmap/viewer.html?webmap=c50de463235e4161b206d000587af18b"));


        private async void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            await mapView.SetViewpointRotationAsync(mapView.MapRotation - 30);
        }

        private async void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            await mapView.SetViewpointRotationAsync(mapView.MapRotation + 30);
        }

        private async void Reset_Click(object sender, RoutedEventArgs e)
        {
            await mapView.SetViewpointRotationAsync(0);
        }
    }
}
