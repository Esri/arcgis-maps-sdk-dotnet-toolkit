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

namespace Esri.ArcGISRuntime.Toolkit.Samples.Compass
{
    public partial class SceneViewCompassSample : UserControl
    {
        public SceneViewCompassSample()
        {
            InitializeComponent();
            sceneView.Scene = new Scene(BasemapType.Imagery);
        }
        
        private void Compass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // When tapping the compass, reset the rotation
            RotateToHeadingAsync(0);
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotateToHeadingAsync(-30, true);
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotateToHeadingAsync(30, true);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            RotateToHeadingAsync(0);
        }

        private void RotateToHeadingAsync(double heading, bool isDelta = false)
        {
            Camera camera = sceneView.Camera;
            camera = camera.RotateTo(heading + (isDelta ? camera.Heading : 0), camera.Pitch, camera.Roll);
            sceneView.SetViewpointCameraAsync(camera);
        }
    }
}
