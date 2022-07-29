using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "Legend", DisplayName = "LayerLegend", Description = "Renders a legend for a single layer")]
    public partial class LayerLegendViewController : UIViewController
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private LayerLegend legend;
#pragma warning restore CS0618 // Type or member is obsolete
        private MapView mapView;

        public LayerLegendViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            mapView = new MapView()
            {
                Map = new Map(new Uri("https://www.arcgis.com/home/webmap/viewer.html?webmap=f1ed0d220d6447a586203675ed5ac213")),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            mapView.Map = new Map(Basemap.CreateLightGrayCanvasVector())
            {
                InitialViewpoint = new Viewpoint(new Envelope(569614.225, 6847121.683, 570198.333, 6846604.317, SpatialReferences.WebMercator))
            };
            mapView.Map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://basisregistraties.arcgisonline.nl/arcgis/rest/services/DKK/DKKv4/MapServer")));
            this.View.AddSubview(mapView);

#pragma warning disable CS0618 // Type or member is obsolete
            legend = new LayerLegend()
#pragma warning restore CS0618 // Type or member is obsolete
            {
                LayerContent = mapView.Map.OperationalLayers[0],
                TranslatesAutoresizingMaskIntoConstraints = false,
            };
            this.View.AddSubview(legend);

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            legend.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            legend.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            legend.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            legend.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
        }
    }
}