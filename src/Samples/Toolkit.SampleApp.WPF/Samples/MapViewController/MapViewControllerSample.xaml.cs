using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.MapViewController
{
    [SampleInfo(ApiKeyRequired = true)]
    public partial class MapViewControllerSample : UserControl
    {
        public MapViewControllerSample()
        {
            InitializeComponent();
            CreateInitialGraphics();
        }

        private void CreateInitialGraphics()
        {
            var polygonPoints = new List<MapPoint>
            {
                new(-117.1956, 34.0565),
                new(-117.1910, 34.0565),
                new(-117.1910, 34.0600),
                new(-117.1933, 34.0610),
                new(-117.1956, 34.0600),
                new(-117.1956, 34.0565)
            };
            var demoPolygon = new Polygon(polygonPoints, SpatialReferences.Wgs84);
            var polygonGraphic = new Graphic(demoPolygon, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 2));

            // Add a new vertex to the polyline
            var polylinePoints = new List<MapPoint>
            {
                new(-117.1956, 34.0565),
                new(-117.1934, 34.0575),
                new(-117.1910, 34.0565),
                new(-117.1920, 34.0580)
            };
            var demoPolyline = new Polyline(polylinePoints, SpatialReferences.Wgs84);
            var polylineGraphic = new Graphic(demoPolyline, new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Blue, 2));

            var graphicsOverlay = new GraphicsOverlay();
            graphicsOverlay.Graphics.Add(polygonGraphic);
            graphicsOverlay.Graphics.Add(polylineGraphic);

            var pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Green, 10);
            foreach (var pt in polygonPoints)
            {
                var pointGraphic = new Graphic(pt, pointSymbol);
                graphicsOverlay.Graphics.Add(pointGraphic);
            }

            MyMapView.GraphicsOverlays.Add(graphicsOverlay);


            MyMapView.SetViewpoint(new Mapping.Viewpoint(demoPolygon.Extent));
        }
    }
}
