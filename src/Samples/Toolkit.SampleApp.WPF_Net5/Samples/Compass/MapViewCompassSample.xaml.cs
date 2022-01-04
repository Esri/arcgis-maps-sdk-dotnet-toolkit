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
    [SampleInfoAttribute(Category = "Compass", DisplayName = "Compass - MapView", Description = "Compass used with a MapView")]
    public partial class MapViewCompassSample : UserControl
    {
        public MapViewCompassSample()
        {
            InitializeComponent();
        }
        
        private void RotateLeft_Click(object sender, RoutedEventArgs e) => _ = HandleRotateLeft();

        private async Task HandleRotateLeft()
        {
            try
            {
                await mapView.SetViewpointRotationAsync(mapView.MapRotation - 30);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e) => _ = HandleRotateRight();

        private async Task HandleRotateRight()
        {
            try
            {
                await mapView.SetViewpointRotationAsync(mapView.MapRotation + 30);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e) => _ = HandleResetClick();

        private async Task HandleResetClick()
        {
            try
            {
                await mapView.SetViewpointRotationAsync(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
