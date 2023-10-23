using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "Compass", Description = "Compass with SceneView sample")]
    public partial class CompassSceneViewSample : ContentPage
    {
        public CompassSceneViewSample()
        {
            InitializeComponent();
            sceneView.Scene = new Scene(new Basemap(new Uri("https://www.arcgis.com/home/item.html?id=52bdc7ab7fb044d98add148764eaa30a")));
        }
    }
}
