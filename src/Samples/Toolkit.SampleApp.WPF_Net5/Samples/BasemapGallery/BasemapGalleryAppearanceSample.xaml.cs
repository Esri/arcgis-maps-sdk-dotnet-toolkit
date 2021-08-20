﻿using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.BasemapGallery
{
    [SampleInfoAttribute(Category = "BasemapGallery", DisplayName = "BasemapGallery - Appearance", Description = "Sample showing customization options related to appearance")]
    public partial class BasemapGalleryAppearanceSample : UserControl
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
    }
}
