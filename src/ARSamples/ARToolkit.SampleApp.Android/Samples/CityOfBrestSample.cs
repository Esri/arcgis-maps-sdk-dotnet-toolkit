using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Mapping;
using Android.Opengl;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using System;
using Javax.Microedition.Khronos.Egl;
using Javax.Microedition.Khronos.Opengles;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Android.Support.Design.Widget;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Geometry;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp.Samples
{
    [Activity(
        Label = "City of Brest",
        Theme = "@style/Theme.AppCompat",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    [SampleInfo(DisplayName = "City of Brest", Description = "A sample that doens't rely on ARCore but only features the ability to look around")]
    public class CityOfBrestSample : ARActivityBase
    {
        private Scene Scene;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                ARView.RenderVideoFeed = false;
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
                ARView.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(start, 200, 0, 90, 0);
                ARView.Scene = scene;
                ARView.StartTrackingAsync();
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}