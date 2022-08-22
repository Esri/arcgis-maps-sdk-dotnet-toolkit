using Esri.ArcGISRuntime.Mapping;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.FloorFilter
{
    public sealed partial class FloorFilterSample : Page
    {
        public FloorFilterSample()
        {
            InitializeComponent();
            MyMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=b4b599a43a474d33946cf0df526426f5"));
        }
    }
}
