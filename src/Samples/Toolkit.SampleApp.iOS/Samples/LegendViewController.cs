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
                TranslatesAutoresizingMaskIntoConstraints = false,
                Map = CreateMap()
            };
            this.View.AddSubview(mapView);

            legend = new Legend()
            {
                GeoView = mapView,
                TranslatesAutoresizingMaskIntoConstraints = false,
                FilterHiddenLayers = true,
                FilterByVisibleScaleRange = true
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