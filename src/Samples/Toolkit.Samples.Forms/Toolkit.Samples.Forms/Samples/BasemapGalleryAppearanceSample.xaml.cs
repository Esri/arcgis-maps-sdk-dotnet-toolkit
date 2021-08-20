using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "BasemapGallery", Description = "Configure various appearance properties, including list and grid view options.")]
    public partial class BasemapGalleryAppearanceSample : ContentPage
    {
        public BasemapGalleryAppearanceSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }

        private void Button_Add_Item(object sender, EventArgs e)
        {
            BasemapGalleryItem item = new BasemapGalleryItem(new Basemap())
            {
                Name = "With Thumbnail",
                Tooltip = Guid.NewGuid().ToString(),
                Thumbnail = new Esri.ArcGISRuntime.UI.RuntimeImage(new Uri("https://www.esri.com/content/dam/esrisites/en-us/home/homepage-tile-arcgis-collaboration.jpg"))
            };
            Gallery.AvailableBasemaps.Add(item);

            BasemapGalleryItem item2 = new BasemapGalleryItem(new Basemap())
            {
                Name = "Without Thumbnail"
            };
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