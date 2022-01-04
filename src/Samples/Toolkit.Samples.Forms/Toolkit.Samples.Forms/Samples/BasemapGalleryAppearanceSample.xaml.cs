using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "BasemapGallery", Description = "Appearance customization sample")]
    public partial class BasemapGalleryAppearanceSample : ContentPage
    {
        public BasemapGalleryAppearanceSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }

        private void Button_Add_Item(object sender, EventArgs e) => _ = HandleAddItem();

        private async Task HandleAddItem()
        {
            BasemapGalleryItem item = await BasemapGalleryItem.CreateAsync(new Basemap());
            item.Name = "With Thumbnail";
            item.Tooltip = Guid.NewGuid().ToString();
            item.Thumbnail = new Esri.ArcGISRuntime.UI.RuntimeImage(new Uri("https://www.esri.com/content/dam/esrisites/en-us/home/homepage-tile-arcgis-collaboration.jpg"));
            Gallery.AvailableBasemaps.Add(item);

            BasemapGalleryItem item2 = await BasemapGalleryItem.CreateAsync(new Basemap());
            item2.Name = "Without Thumbnail";
            Gallery.AvailableBasemaps.Add(item2);
        }

        private void Button_Remove_Item(object sender, EventArgs e)
        {
            if (Gallery.AvailableBasemaps.Any())
            {
                Gallery.AvailableBasemaps.Remove(Gallery.AvailableBasemaps.Last());
            }
        }
    }
}