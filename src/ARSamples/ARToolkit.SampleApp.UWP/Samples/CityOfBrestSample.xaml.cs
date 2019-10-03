using Esri.ArcGISRuntime.Geometry;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ARToolkit.SampleApp.Samples
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CityOfBrestSample : Page
    {
        public CityOfBrestSample()
        {
            this.InitializeComponent();

            arview.RenderVideoFeed = false;
            Surface sceneSurface = new Surface();
            sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            Scene scene = new Scene(Basemap.CreateImagery())
            {
                BaseSurface = sceneSurface
            };

            // Create and add a building layer.
            ArcGISSceneLayer buildingsLayer = new ArcGISSceneLayer(new Uri("https://tiles.arcgis.com/tiles/P3ePLMYs2RVChkJx/arcgis/rest/services/Buildings_Brest/SceneServer/layers/0"));
            scene.OperationalLayers.Add(buildingsLayer);
            MapPoint start = new MapPoint(-4.494677, 48.384472, 24.772694, SpatialReferences.Wgs84);
            arview.OriginCamera = new Camera(start, 200, 0, 90, 0);
            arview.Scene = scene;
            arview.StartTrackingAsync();
        }
    }
}
