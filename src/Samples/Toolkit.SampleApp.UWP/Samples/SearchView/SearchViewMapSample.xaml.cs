using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.SearchView
{
    public sealed partial class SearchViewMapSample : Page
    {
        public SearchViewMapSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }
    }
}
