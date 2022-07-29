
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
            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://server6.tplgis.org/arcgis6/rest/services/National_UHI_2020/MapServer")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("https://services2.arcgis.com/ZQgQTuoyBrtmoGdP/arcgis/rest/services/SF_311_Incidents/FeatureServer/0")));
            return map;
        }
    }
}