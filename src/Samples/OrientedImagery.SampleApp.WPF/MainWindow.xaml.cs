using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.OrientedImagery
{
    public partial class MainWindow : Window
    {
        private const string MapBasemap = "https://runtime.maps.arcgis.com/home/item.html?id=67372ff42cd145319639a99152b15bc3";
        private const string SceneBasemap = "https://runtime.maps.arcgis.com/home/item.html?id=0560e29930dc4d5ebeb58c635c0909c9";
        private const string ElevationUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

        private readonly List<string> _exampleLayers =
        [
            "https://services.arcgis.com/2Pv4ow3pE6NFC9SW/ArcGIS/rest/services/Oakwood_camori_corrected/FeatureServer/0",
            "https://services8.arcgis.com/joda3ARQ9znLf0hf/ArcGIS/rest/services/HopefulHeights_360/FeatureServer/1",
            "https://services5.arcgis.com/N82JbI5EYtAkuUKU/ArcGIS/rest/services/EsriCampus_sequenceTest_attachment/FeatureServer/1"
        ];

        private readonly OrientedImageryViewModel _orientedImageryVM;
        private readonly MapView _mapView;
        private readonly SceneView _sceneView;
        private readonly ElevationSource _elevationSource;
        private bool _usingMapView;
        private GeoView _currentGeoView => _usingMapView ? _mapView : _sceneView;

        public MainWindow()
        {
            InitializeComponent();

            _mapView = new MapView() { Map = new Map(new Uri(MapBasemap)) };
            _sceneView = new SceneView() { Scene = new Scene(new Uri(SceneBasemap)) };
            _usingMapView = true;
            GeoViewContainer.Children.Add(_currentGeoView);

            _elevationSource = new ArcGISTiledElevationSource(new Uri(ElevationUrl));

            MainOrientedImageryView.GeoView = _currentGeoView;
            _orientedImageryVM = MainOrientedImageryView.ViewModel;

            SelectLayerDropdown.ItemsSource = _exampleLayers;
        }

        private async Task ApplyLayer(string layerUrl)
        {
            try
            {
                var layer = new OrientedImageryLayer(new Uri(layerUrl));
                if (_usingMapView)
                {
                    _mapView.Map!.OperationalLayers.Clear();
                    _mapView.Map!.OperationalLayers.Add(layer);
                    _orientedImageryVM.OrientedImageryLayer = layer;
                }
                else
                {
                    layer.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                    _sceneView.Scene!.OperationalLayers.Clear();
                    _sceneView.Scene!.OperationalLayers.Add(layer);
                    _orientedImageryVM.OrientedImageryLayer = layer;
                }
                await layer.LoadAsync();
                _currentGeoView.SetViewpoint(new Viewpoint(layer.FullExtent!));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply layer: {ex}");
            }
        }

        private async void SelectLayerDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectLayerDropdown.SelectedItem is string layerUrl)
                await ApplyLayer(layerUrl);
        }

        private void AddLayerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var _ = new OrientedImageryLayer(new Uri(LayerUriTextBox.Text));
                _exampleLayers.Add(LayerUriTextBox.Text);
                SelectLayerDropdown.ItemsSource = null;
                SelectLayerDropdown.ItemsSource = _exampleLayers;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void Toggle2DButton_Click(object sender, RoutedEventArgs e)
        {
            GeoViewContainer.Children.Clear();
            _usingMapView = !_usingMapView;
            MainOrientedImageryView.GeoView = _currentGeoView;
            GeoViewContainer.Children.Add(_currentGeoView);
            SelectLayerDropdown.SelectedItem = null;
            _orientedImageryVM.OrientedImageryLayer = null;
        }

        private void ToggleElevationSurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sceneView.Scene?.BaseSurface is not Surface surface)
                return;

            if (surface.ElevationSources.Count > 0)
                surface.ElevationSources.Clear();
            else
                surface.ElevationSources.Add(_elevationSource);
        }
    }
}
