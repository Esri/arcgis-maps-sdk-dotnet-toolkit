using Esri.ArcGISRuntime.Mapping;
using System;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Bookmarks
{
    /// <summary>
    /// Interaction logic for MapBookmarksSample.xaml
    /// </summary>
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks - MapView", Description = "Bookmarks driven by a mapview with custom item template.")]
    public partial class MapBookmarksSample : UserControl
    {
        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";
        public MapBookmarksSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(new Uri(_mapUrl));
        }
    }
}
