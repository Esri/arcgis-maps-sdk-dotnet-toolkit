using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
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

        private void Button_Add_Last(object sender, RoutedEventArgs e) => _ = HandleAddLast();

        private async Task HandleAddLast()
        {
            try
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
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message).ShowAsync();
            }
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
