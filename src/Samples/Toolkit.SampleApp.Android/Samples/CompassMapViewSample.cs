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
    [Activity(Label = "Compass - MapView")]
    [SampleInfoAttribute(Category = "Compass", Description = "Compass used with a MapView")]
    public class CompassMapViewSampleActivity : Activity
    {
        private MapView mapView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CompassMapViewSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(Basemap.CreateLightGrayCanvasVector());
            var compass = FindViewById<UI.Controls.Compass>(Resource.Id.compass);
            compass.GeoView = mapView;
            compass.AutoHide = false;
        }
    }
}