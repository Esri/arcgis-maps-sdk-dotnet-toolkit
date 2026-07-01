#nullable enable

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
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
        private const string BasemapUri = "https://runtime.maps.arcgis.com/home/item.html?id=d8c5e76fb2cc4bb6955a6783a5f577b7";

        private OrientedImageryLayer? _oiLayer;

        public OrientedImageryView()
        {
            InitializeComponent();
            ConfigureToolbar();

            _ = Initialize();
        }

        private void ConfigureToolbar()
        {
            var toolbarItems = Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryView.GetDefaultToolbarItems();
            toolbarItems.Add(new ExampleOIViewerToolbarVM());
            MainOrientedImageryView.ItemsSource = toolbarItems;

            if (MainOrientedImageryView.ItemTemplateSelector is OrientedImageryViewTemplateSelector selector &&
                FindResource("DemoOilToolbarSelectorItem") is OrientedImageryViewTemplateSelectorItem selectorItem)
            {
                selector.TypeTemplatePairs.Add(selectorItem);
            }
        }

        private async Task Initialize()
        {
            await ApplyLayer(new Uri(LayerUriTextBox.Text));

            MainMapView.Map = new Map(new Uri(BasemapUri));
            MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;
        }

        private async Task ApplyLayer(Uri layerUri)
        {
            var oiLayer = new OrientedImageryLayer(layerUri);
            await oiLayer.LoadAsync();
            if (oiLayer.LoadStatus == LoadStatus.FailedToLoad)
                return;

            _oiLayer = oiLayer;

            if (MainMapView.Map == null)
                MainMapView.Map = new Map(new Uri(BasemapUri));

            MainMapView.Map.OperationalLayers.Clear();
            MainMapView.Map.OperationalLayers.Add(_oiLayer);

            MainOrientedImageryView.OrientedImageryLayer = _oiLayer;
            if (_oiLayer.FullExtent != null)
                MainMapView.SetViewpoint(new Viewpoint(_oiLayer.FullExtent));
        }

        private async void ApplyLayerButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await ApplyLayer(new Uri(LayerUriTextBox.Text));
        }

        private async void MainMapView_GeoViewTapped(object? sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (e.Location == null || _oiLayer == null)
                return;

            // In this case we are choosing to interpret OrientedImageryViewModel.AllowAddingMarkers as mutually exclusive with image searching.
            if (MainOrientedImageryView.ViewModel.AllowAddingMarkers)
            {
                MainOrientedImageryView.ViewModel.AddMarkerLocation(e.Location);
            }
            else
            {
                var parameters = new OrientedImageSearchParameters();
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
    }
}
