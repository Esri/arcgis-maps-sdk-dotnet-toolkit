using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.BasemapGallery
{
    public sealed partial class BasemapGalleryBehaviorSample : Page
    {
        public BasemapGalleryBehaviorSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
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
            MySceneView.Visibility = Visibility.Collapsed;
            MyMapView.Visibility = Visibility.Visible;
            Gallery.GeoModel = MyMapView.Map;
        }

        private void Button_Switch_To_Scene(object sender, RoutedEventArgs e)
        {
            MyMapView.Visibility = Visibility.Collapsed;
            MySceneView.Visibility = Visibility.Visible;
            Gallery.GeoModel = MySceneView.Scene;
        }

        private void Button_Disconect_View(object sender, RoutedEventArgs e)
        {
            MySceneView.Visibility = Visibility.Collapsed;
            MyMapView.Visibility = Visibility.Collapsed;
            Gallery.GeoModel = null;
        }

        private async void Button_Add_Last(object sender, RoutedEventArgs e)
        {
            BasemapGalleryItem item = await BasemapGalleryItem.CreateAsync(new Basemap(new Uri("https://arcgisruntime.maps.arcgis.com/home/item.html?id=f33a34de3a294590ab48f246e99958c9")));
            item.Name = "With Thumbnail Override";
            item.Tooltip = Guid.NewGuid().ToString();
            item.Thumbnail = new Esri.ArcGISRuntime.UI.RuntimeImage(new Uri("https://www.esri.com/content/dam/esrisites/en-us/home/homepage-tile-arcgis-collaboration.jpg"));
            Gallery.AvailableBasemaps.Add(item);

            BasemapGalleryItem item2 = await BasemapGalleryItem.CreateAsync(new Basemap(new Uri("https://arcgisruntime.maps.arcgis.com/home/item.html?id=4f2e99ba65e34bb8af49733d9778fb8e")));
            item2.Name = "Human Geography Dark";
            Gallery.AvailableBasemaps.Add(item2);
        }

        private async void Button_Add_WGS84(object sender, RoutedEventArgs e)
        {
            BasemapGalleryItem item = await BasemapGalleryItem.CreateAsync(new Basemap(new Uri("https://www.arcgis.com/home/item.html?id=1396c369fa3b44a2a5437f18412f8032")));
            Gallery.AvailableBasemaps.Add(item);
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
            LastSelectedDateLabel.Text = DateTime.Now.ToLongTimeString();
        }
    }
}
