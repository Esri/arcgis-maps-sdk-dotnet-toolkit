using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

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


        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            _ = mapView.SetViewpointRotationAsync(mapView.MapRotation - 30);
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            _ = mapView.SetViewpointRotationAsync(mapView.MapRotation + 30);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _ = mapView.SetViewpointRotationAsync(0);
        }
    }
}
