
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
    [Activity(Label = "Legend")]
    [SampleInfoAttribute(Category = "Legend", Description = "Render a legend for a map")]
    public class LegendSampleActivity : Activity
    {
        private MapView mapView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.LegendSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(new Uri("https://www.arcgis.com/home/webmap/viewer.html?webmap=f1ed0d220d6447a586203675ed5ac213"));
            var legend = FindViewById<UI.Controls.Legend>(Resource.Id.legend);
            legend.GeoView = mapView;
        }
    }
}