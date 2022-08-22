using Esri.ArcGISRuntime.Mapping;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.SearchView
{
    public partial class SearchViewMapSample : UserControl
    {
        public SearchViewMapSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }
    }
}
