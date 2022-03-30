
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.FloorFilter
{
    public partial class SimpleFloorFilterSample: UserControl
    {
        public SimpleFloorFilterSample()
        {
            InitializeComponent();
            MyMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=b4b599a43a474d33946cf0df526426f5"));
        }
    }
}