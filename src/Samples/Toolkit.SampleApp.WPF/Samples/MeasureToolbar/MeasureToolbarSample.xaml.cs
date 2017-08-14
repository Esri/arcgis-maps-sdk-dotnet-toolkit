using Esri.ArcGISRuntime.Mapping;
using System;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.MeasureToolbar
{
    /// <summary>
    /// Interaction logic for MeasureToolbarSample.xaml
    /// </summary>
    public partial class MeasureToolbarSample : UserControl
    {
        public MeasureToolbarSample()
        {
            InitializeComponent();
            var map = new Map(Basemap.CreateLightGrayCanvasVector());
            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/USA/MapServer/2")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/USA/MapServer/1")));
            mapView.Map = map;
        }
    }
}
