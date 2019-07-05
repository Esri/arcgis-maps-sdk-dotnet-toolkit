using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if FORMS
using Esri.ArcGISRuntime.ARToolkit.Forms;
#else
using Esri.ArcGISRuntime.ARToolkit;
#endif
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace ARToolkit.SampleApp
{
    public static class ARTestScenes
    {
        public static async Task<Scene> CreateBrestFrance(ARSceneView sv)
        {
            // URL for a scene service of buildings in Brest, France
            string brestFrance = @"https://tiles.arcgis.com/tiles/P3ePLMYs2RVChkJx/arcgis/rest/services/Buildings_Brest/SceneServer/layers/0";
            string _elevationSourceUrl = @"https://scene.arcgis.com/arcgis/rest/services/BREST_DTM_1M/ImageServer";
            var scene = new Scene(Basemap.CreateImagery());

            var observerCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(-4.49492, 48.3808, 48.2511, SpatialReferences.Wgs84), 344.488, 74.1212, 0.0);
            scene.InitialViewpoint = new Esri.ArcGISRuntime.Mapping.Viewpoint(observerCamera.Location, observerCamera);

            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri(_elevationSourceUrl)));
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            var iml = new ArcGISSceneLayer(new Uri(brestFrance)) { Opacity = 1 };
            scene.OperationalLayers.Add(iml);
            await iml.LoadAsync();

            sv.TranslationFactor = 250;
            return scene;
        }

        public static async Task<Scene> CreateBerlin(ARSceneView sv)
        {
            // URL for a scene service of buildings in Brest, France
            Uri buildingsService = new Uri("https://tiles.arcgis.com/tiles/P3ePLMYs2RVChkJx/arcgis/rest/services/Buildings_Berlin/SceneServer");
            var iml = new ArcGISSceneLayer(buildingsService) { Opacity = 1 };
            await iml.LoadAsync();

            var observerCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(-4.49492, 48.3808, 48.2511, SpatialReferences.Wgs84), 344.488, 74.1212, 0.0);
            MapPoint center = (MapPoint)GeometryEngine.Project(iml.FullExtent.GetCenter(), SpatialReferences.Wgs84);
            observerCamera = new Esri.ArcGISRuntime.Mapping.Camera(center.Y, center.X, 600, 120, 60, 0);
            var scene = new Scene(Basemap.CreateImagery());
            scene.InitialViewpoint = new Esri.ArcGISRuntime.Mapping.Viewpoint(observerCamera.Location, observerCamera);
            scene.OperationalLayers.Add(iml);
            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            sv.TranslationFactor = 1000;
            return scene;
        }

        public static async Task<Scene> CreatePortland(ARSceneView sv)
        {
            Uri buildingsService = new Uri("https://tiles.arcgis.com/tiles/P3ePLMYs2RVChkJx/arcgis/rest/services/Buildings_Portland/SceneServer");
            var iml = new ArcGISSceneLayer(buildingsService) { Opacity = 1 };
            await iml.LoadAsync();

            var observerCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(-4.49492, 48.3808, 48.2511, SpatialReferences.Wgs84), 344.488, 74.1212, 0.0);
            MapPoint center = (MapPoint)GeometryEngine.Project(iml.FullExtent.GetCenter(), SpatialReferences.Wgs84);
            observerCamera = new Esri.ArcGISRuntime.Mapping.Camera(center.Y, center.X, 225, 220, 80, 0);
            var scene = new Scene(Basemap.CreateImagery());
            scene.InitialViewpoint = new Esri.ArcGISRuntime.Mapping.Viewpoint(observerCamera.Location, observerCamera);
            scene.OperationalLayers.Add(iml);
            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            sv.TranslationFactor = 500;
            return scene;
        }

        public static async Task<Scene> CreatePhiladelphia(ARSceneView sv)
        {
            Uri buildingsService = new Uri("https://scenesampleserverdev.arcgis.com/arcgis/rest/services/Hosted/Buildings_Philadelphia/SceneServer");
            var iml = new ArcGISSceneLayer(buildingsService) { Opacity = 1 };
            await iml.LoadAsync();

            var observerCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(-4.49492, 48.3808, 48.2511, SpatialReferences.Wgs84), 344.488, 74.1212, 0.0);
            MapPoint center = (MapPoint)GeometryEngine.Project(iml.FullExtent.GetCenter(), SpatialReferences.Wgs84);
            observerCamera = new Esri.ArcGISRuntime.Mapping.Camera(center.Y, center.X, 225, 240, 80, 0);
            var scene = new Scene(Basemap.CreateImagery());
            scene.InitialViewpoint = new Esri.ArcGISRuntime.Mapping.Viewpoint(observerCamera.Location, observerCamera);
            scene.OperationalLayers.Add(iml);
            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            sv.TranslationFactor = 500;
            return scene;
        }

        public static async Task<Scene> CreateBartonSchoolHouse(ARSceneView sv)
        {
            var scene = new Scene();
            //scene.InitialViewpoint = new Viewpoint(new MapPoint(34.0508296, -117.215160, SpatialReferences.Wgs84), new Esri.ArcGISRuntime.Mapping.Camera(34.0508296, -117.215160, 385, 0, 0, 0));
            sv.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(34.0508296, -117.215160, 385, 0, 90, 0);
            scene.BaseSurface = new Surface();
            scene.BaseSurface.BackgroundGrid.IsVisible = false;
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            sv.TranslationFactor = 150;
            var p = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var path = "BartonSchoolHouse_3d_mesh.slpk";
            path = System.IO.Path.Combine(p, path);
            var iml = new IntegratedMeshLayer(new Uri(path, UriKind.Relative)) { Opacity = 1 };
            await iml.LoadAsync();
            scene.OperationalLayers.Add(iml);
            return scene;
        }

    }
}