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
        Label = "Earth",
        Theme = "@style/Theme.AppCompat",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    [SampleInfo(DisplayName = "Earth", Description = "Shows the entire earth hovering in front of you allowing you to walk around it")]
    public class EarthSample : ARActivityBase
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var scene = new Scene(Basemap.CreateImagery());
                scene.BaseSurface = new Surface();
                scene.BaseSurface.BackgroundGrid.IsVisible = false;
                scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                scene.BaseSurface.ElevationExaggeration = 50;
                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                ARView.TranslationFactor = 100000000;
                // Set pitch to 0 so looking forward looks "down" on earth from space
                ARView.OriginCamera = new Camera(new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84), 0, 0, 0);
                ARView.NorthAlign = false;
                await scene.LoadAsync();
                ARView.Scene = scene;
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}