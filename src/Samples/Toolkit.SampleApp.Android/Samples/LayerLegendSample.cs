
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
    [Activity(Label = "Layer Legend", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "Legend", Description = "Render a legend for a single layer")]
    public class LayerLegendSampleActivity : Activity
    {
        private MapView mapView;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.LayerLegendSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = CreateMap();
            var legend = FindViewById<UI.Controls.LayerLegend>(Resource.Id.layerLegend);
            legend.LayerContent = mapView.Map.OperationalLayers[0];
        }

        private Map CreateMap()
        {
            Map map = new Map(Basemap.CreateLightGrayCanvasVector())
            {
                InitialViewpoint = new Viewpoint(new Envelope(-1.98402303E7, 2144435, -7452840, 1.15368106626E7, SpatialReferences.WebMercator))
            };
            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Wildfire/MapServer")));
            return map;
        }
  
    }
}