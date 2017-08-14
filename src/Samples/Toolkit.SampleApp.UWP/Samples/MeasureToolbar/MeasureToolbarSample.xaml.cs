using Esri.ArcGISRuntime.Mapping;
using System;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.MeasureToolbar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MeasureToolbarSample : Page
    {
        public MeasureToolbarSample()
        {
            this.InitializeComponent();

            var map = new Map(Basemap.CreateLightGrayCanvasVector());
            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/USA/MapServer/2")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/USA/MapServer/1")));
            mapView.Map = map;
        }

    }
}