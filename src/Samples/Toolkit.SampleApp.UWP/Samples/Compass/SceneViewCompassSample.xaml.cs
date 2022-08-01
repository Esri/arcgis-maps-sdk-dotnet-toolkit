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
    public sealed partial class SceneViewCompassSample : Page
    {
        public SceneViewCompassSample()
        {
            Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
            Scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("http://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            this.InitializeComponent();
        }

        public Scene Scene { get; }
        
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
            sceneView.SetViewpointCamera(camera);
        }
    }
}
