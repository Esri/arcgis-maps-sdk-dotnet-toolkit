using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using System;
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
        private OrientedImageryLayer _oiLayer;

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

            MainMapView.Map = new Mapping.Map(BasemapStyle.ArcGISTopographic);
            MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;
        }

        private async Task ApplyLayer(Uri layerUri)
        {
            _oiLayer = new OrientedImageryLayer(layerUri);
            await _oiLayer.LoadAsync();

            MainMapView.Map ??= new Mapping.Map(BasemapStyle.ArcGISTopographic);
            MainMapView.Map.OperationalLayers.Clear();
            MainMapView.Map.OperationalLayers.Add(_oiLayer);

            MainOrientedImageryView.OrientedImageryLayer = _oiLayer;
            MainMapView.SetViewpoint(new Viewpoint(_oiLayer.FullExtent));
        }

        private async void ApplyLayerButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await ApplyLayer(new Uri(LayerUriTextBox.Text));
        }

        private async void MainMapView_GeoViewTapped(object sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (e.Location == null)
                return;

            // In this case we are choosing to interpret OrientedImageryViewModel.AllowAddingMarkers as mutually exclusive with image searching.
            if (!MainOrientedImageryView.ViewModel.AllowAddingMarkers)
            {
                var parameters = new OrientedImageSearchParameters();
                var images = await _oiLayer.SearchImagesAsync(e.Location, parameters);
                MainOrientedImageryView.ViewModel.SetImages(images.ToList(), e.Location);
                MainOrientedImageryView.ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
            }
            else
            {
                MainOrientedImageryView.ViewModel.AddMarkerLocation(e.Location);
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
