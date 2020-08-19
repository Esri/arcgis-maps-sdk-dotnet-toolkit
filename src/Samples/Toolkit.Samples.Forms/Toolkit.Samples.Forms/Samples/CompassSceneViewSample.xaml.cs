using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "Compass", Description = "Compass used with a SceneView")]
    public partial class CompassSceneViewSample : ContentPage
    {
        public CompassSceneViewSample()
        {
            InitializeComponent();
            sceneView.Scene = new Scene(Basemap.CreateImagery());
        }
    }
}
