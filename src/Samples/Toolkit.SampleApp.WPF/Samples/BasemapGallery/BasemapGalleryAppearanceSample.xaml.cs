using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.BasemapGallery
{
    [SampleInfoAttribute(Category = "BasemapGallery", DisplayName = "BasemapGallery - Appearance", Description = "Sample showing customization options related to appearance")]
    public partial class BasemapGalleryAppearanceSample : UserControl
    {

        private ObservableCollection<BasemapGalleryItem> _availableBasemaps;

        public ObservableCollection<BasemapGalleryItem> AvailableBasemaps
        {
            get => _availableBasemaps;
            set => _availableBasemaps = value;
        }

        public BasemapGalleryAppearanceSample()
        {
            ArcGISRuntimeEnvironment.ApiKey = "AAPK8a018df60304448aaeebc83f9fd93c34v69aknvg5zkiVUQ_QK5NSdpQzT08ZmU_hHoCNYWTaFdXcsnOB9zUGDcAp8po7357";

            InitializeComponent();

            MainMapView.Map = new Map(SpatialReference.Create(25833));

            MainMapView.LayerViewStateChanged += MainMapView_LayerViewStateChanged;

            Load();
      
        }

        private void MainMapView_LayerViewStateChanged(object sender, LayerViewStateChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Layer.Name + ": " + e.LayerViewState.ToString());    
        }

        private async void Load()
        {
            var bm1 = new Basemap() { Name = "GeocacheBasis" };
            bm1.BaseLayers.Add(new ArcGISVectorTiledLayer(new Uri("https://services.geodataonline.no/arcgis/rest/services/GeocacheVector/GeocacheBasis/VectorTileServer")));

            var bm2 = new Basemap() { Name = "GeocacheGraatone" };
            bm2.BaseLayers.Add(new ArcGISVectorTiledLayer(new Uri("https://services.geodataonline.no/arcgis/rest/services/GeocacheVector/GeocacheGraatone/VectorTileServer")));

            MainMapView.Map.Basemap= bm1;   
            var gi1 = await BasemapGalleryItem.CreateAsync(bm1);
            var gi2 = await BasemapGalleryItem.CreateAsync(bm2);

            AvailableBasemaps = new ObservableCollection<BasemapGalleryItem>
            {
                gi1, gi2
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BasemapGallery.AvailableBasemaps.Clear();
            foreach (var basemap in AvailableBasemaps) BasemapGallery.AvailableBasemaps.Add(basemap);
        }
    }
}