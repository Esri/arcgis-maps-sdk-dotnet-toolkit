using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BasemapGallerySample : ContentPage
    {
        public BasemapGallerySample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            Gallery.GeoView = MyMapView;
        }

        private void Button_Select_Scene(object sender, EventArgs e)
        {
            MySceneView.IsVisible = true;
            MyMapView.IsVisible = false;
            Gallery.GeoView = MySceneView;
        }

        private void Button_Select_Map(object sender, EventArgs e)
        {
            MyMapView.IsVisible = true;
            MySceneView.IsVisible = false;
            Gallery.GeoView = MyMapView;
        }
        private void Button_Load_Scene(object sender, EventArgs e)
        {
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
        }
        private void Button_Load_Map_WM(object sender, EventArgs e)
        {
            MyMapView.Map = new Map(SpatialReferences.WebMercator);
        }
        private void Button_Load_Map_WGS(object sender, EventArgs e)
        {
            MyMapView.Map = new Map(SpatialReferences.Wgs84);
        }
        private void Button_Add_Item(object sender, EventArgs e)
        {
            BasemapGalleryItem item = new BasemapGalleryItem(new Basemap())
            {
                Name = "My Custom Basemap",
                Tooltip = Guid.NewGuid().ToString(),
                Thumbnail = new Esri.ArcGISRuntime.UI.RuntimeImage(new Uri("https://www.esri.com/content/dam/esrisites/en-us/home/homepage-tile-arcgis-collaboration.jpg"))
            };
            Gallery.Controller.Add(item);
        }
        private void Button_Remove_Item(object sender, EventArgs e)
        {
            Gallery.Controller.Remove(Gallery.Controller.Basemaps.Last());
        }

        private async void Button_Load_AGOL(object sender, EventArgs e)
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

        private async void Button_Load_Portal(object sender, EventArgs e)
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