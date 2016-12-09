using Esri.ArcGISRuntime.Mapping;
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

namespace Esri.ArcGISRuntime.Toolkit.Samples.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
            InitializeComponent();

          
            
        }

        System.Threading.CancellationTokenSource tcs;
        private void mapView_GeoViewTapped(object sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (tcs != null)
                tcs.Cancel();
            tcs = new System.Threading.CancellationTokenSource();
           
        }

        private async void OpenMap_Click(object sender, RoutedEventArgs e)
        {

            var id = (sender as MenuItem).Tag as string;
            var map = new Map(new Uri("http://www.arcgis.com/home/item.html?id=" + id));
            try
            {
                await map.LoadAsync();
                mapView.Map = map;
            }
            catch(System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to load map");
            }
        }

        private void Compass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // When tapping the compass, reset the rotation
            mapView.SetViewpointRotationAsync(0);
        }
    }
}
