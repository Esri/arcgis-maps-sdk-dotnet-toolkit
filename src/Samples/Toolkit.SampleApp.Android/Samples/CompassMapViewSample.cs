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
        private UI.Controls.Compass compass;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CompassMapViewSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(Basemap.CreateLightGrayCanvasVector());
            compass = FindViewById<UI.Controls.Compass>(Resource.Id.compass);
            compass.GeoView = mapView;
            compass.AutoHide = false;
            var slider = FindViewById<SeekBar>(Resource.Id.sizeSlider);
            slider.Max = 100;
            slider.Progress = 30;
            slider.Min = 10;
            slider.ProgressChanged += Slider_ProgressChanged;
        }

        private void Slider_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            var lp = compass.LayoutParameters;
            lp.Width = lp.Height = e.Progress;
            compass.LayoutParameters = lp;
        }
    }
}