using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.OverviewMap
{
    public sealed partial class OverviewMapSample : Page
    {
        public OverviewMapSample()
        {
            this.InitializeComponent();
            mapView.Map = new Map(BasemapStyle.ArcGISImagery);

        }
    }
}
