using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Compass
{
    public partial class MapViewCompassSample : UserControl
    {
        public MapViewCompassSample()
        {
            InitializeComponent();
        }
        
        private async void Compass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // When tapping the compass, reset the rotation
            await mapView.SetViewpointRotationAsync(0);
        }

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
