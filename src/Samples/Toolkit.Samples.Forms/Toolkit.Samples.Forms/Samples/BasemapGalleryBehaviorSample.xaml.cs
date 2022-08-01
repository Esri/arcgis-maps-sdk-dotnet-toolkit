using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "BasemapGallery", Description = "Exercises various bindings, properties, and interaction behaviors")]
    public partial class BasemapGalleryBehaviorSample : ContentPage
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

        private void ViewStyleCombobox_SelectedIndexChanged(object sender, EventArgs e)
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

        private async void Button_Load_Portal(object sender, EventArgs e)
        {
            try
            {
                Gallery.Portal = await Esri.ArcGISRuntime.Portal.ArcGISPortal.CreateAsync(new Uri("https://arcgisruntime.maps.arcgis.com/"));
            }
            catch (Exception)
            {

            }
        }

        private async void Button_Load_AGOL(object sender, EventArgs e)
        {
            try
            {
                Gallery.Portal = await Esri.ArcGISRuntime.Portal.ArcGISPortal.CreateAsync();
            }
            catch (Exception) { }
        }

        private void Button_Switch_To_Map(object sender, EventArgs e)
        {
            MySceneView.IsVisible = false;
            MyMapView.IsVisible = true;
            Gallery.GeoModel = MyMapView.Map;
        }

        private void Button_Switch_To_Scene(object sender, EventArgs e)
        {
            MyMapView.IsVisible = false;
            MySceneView.IsVisible = true;
            Gallery.GeoModel = MySceneView.Scene;
        }

        private void Button_Disconect_View(object sender, EventArgs e)
        {
            MySceneView.IsVisible = false;
            MyMapView.IsVisible = false;
            Gallery.GeoModel = null;
        }

        private async void Button_Add_Last(object sender, EventArgs e)
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

        private async void Button_Add_WGS84(object sender, EventArgs e)
        {
            BasemapGalleryItem item = await BasemapGalleryItem.CreateAsync(new Basemap(new Uri("https://www.arcgis.com/home/item.html?id=1396c369fa3b44a2a5437f18412f8032")));
            Gallery.AvailableBasemaps.Add(item);
        }

        private void Button_Remove_Last(object sender, EventArgs e)
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