
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
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [Activity(Label = "Legend", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "Legend", Description = "Render a legend for a map")]
    public class LegendSampleActivity : Activity
    {
        private MapView mapView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.LegendSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = CreateMap();
            var legend = FindViewById<UI.Controls.Legend>(Resource.Id.legend);
            legend.GeoView = mapView;
            var checkbox = FindViewById<CheckBox>(Resource.Id.checkboxVisibleRangeOnly);
            checkbox.CheckedChange += (s, e) =>
            {
                legend.FilterByVisibleScaleRange = e.IsChecked;
            };
        }

        private Map CreateMap()
        {
            Map map = new Map(Basemap.CreateLightGrayCanvasVector())
            {
                InitialViewpoint = new Viewpoint(new Envelope(-178, 17.8, -65, 71.4, SpatialReference.Create(4269)))
            };
            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Census/MapServer")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/SF311/FeatureServer/0")));
            return map;
        }
    }
}