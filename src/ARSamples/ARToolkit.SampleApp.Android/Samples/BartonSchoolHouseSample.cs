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
using Android.Views;

namespace ARToolkit.SampleApp.Samples
{
    [Activity(
        Label = "Barton School House",
        Theme = "@style/Theme.AppCompat",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    [SampleData(ItemId = "b30f53d65c714054b75a0eb16639529a", Path = "BartonSchoolHouse_3d_mesh.slpk")]
    public class BartonSchoolHouseSample : ARActivityBase
    {
        private Scene Scene;
        
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                Scene = await ARTestScenes.CreateBartonSchoolHouse(ARView);
                ARView.Scene = Scene;
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}