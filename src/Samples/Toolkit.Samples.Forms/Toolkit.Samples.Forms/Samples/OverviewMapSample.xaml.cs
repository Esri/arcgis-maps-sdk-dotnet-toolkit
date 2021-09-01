using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "OverviewMap", Description = "OverviewMap sample")]
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

        private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            MyMapView.SetViewpointRotationAsync(e.NewValue);
        }

        private void ToggleViewClick(object sender, EventArgs e)
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

        private void ToggleModelClick(object sender, EventArgs e)
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

        private void ToggleSymbolsClick(object sender, EventArgs e)
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