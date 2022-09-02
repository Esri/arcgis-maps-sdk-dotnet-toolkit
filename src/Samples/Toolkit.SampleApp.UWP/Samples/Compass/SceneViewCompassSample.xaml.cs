using Esri.ArcGISRuntime.Mapping;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Compass
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SceneViewCompassSample : Page
    {
        public SceneViewCompassSample()
        {
            Scene = new Scene(new Basemap(new Uri("https://www.arcgis.com/home/item.html?id=52bdc7ab7fb044d98add148764eaa30a")));
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
