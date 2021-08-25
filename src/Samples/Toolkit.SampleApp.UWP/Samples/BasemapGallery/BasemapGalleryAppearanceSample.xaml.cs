using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.BasemapGallery
{
    public sealed partial class BasemapGalleryAppearanceSample : Page
    {
        public BasemapGalleryAppearanceSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
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
        private async void Button_Add_Last(object sender, RoutedEventArgs e)
        {
            BasemapGalleryItem item = await BasemapGalleryItem.CreateAsync(new Basemap());
            item.Name = "With Thumbnail";
            item.Tooltip = Guid.NewGuid().ToString();
            item.Thumbnail = new ArcGISRuntime.UI.RuntimeImage(new Uri("https://www.esri.com/content/dam/esrisites/en-us/home/homepage-tile-arcgis-collaboration.jpg"));
            Gallery.AvailableBasemaps.Add(item);

            BasemapGalleryItem item2 = await BasemapGalleryItem.CreateAsync(new Basemap());
            item2.Name = "Without Thumbnail";
            Gallery.AvailableBasemaps.Add(item2);
        }

        private void Button_Remove_Last(object sender, RoutedEventArgs e)
        {
            if (Gallery.AvailableBasemaps.Any())
            {
                Gallery.AvailableBasemaps.Remove(Gallery.AvailableBasemaps.Last());
            }
        }
    }
}
