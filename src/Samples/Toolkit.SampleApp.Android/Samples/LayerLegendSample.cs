
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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.LayerLegendSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = CreateMap();
#pragma warning disable CS0618 // Type or member is obsolete
            var legend = FindViewById<UI.Controls.LayerLegend>(Resource.Id.layerLegend);
#pragma warning restore CS0618 // Type or member is obsolete
            legend.LayerContent = mapView.Map.OperationalLayers[0];
        }

        private Map CreateMap()
        {
            Map map = new Map(Basemap.CreateLightGrayCanvasVector())
            {
                InitialViewpoint = new Viewpoint(new Envelope(569614.225, 6847121.683, 570198.333, 6846604.317, SpatialReferences.WebMercator))
            };
            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://basisregistraties.arcgisonline.nl/arcgis/rest/services/DKK/DKKv4/MapServer")));
            return map;
        }
  
    }
}