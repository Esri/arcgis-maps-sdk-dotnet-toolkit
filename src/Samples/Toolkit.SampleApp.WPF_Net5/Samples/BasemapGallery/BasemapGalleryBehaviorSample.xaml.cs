using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.BasemapGallery
{
    [SampleInfoAttribute(Category = "BasemapGallery", DisplayName = "BasemapGallery - Behavior", Description = "Sample showing behaviors")]
    public partial class BasemapGalleryBehaviorSample : UserControl, INotifyPropertyChanged
    {
        private GeoView _selectedGeoView;
        private MapView _mapView = new MapView();
        private SceneView _sceneView = new SceneView();

        public event PropertyChangedEventHandler PropertyChanged;

        public GeoView SelectedGeoView
        {
            get => _selectedGeoView;
            set
            {
                if (_selectedGeoView != value)
                {
                    _selectedGeoView = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedGeoView)));
                    if (_selectedGeoView is MapView mv)
                    {
                        Gallery.GeoModel = mv.Map;
                    }
                    else if (_selectedGeoView is SceneView sv)
                    {
                        Gallery.GeoModel = sv.Scene;
                    }
                    else
                    {
                        Gallery.GeoModel = null;
                    }
                }
            }
        }

        public BasemapGalleryBehaviorSample()
        {
            InitializeComponent();
            this.DataContext = this;
            _mapView.Map = new Map(BasemapStyle.ArcGISImagery);
            _sceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
            ViewStyleCombobox.Items.Add("List");
            ViewStyleCombobox.Items.Add("Grid");
            ViewStyleCombobox.SelectedIndex = 0;
        }

        private void ViewStyleCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ViewStyleCombobox.SelectedIndex)
            {
                case 0:
                    Gallery.GalleryViewStyle = BasemapGalleryViewStyle.List;
                    break;
                case 1:
                    Gallery.GalleryViewStyle = BasemapGalleryViewStyle.Grid;
                    break;
            }
        }

        private async void Button_Load_Portal(object sender, RoutedEventArgs e)
        {
            try
            {
                Gallery.Portal = await Portal.ArcGISPortal.CreateAsync(new Uri("https://arcgisruntime.maps.arcgis.com/"));
            }
            catch (Exception)
            {

            }
        }

        private async void Button_Load_AGOL(object sender, RoutedEventArgs e)
        {
            try
            {
                Gallery.Portal = await Portal.ArcGISPortal.CreateAsync();
            }
            catch (Exception) { }
        }

        private void Button_Switch_To_Map(object sender, RoutedEventArgs e)
        {
            SelectedGeoView = _mapView;
        }

        private void Button_Switch_To_Scene(object sender, RoutedEventArgs e)
        {
            SelectedGeoView = _sceneView;
        }

        private void Button_Disconect_View(object sender, RoutedEventArgs e)
        {
            SelectedGeoView = null;
        }

        private void Button_Add_Last(object sender, RoutedEventArgs e)
        {
            BasemapGalleryItem item = new BasemapGalleryItem(new Basemap())
            {
                Name = "With Thumbnail",
                Tooltip = Guid.NewGuid().ToString(),
                Thumbnail = new ArcGISRuntime.UI.RuntimeImage(new Uri("https://www.esri.com/content/dam/esrisites/en-us/home/homepage-tile-arcgis-collaboration.jpg"))
            };
            Gallery.AvailableBasemaps.Add(item);

            BasemapGalleryItem item2 = new BasemapGalleryItem(new Basemap())
            {
                Name = "Without Thumbnail"
            };
            Gallery.AvailableBasemaps.Add(item2);
        }

        private void Button_Remove_Last(object sender, RoutedEventArgs e)
        {
            if (Gallery.AvailableBasemaps.Any())
            {
                Gallery.AvailableBasemaps.Remove(Gallery.AvailableBasemaps.Last());
            }
        }

        private void Gallery_BasemapSelected(object sender, BasemapGalleryItem e)
        {
            LastSelectedDateLabel.Content = DateTime.Now.ToLongTimeString();
        }
    }
}
