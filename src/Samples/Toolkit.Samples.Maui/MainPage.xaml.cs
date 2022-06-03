using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace Toolkit.Samples.MAUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            ArcGISRuntimeEnvironment.ApiKey = "AAPK034aa764a7c442ffb4fddd8ffc427a61JIt6iRRIn3I40rprNUc17qJOn3pFbft5c5UBXqtqwuETql8p_WUgmV9CdYRyMNFF";

            var map = new Map(BasemapStyle.ArcGISLightGray);

            mapView.Map = map;
        }
    }
}