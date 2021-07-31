using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples.BasemapGallery
{
    /// <summary>
    /// Interaction logic for BasemapGallerySample.xaml
    /// </summary>
    [SampleInfoAttribute(Category = "BasemapGallery", DisplayName = "BasemapGallery", Description = "Full BasemapGallery scenario")]
    public partial class BasemapGallerySample : UserControl
    {
        public BasemapGallerySample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
            Gallery.GeoView = MyMapView;
            ViewStyleCombobox.Items.Add("Auto");
            ViewStyleCombobox.Items.Add("List");
            ViewStyleCombobox.Items.Add("Grid");
            ViewStyleCombobox.SelectedIndex = 0;
        }

        private void ViewStyleCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ViewStyleCombobox.SelectedIndex)
            {
                case 0:
                    Gallery.GalleryViewStyle = BasemapGalleryViewStyle.Automatic;
                    break;
                case 1:
                    Gallery.GalleryViewStyle = BasemapGalleryViewStyle.List;
                    break;
                case 2:
                    Gallery.GalleryViewStyle = BasemapGalleryViewStyle.Grid;
                    break;
            }
        }

        private void Button_Select_Map(object sender, RoutedEventArgs e)
        {
            Gallery.GeoView = MyMapView;
            MyMapView.Visibility = Visibility.Visible;
            MySceneView.Visibility = Visibility.Collapsed;
        }

        private void Button_Select_Scene(object sender, RoutedEventArgs e)
        {
            Gallery.GeoView = MySceneView;
            MySceneView.Visibility = Visibility.Visible;
            MyMapView.Visibility = Visibility.Collapsed;
        }

        private void Button_Load_Map_WM(object sender, RoutedEventArgs e)
        {
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }

        private void Button_Load_Map_WGS(object sender, RoutedEventArgs e)
        {
            MyMapView.Map = new Map(SpatialReferences.Wgs84);
        }

        private void Button_Load_Scene(object sender, RoutedEventArgs e)
        {
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
        }

        private void Button_Add_Last(object sender, RoutedEventArgs e)
        {
            BasemapGalleryItem item = new BasemapGalleryItem(new Basemap())
            {
                Name = "My Custom Basemap",
                Tooltip = Guid.NewGuid().ToString(),
                Thumbnail = new ArcGISRuntime.UI.RuntimeImage(new Uri("https://www.esri.com/content/dam/esrisites/en-us/home/homepage-tile-arcgis-collaboration.jpg"))
            };
            Gallery.Controller.Add(item);
        }

        private void Button_Remove_Last(object sender, RoutedEventArgs e)
        {
            Gallery.Controller.Remove(Gallery.Controller.Basemaps.Last());
        }

        private async void Button_Load_AGOL(object sender, RoutedEventArgs e)
        {
            try
            {
                Gallery.Portal = await ArcGISPortal.CreateAsync();
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private async void Button_Load_Portal(object sender, RoutedEventArgs e)
        {
            try
            {
                Gallery.Portal = await ArcGISPortal.CreateAsync(new Uri("https://arcgisruntimesdk.maps.arcgis.com/"));
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }
}
