using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "Compass", Description = "Compass with SceneView sample")]
    public partial class CompassSceneViewSample : ContentPage
    {
        public CompassSceneViewSample()
        {
            InitializeComponent();
            sceneView.Scene = new Scene(Basemap.CreateImagery());
        }
    }
}
