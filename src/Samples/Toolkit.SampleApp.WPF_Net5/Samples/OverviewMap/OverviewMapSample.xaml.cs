using Esri.ArcGISRuntime.Mapping;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.OverviewMap
{
    [SampleInfoAttribute(Category = "OverviewMap", DisplayName = "OverviewMap - Map", Description = "OverviewMap sample")]
    public partial class OverviewMapSample : UserControl
    {
        public OverviewMapSample()
        {
            InitializeComponent();

            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }

        private void Handle_valuechanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Set the MapView rotation to that of the Slider.
            MyMapView.SetViewpointRotationAsync(e.NewValue);
        }
    }
}
