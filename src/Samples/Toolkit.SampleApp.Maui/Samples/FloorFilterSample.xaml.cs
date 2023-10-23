namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "FloorFilter", Description = "Demonstrates FloorFilter with a floor-aware map.")]
    public partial class FloorFilterSample : ContentPage
    {
        public FloorFilterSample()
        {
            InitializeComponent();
            MyMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=b4b599a43a474d33946cf0df526426f5"));
        }
    }
}