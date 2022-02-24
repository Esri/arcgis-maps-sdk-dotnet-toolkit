using Android.App;
using Android.OS;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp.Samples
{
    [Activity(
        Label = "Tap To Place",
        Theme = "@style/Theme.AppCompat",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    [SampleData(ItemId = "7dd2f97bb007466ea939160d0de96a9d", Path = "philadelphia.mspk")]
    [SampleInfo(DisplayName = "Tap-to-place 3D Model", 
        Description = "This demonstrates the table-top experience, where you can double-tap a surface to place the scene on that surface")]
    public class TapToPlaceSample : ARActivityBase
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                var p = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                var path = System.IO.Path.Combine(p, "philadelphia.mspk");
                MobileScenePackage package = await MobileScenePackage.OpenAsync(path);
                // Load the package.
                await package.LoadAsync();
                // Show the first scene.
                var scene = package.Scenes[0];
                scene.BaseSurface.BackgroundGrid.IsVisible = false;
                scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                ARView.Scene = scene;
                //We'll set the origin of the scene in the middle so we can use that as the tie-point
                ARView.OriginCamera = new Camera(39.9579126, -75.1705827, 9.64, 0, 90, 0);
                ARView.TranslationFactor = 1000; // By increasing the translation factor, the scene appears as if it's at scale 1:1000
                ARView.NorthAlign = false;
                //Set the clipping distance to only render a circular area around the origin
                ARView.ClippingDistance = 350;
                //Set the initial location 1.5 meter in front of and .5m above the scene
                ARView.SetInitialTransformation(TransformationMatrix.Create(0, 0, 0, 1, 0, .5, 1.5));
                //Listen for double-tap to place
                ARView.GeoViewDoubleTapped += ArView_GeoViewDoubleTapped;
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
            }
            ToggleRenderPlanes(true);
        }

        private void ArView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            if (ARView.SetInitialTransformation(e.Position))
            {
                Toast.MakeText(this, "Placed scene", ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(this, "Couldn't place scene", ToastLength.Short).Show();
            }
        }
    }
}