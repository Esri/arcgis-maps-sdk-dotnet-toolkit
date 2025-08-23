namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.GeoViewController
{
    public sealed partial class GeoViewControllerSample : Page
    {
        public GeoViewControllerSample()
        {
            this.InitializeComponent();
        }
        public GeoViewControllerSampleVM VM { get; } = new GeoViewControllerSampleVM();
    }
}
