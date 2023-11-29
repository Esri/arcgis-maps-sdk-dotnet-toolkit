using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "OverviewMap", Description = "Demonstrates various scenarios for the OverviewMap control.", ApiKeyRequired = true)]
    public partial class OverviewMapSample : ContentPage
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

        private void Slider_ValueChanged(object? sender, ValueChangedEventArgs e)
        {

            if (MyMapView.IsVisible == true)
            {
                var vp = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
                MyMapView.SetViewpoint(new Viewpoint(vp.TargetGeometry, e.NewValue));
            }
            else
            {
                var camera = MySceneView.Camera;
                MySceneView.SetViewpointCamera(camera.RotateTo(e.NewValue, camera.Pitch, camera.Roll));
            }
        }

        private void ToggleViewClick(object? sender, EventArgs e)
        {
            if (MyMapView.IsVisible == true)
            {
                MyMapView.IsVisible = false;
                MySceneView.IsVisible = true;
                SampleOverview.GeoView = MySceneView;
            }
            else
            {
                MyMapView.IsVisible = true;
                MySceneView.IsVisible = false;
                SampleOverview.GeoView = MyMapView;
            }
        }

        private void ToggleModelClick(object? sender, EventArgs e)
        {
            _mapToggle = !_mapToggle;

            if (_mapToggle)
            {
                SampleOverview.Map = new Map(BasemapStyle.OSMDarkGray);
            }
            else
            {
                SampleOverview.Map = new Map(BasemapStyle.ArcGISNavigation);
            }
        }

        private void ToggleSymbolsClick(object? sender, EventArgs e)
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