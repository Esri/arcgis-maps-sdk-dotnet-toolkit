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
        Label = "Earth",
        Theme = "@style/Theme.AppCompat.NoActionBar",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class EarthSample : ARActivityBase
    {
        private Scene Scene;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                Scene = new Scene(Basemap.CreateImagery())
                {
                    InitialViewpoint = new Viewpoint(
                        new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84),
                        new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84), 0, 0, 0))
                };
                Scene.BaseSurface = new Surface();
                Scene.BaseSurface.BackgroundGrid.IsVisible = false;
                Scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("http://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                Scene.BaseSurface.ElevationExaggeration = 10;
                Scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                ARView.TranslationFactor = 100000000;
                await Scene.LoadAsync();
                ARView.Scene = Scene;
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}