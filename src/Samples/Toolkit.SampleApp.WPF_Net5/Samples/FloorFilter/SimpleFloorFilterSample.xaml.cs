
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
            MyMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=f133a698536f44c8884ad81f80b6cfc7"));
        }
    }
}