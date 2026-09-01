#nullable enable

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.OrientedImagery
{
    /// <summary>
    /// Interaction logic for OrientedImageryView.xaml
    /// </summary>
    public partial class OrientedImageryView : UserControl
    {
        private const string MapBasemap = "https://runtime.maps.arcgis.com/home/item.html?id=67372ff42cd145319639a99152b15bc3";
        private const string SceneBasemap = "https://runtime.maps.arcgis.com/home/item.html?id=0560e29930dc4d5ebeb58c635c0909c9";

        private OrientedImageryLayer? _oiLayer;

        private MapView _mapView;
        private SceneView _sceneView;
        private bool _usingMapView;
        private GeoView _currentGeoView => _usingMapView ? _mapView : _sceneView;

        public OrientedImageryView()
        {
            InitializeComponent();
            ConfigureToolbar();

            _mapView = new MapView() { Map = new Map(new Uri(MapBasemap)) };
            _sceneView = new SceneView() { Scene = new Scene(new Uri(SceneBasemap)) };
            _usingMapView = true;
            _mapView.GeoViewTapped += CurrentGeoView_GeoViewTapped;
            GeoViewContainer.Children.Add(_mapView);
            MainOrientedImageryView.GeoView = _mapView;
        }

        private void ConfigureToolbar()
        {
            var toolbarItems = Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryView.GetDefaultToolbarItems();
            toolbarItems.Add(new ExampleOIToolbarItem());
            MainOrientedImageryView.ItemsSource = toolbarItems;
        }

        private async Task ApplyLayer(Uri layerUri)
        {
            try
            {
                var oiLayer = new OrientedImageryLayer(layerUri);
                await oiLayer.LoadAsync();
                if (oiLayer.LoadStatus == LoadStatus.FailedToLoad)
                    return;

                _oiLayer = oiLayer;

                if (_currentGeoView is MapView mapView)
                {
                    mapView.Map!.OperationalLayers.Clear();
                    mapView.Map.OperationalLayers.Add(_oiLayer);
                }
                else if (_currentGeoView is SceneView sceneView)
                {
                    sceneView.Scene!.OperationalLayers.Clear();
                    sceneView.Scene.OperationalLayers.Add(_oiLayer);
                }

                MainOrientedImageryView.OrientedImageryLayer = _oiLayer;
                if (_oiLayer?.FullExtent != null)
                    _currentGeoView.SetViewpoint(new Viewpoint(_oiLayer.FullExtent));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply layer: {ex}");
            }
        }

        private async void ApplyLayerButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await ApplyLayer(new Uri(LayerUriTextBox.Text));
        }

        private async void CurrentGeoView_GeoViewTapped(object? sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (e.Location == null || _oiLayer == null)
                return;

            // In this case we are choosing to interpret OrientedImageryViewModel.AllowAddingMarkers as mutually exclusive with image searching.
            if (MainOrientedImageryView.ViewModel.AllowAddingMarkers)
            {
                MainOrientedImageryView.ViewModel.AddMarkerLocation(e.Location);
                return;
            }

            var identifyResult = await _currentGeoView.IdentifyLayerAsync(_oiLayer, e.Position, 0, false);
            if (identifyResult.GeoElements.Count > 0 && identifyResult.GeoElements[0] is Feature feature)
            {
                MainOrientedImageryView.ViewModel.SelectedImage = await _oiLayer.FetchImageForFeatureAsync(feature);
            }
            else
            {
                var parameters = new OrientedImageSearchParameters() { MaxResults = -1 };
                var images = await _oiLayer.SearchImagesAsync(e.Location, parameters) ?? new List<OrientedImage>();
                MainOrientedImageryView.ViewModel.SetImages(images.ToList(), e.Location);
                MainOrientedImageryView.ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
            }
        }

        private void OpenSelectedImagePopupButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedImage = MainOrientedImageryView.ViewModel.SelectedImage;
            if (selectedImage == null)
                return;

            SelectedImagePopupViewer.Popup = CreatePopup(selectedImage);
            SelectedImagePopupBackground.Visibility = Visibility.Visible;
        }

        private static Popup CreatePopup(Mapping.OrientedImage orientedImage)
        {
            var graphic = new Graphic(orientedImage.Geometry);
            foreach (var attribute in orientedImage.Attributes)
            {
                graphic.Attributes[attribute.Key] = attribute.Value;
            }

            return Popup.FromGeoElement(graphic);
        }

        private void SelectedImagePopupBackground_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectedImagePopupBackground.Visibility = Visibility.Collapsed;
            SelectedImagePopupViewer.Popup = null;
        }

        private void Toggle2DButton_Click(object sender, RoutedEventArgs e)
        {
            GeoViewContainer.Children.Clear();
            _currentGeoView.GeoViewTapped -= CurrentGeoView_GeoViewTapped;
            if (_usingMapView)
                _mapView.Map!.OperationalLayers.Clear();
            else
                _sceneView.Scene!.OperationalLayers.Clear();

            _usingMapView = !_usingMapView;
            MainOrientedImageryView.GeoView = _currentGeoView;
            _currentGeoView.GeoViewTapped += CurrentGeoView_GeoViewTapped;
            GeoViewContainer.Children.Add(_currentGeoView);

            if (_oiLayer == null)
                return;
            if (_usingMapView)
                _mapView.Map!.OperationalLayers.Add(_oiLayer);
            else
                _sceneView.Scene!.OperationalLayers.Add(_oiLayer);

            if (_oiLayer?.FullExtent != null)
                _currentGeoView.SetViewpoint(new Viewpoint(_oiLayer.FullExtent));
        }
    }
}
