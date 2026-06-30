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

        private bool _toolbarIsUsingDefaultStyle = true;

        public OrientedImageryView()
        {
            InitializeComponent();

            _ = Initialize();
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

            MainOrientedImageryView.ViewModel.SelectedImage = new Mapping.OrientedImage();
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

        private void ToggleToolbarStyleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_toolbarIsUsingDefaultStyle)
            {
                MainOrientedImageryView.ItemsSource = new ObservableCollection<object>
                {
                    new AutoUpdateFootprintVM(),
                    new ShowSelectedFootprintVM(),
                    new ExampleOIViewerToolbarVM()
                };

                var selector = MainOrientedImageryView.ItemTemplateSelector as OrientedImageryViewTemplateSelector;
                selector.TypeTemplatePairs.Add(FindResource("DemoOilToolbarSelectorItem") as OrientedImageryViewTemplateSelectorItem);
                selector.TypeTemplatePairs.Add(FindResource("AlternateAutoUpdateFootprintSelectorItem") as OrientedImageryViewTemplateSelectorItem);
            }
            else
            {
                MainOrientedImageryView.ItemsSource = Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryView.GetDefaultToolbarItems();
            }
            _toolbarIsUsingDefaultStyle = !_toolbarIsUsingDefaultStyle;
        }
    }
}
