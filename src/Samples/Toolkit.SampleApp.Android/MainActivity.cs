using Android.App;
using Android.Widget;
using Android.OS;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    [Activity(Label = "Toolkit.Samples.Droid", MainLauncher = true)]
    public class MainActivity : Activity
    {
        UI.Controls.Compass compass;
        UI.Controls.ScaleLine scaleLine;
        MapView mapView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(Basemap.CreateStreets());
            mapView.ViewpointChanged += MapView_ViewpointChanged;

            scaleLine = FindViewById<UI.Controls.ScaleLine>(Resource.Id.scaleLine);
            compass = FindViewById<UI.Controls.Compass>(Resource.Id.compass);
        }

        private void MapView_ViewpointChanged(object sender, System.EventArgs e)
        {
            compass.Heading = mapView.MapRotation;
            RunOnUiThread(() =>
            {
                scaleLine.MapScale = mapView.MapScale;
            });
        }
    }
}

