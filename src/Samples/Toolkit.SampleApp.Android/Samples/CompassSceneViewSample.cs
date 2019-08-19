using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [Activity(Label = "Compass - SceneView", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "Compass", Description = "Compass used with a SceneView")]
    public class CompassSceneViewSampleActivity : Activity
    {
        private SceneView sceneView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CompassSceneViewSample);
            sceneView = FindViewById<SceneView>(Resource.Id.sceneView);
            sceneView.Scene = new Scene(Basemap.CreateImagery());
            var compass = FindViewById<UI.Controls.Compass>(Resource.Id.compass);
            compass.GeoView = sceneView;
            compass.AutoHide = false;
        }
    }
}