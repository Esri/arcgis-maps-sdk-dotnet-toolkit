using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "BookmarksView", Description = "BookmarksView with custom item templates")]
    public partial class BookmarksViewTemplatedSample : ContentPage
    {
        public BookmarksViewTemplatedSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee"));
        }
    }
}