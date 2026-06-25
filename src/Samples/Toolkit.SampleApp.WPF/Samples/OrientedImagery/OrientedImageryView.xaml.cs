using Esri.ArcGISRuntime.Geometry;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Mapping;
using System.Collections.ObjectModel;

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

            _oiLayer = new OrientedImageryLayer(new Uri("https://arcgis.com/"));
            _ = Initialize();
        }

        private async Task Initialize()
        {
            MainMapView.Map = new Mapping.Map(SpatialReferences.Wgs84);
            MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;
            MainOrientedImageryView.ViewModel.SelectedImage = new Mapping.OrientedImage();
            MainOrientedImageryView.OrientedImageryLayer = _oiLayer;
        }

        private async void MainMapView_GeoViewTapped(object sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            var parameters = new OrientedImageSearchParameters();
            var images = await _oiLayer.SearchImagesAsync(e.Location, parameters);
            Collection<OrientedImage> manualImages = [new OrientedImage(), new OrientedImage(), new OrientedImage()];
            MainOrientedImageryView.ViewModel.SetImages(manualImages, e.Location);
            MainOrientedImageryView.ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
        }
    }
}
