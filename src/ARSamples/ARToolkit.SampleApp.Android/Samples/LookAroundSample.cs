using Android.App;
using Android.OS;
using Android.Widget;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp.Samples
{
    [Activity(
        Label = "Look around mode",
        Theme = "@style/Theme.AppCompat",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    [SampleInfo(DisplayName = "ARCore Disabled", Description = "A sample that doesn't rely on ARCore but only features the ability to look around based on a motion sensor")]
    public class LookAroundSample : ARActivityBase
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                ARView.RenderVideoFeed = false;
                ARView.OriginCamera = new Camera(new MapPoint(-119.622075, 37.720650, 2105), 0, 90, 0); //Yosemite

                Surface sceneSurface = new Surface();
                sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                Scene scene = new Scene(Basemap.CreateImagery())
                {
                    BaseSurface = sceneSurface
                };
                ARView.Scene = scene;
                ARView.StartTrackingAsync(Esri.ArcGISRuntime.ARToolkit.ARLocationTrackingMode.Ignore);
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}