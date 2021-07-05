using Esri.ArcGISRuntime.Mapping;
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

        private void Slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            _ = mapView.SetViewpointRotationAsync(e.NewValue);
        }
    }
}
