using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.OverviewMap
{
    public sealed partial class OverviewMapSample : Page
    {
        private bool _symbolToggle;
        private bool _mapToggle;
        public OverviewMapSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
            SampleOverview.GeoView = MyMapView;
        }

        private void Slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            _ = MyMapView.SetViewpointRotationAsync(e.NewValue);
        }

        private void ToggleViewClick(object sender, RoutedEventArgs e)
        {
            if (MyMapView.Visibility == Visibility.Visible)
            {
                MyMapView.Visibility = Visibility.Collapsed;
                MySceneView.Visibility = Visibility.Visible;
                SampleOverview.GeoView = MySceneView;
            }
            else
            {
                MyMapView.Visibility = Visibility.Visible;
                MySceneView.Visibility = Visibility.Collapsed;
                SampleOverview.GeoView = MyMapView;
            }
        }

        private void ToggleModelClick(object sender, RoutedEventArgs e)
        {
            _mapToggle = !_mapToggle;

            SampleOverview.Map = _mapToggle ? new Map(BasemapStyle.OSMDarkGray) : new Map(BasemapStyle.ArcGISNavigation);
        }

        private void ToggleSymbolsClick(object sender, RoutedEventArgs e)
        {
            _symbolToggle = !_symbolToggle;
            if (_symbolToggle)
            {
                SampleOverview.AreaSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.DiagonalCross, System.Drawing.Color.Orange, null);
                SampleOverview.PointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 16);
            }
            else
            {
                SampleOverview.AreaSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Horizontal, System.Drawing.Color.Magenta, new SimpleLineSymbol(SimpleLineSymbolStyle.ShortDash, System.Drawing.Color.Green, 2));
                SampleOverview.PointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Magenta, 8);
            }
        }
    }
}
