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
    [Activity(Label = "ScaleLine", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "ScaleLine")]
    public class ScaleLineSampleActivity : Activity
    {
        private MapView mapView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ScaleLineSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(BasemapStyle.ArcGISLightGray);

            var scaleLine = FindViewById<UI.Controls.ScaleLine>(Resource.Id.scaleLine);
            scaleLine.MapView = mapView;
        }
    }
}