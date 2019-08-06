using Esri.ArcGISRuntime.Data;
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
    [SampleInfoAttribute(Category = "Legend", DisplayName = "Legend", Description = "Render a legend for a map")]
    public partial class LegendViewController : UIViewController
    {
        private Legend legend;
        private MapView mapView;

        public LegendViewController()
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
            mapView.Map = new Map(Basemap.CreateLightGrayCanvasVector());
            mapView.Map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Census/MapServer")));
            this.View.AddSubview(mapView);

            legend = new Legend()
            {
                GeoView = mapView,
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