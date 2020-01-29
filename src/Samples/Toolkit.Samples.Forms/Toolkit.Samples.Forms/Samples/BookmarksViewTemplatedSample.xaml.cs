using Esri.ArcGISRuntime.Mapping;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BookmarksViewTemplatedSample : ContentPage
    {
        public BookmarksViewTemplatedSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee"));
        }
    }
}