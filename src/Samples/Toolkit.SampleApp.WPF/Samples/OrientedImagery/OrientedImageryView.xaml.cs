using Esri.ArcGISRuntime.Geometry;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Mapping;
using System.Collections.ObjectModel;
using System.Linq;

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
