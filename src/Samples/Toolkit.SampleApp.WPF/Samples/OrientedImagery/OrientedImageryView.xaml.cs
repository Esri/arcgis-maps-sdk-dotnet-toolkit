using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.OrientedImagery
{
    /// <summary>
    /// Interaction logic for OrientedImageryView.xaml
    /// </summary>
    public partial class OrientedImageryView : UserControl
    {
        private OrientedImageryLayer _oiLayer;

        public OrientedImageryView()
        {
            InitializeComponent();

            _ = Initialize();
        }

        private async Task Initialize()
        {
            _oiLayer = new OrientedImageryLayer(new Uri("https://services8.arcgis.com/joda3ARQ9znLf0hf/arcgis/rest/services/RedRocks/FeatureServer/0"));
            await _oiLayer.LoadAsync();

            MainMapView.Map = new Mapping.Map(BasemapStyle.ArcGISTopographic);
            MainMapView.Map.OperationalLayers.Add(_oiLayer);
            MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;

            var markerSymbolPickerVM = new SelectNewMarkerSymbolVM(new Collection<MarkerSymbol>()
            {
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 10),
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Yellow, 10),
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 10)
            });
            var itemsSouce = new Collection<object>() { new AutoUpdateFootprintVM(), new ShowSelectedFootprintVM(), new ShowUnselectedFootprintsVM(), new ShowCameraMarkersVM(), markerSymbolPickerVM, new ClearMarkersVM(), new DemoOilToolbarVM() };
            MainOrientedImageryView.ItemsSource = itemsSouce;

            MainOrientedImageryView.ViewModel.SelectedImage = new Mapping.OrientedImage();
            MainOrientedImageryView.OrientedImageryLayer = _oiLayer;
            MainMapView.SetViewpoint(new Viewpoint(_oiLayer.FullExtent));
        }

        private async void MainMapView_GeoViewTapped(object sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            var parameters = new OrientedImageSearchParameters();
            var images = await _oiLayer.SearchImagesAsync(e.Location, parameters);
            MainOrientedImageryView.ViewModel.SetImages(images.ToList(), e.Location);
            MainOrientedImageryView.ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
        }
    }
}
