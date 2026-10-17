#nullable enable

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        private const string ElevationUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

        private List<string> _layerUrls =
        [
            "https://services.arcgis.com/2Pv4ow3pE6NFC9SW/ArcGIS/rest/services/Oakwood_camori_corrected/FeatureServer/0",
            "https://services8.arcgis.com/joda3ARQ9znLf0hf/ArcGIS/rest/services/HopefulHeights_360/FeatureServer/1",
            "https://services5.arcgis.com/N82JbI5EYtAkuUKU/ArcGIS/rest/services/EsriCampus_sequenceTest_attachment/FeatureServer/1"
        ];

        private OrientedImageryViewModel _orientedImageryVM;

        private MapView _mapView;
        private SceneView _sceneView;
        private bool _usingMapView;
        private GeoView _currentGeoView => _usingMapView ? _mapView : _sceneView;

        private ElevationSource _elevationSource;

        private bool _usingAlternateToolbarStyling = false;
        private bool _layersRemoved = false;

        public OrientedImageryView()
        {
            InitializeComponent();

            _mapView = new MapView() { Map = new Map(new Uri(MapBasemap)) };
            _sceneView = new SceneView() { Scene = new Scene(new Uri(SceneBasemap)) };
            _usingMapView = true;
            GeoViewContainer.Children.Add(_currentGeoView);

            _elevationSource = new ArcGISTiledElevationSource(new Uri(ElevationUrl));

            MainOrientedImageryView.GeoView = _currentGeoView;
            _orientedImageryVM = MainOrientedImageryView.ViewModel;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                foreach (var layerUrl in _layerUrls)
                {
                    _mapView.Map!.OperationalLayers.Add(new OrientedImageryLayer(new Uri(layerUrl)));
                    _sceneView.Scene!.OperationalLayers.Add(new OrientedImageryLayer(new Uri(layerUrl))
                    {
                        SceneProperties = new LayerSceneProperties() {  SurfacePlacement = SurfacePlacement.Absolute }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task AddLayer(string layerUrl)
        {
            if (_layerUrls.Contains(layerUrl))
                return;

            try
            {
                _mapView.Map!.OperationalLayers.Add(new OrientedImageryLayer(new Uri(layerUrl)));

                var sceneLayer = new OrientedImageryLayer(new Uri(layerUrl));
                sceneLayer.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                _sceneView.Scene!.OperationalLayers.Add(sceneLayer);

                _layerUrls.Add(layerUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply layer: {ex}");
            }
        }

        private async void AddLayerButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await AddLayer(LayerUriTextBox.Text);
        }

        private GridLength _orientedImageryPanelWidth = new GridLength(2, GridUnitType.Star);

        private void OpenOrientedImageryButton_Click(object sender, RoutedEventArgs e)
        {
            OrientedImageryColumn.Width = _orientedImageryPanelWidth;
            OrientedImageryPanel.Visibility = Visibility.Visible;
            OrientedImagerySplitter.Visibility = Visibility.Visible;
            OpenOrientedImageryButton.Visibility = Visibility.Collapsed;

            MainOrientedImageryView.GeoView ??= _currentGeoView;
        }

        private void CloseOrientedImageryButton_Click(object sender, RoutedEventArgs e)
        {
            _orientedImageryPanelWidth = OrientedImageryColumn.Width;
            OrientedImageryPanel.Visibility = Visibility.Collapsed;
            OrientedImagerySplitter.Visibility = Visibility.Collapsed;
            OrientedImageryColumn.Width = new GridLength(0);
            OpenOrientedImageryButton.Visibility = Visibility.Visible;
            OpenOrientedImageryButton.Focus();

            MainOrientedImageryView.GeoView = null;
        }

        private void OpenSelectedImagePopupButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedImage = _orientedImageryVM.SelectedImage;
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
            _usingMapView = !_usingMapView;
            MainOrientedImageryView.GeoView = _currentGeoView;
            GeoViewContainer.Children.Add(_currentGeoView);
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

        private void ToggleElevationSurfaceTransparencyAndNavigabilityButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sceneView.Scene?.BaseSurface is not Surface surface)
                return;

            if (surface.NavigationConstraint == NavigationConstraint.None)
            {
                surface.NavigationConstraint = NavigationConstraint.StayAbove;
                surface.Opacity = 1;
            }
            else
            {
                surface.NavigationConstraint = NavigationConstraint.None;
                surface.Opacity = 0.7;
            }
        }

        private void ToggleElevationSurfaceExaggerationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sceneView.Scene?.BaseSurface is Surface surface)
                surface.ElevationExaggeration = surface.ElevationExaggeration == 1 ? 3 : 1;
        }

        private int _scenePropertiesState = 0;
        private void ScenePropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainOrientedImageryView.SelectedLayer is not OrientedImageryLayer activeLayer)
                return;

            if (_scenePropertiesState == 0)
            {
                activeLayer.SceneProperties.SurfacePlacement = SurfacePlacement.DrapedFlat;
            }
            else if (_scenePropertiesState == 1)
            {
                activeLayer.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            }
            else if (_scenePropertiesState == 2)
            {
                activeLayer.SceneProperties.AltitudeOffset = 50;
            }
            else if (_scenePropertiesState == 3)
            {
                activeLayer.SceneProperties.SurfacePlacement = SurfacePlacement.Relative;
                activeLayer.SceneProperties.AltitudeOffset = 0;
            }

            _scenePropertiesState = (_scenePropertiesState + 1) % 4;
        }

        private void UpdateToolbarButton_Click(object sender, RoutedEventArgs e)
        {
            _usingAlternateToolbarStyling = !_usingAlternateToolbarStyling;
            var overlaysCategory = _orientedImageryVM.ToolbarCategories.First(c => c.Name == OrientedImageryToolbarCategory.OverlaysCategoryName);
            if (_usingAlternateToolbarStyling)
            {
                overlaysCategory.ToolbarItems.Add(new ShowSelectedImagePopupVM());
                MainOrientedImageryView.ToolbarItemTemplateSelector = (OrientedImageryViewToolbarSelector)this.FindResource("AlternateToolbarSelector");
            }
            else
            {
                var popupToolbarItem = overlaysCategory.ToolbarItems.OfType<ShowSelectedImagePopupVM>().FirstOrDefault();
                if (popupToolbarItem != null)
                    overlaysCategory.ToolbarItems.Remove(popupToolbarItem);
                MainOrientedImageryView.ToolbarItemTemplateSelector = (OrientedImageryViewToolbarSelector)this.FindResource("DefaultOrientedImageryToolbarSelector");
            }
        }

        private void OverrideClickHandlersButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainOrientedImageryView.OnGeoViewTappedOverride == null)
                MainOrientedImageryView.OnGeoViewTappedOverride = OnGeoViewTappedOverride;
            else
                MainOrientedImageryView.OnGeoViewTappedOverride = null;

            if (MainOrientedImageryView.OnImageTappedOverride == null)
                MainOrientedImageryView.OnImageTappedOverride = OnImageTappedOverride;
            else
                MainOrientedImageryView.OnImageTappedOverride = null;
        }

        private async void OnGeoViewTappedOverride(object? sender, GeoViewInputEventArgs e)
        {
            if (e.Location == null)
                return;

            var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.Green, 20);
            _orientedImageryVM.AddMarkerLocation(e.Location, symbol);
        }

        private async void OnImageTappedOverride(object? sender, OrientedImageTappedEventArgs e)
        {
            try
            {
                var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
                var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.Pink, 20);
                _orientedImageryVM.AddMarkerLocation(location, symbol);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error converting image point to location: {ex.Message}");
            }
        }

        private void RemoveReAddLayers_Click(object sender, RoutedEventArgs e)
        {
            if (_layersRemoved)
            {
                foreach (var layerUrl in _layerUrls)
                {
                    _mapView.Map!.OperationalLayers.Add(new OrientedImageryLayer(new Uri(layerUrl)));
                    _sceneView.Scene!.OperationalLayers.Add(new OrientedImageryLayer(new Uri(layerUrl)));
                }
            }
            else
            {
                _mapView.Map!.OperationalLayers.Clear();
                _sceneView.Scene!.OperationalLayers.Clear();
            }
            _layersRemoved = !_layersRemoved;
        }

        private void RemoveLastLayer_Click(object sender, RoutedEventArgs e)
        {
            _mapView.Map!.OperationalLayers.RemoveAt(_mapView.Map.OperationalLayers.Count - 1);
            _sceneView.Scene!.OperationalLayers.RemoveAt(_sceneView.Scene.OperationalLayers.Count - 1);
        }

        private async void BackgroundThreadUpdate_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                foreach (var marker in _orientedImageryVM.Markers)
                {
                    marker.IsVisible = !marker.IsVisible;
                }
            });
        }

        private async void ToggleButtonsBackgroundThread_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                _orientedImageryVM.ImageSearchEnabled = !_orientedImageryVM.ImageSearchEnabled;
                _orientedImageryVM.ShowCameraLocations = !_orientedImageryVM.ShowCameraLocations;
                _orientedImageryVM.ShowUnselectedFootprints = !_orientedImageryVM.ShowUnselectedFootprints;
            });
        }

        private void ToggleLayerDropdownButton_Click(object sender, RoutedEventArgs e)
        {
            MainOrientedImageryView.IsLayerSelectionDropdownEnabled = !MainOrientedImageryView.IsLayerSelectionDropdownEnabled;
        }
    }

    public class ShowSelectedImagePopupVM : OrientedImageryToolbarItemBase
    { }
}
