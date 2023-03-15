using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Maui;
using Location = Esri.ArcGISRuntime.Location.Location;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "FloorFilter", Description = "Demonstrates FloorFilter with a floor-aware map.")]
    public partial class FloorFilterSample : ContentPage
    {
        private ClickLocationSource _locationSource = new ClickLocationSource();
        public FloorFilterSample()
        {
            InitializeComponent();
            MyMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=b4b599a43a474d33946cf0df526426f5"));
            try
            {
                MyMapView.LocationDisplay.DataSource = _locationSource;
                MyMapView.LocationDisplay.DataSource.StartAsync();
            }
            catch(Exception ex)
            {
                // Ignore - sometimes LocationDisplay is null
            }
            MyMapView.GeoViewTapped += MyMapView_GeoViewTapped;
        }

        private void MyMapView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            if (MyMapView.LocationDisplay != null && MyMapView.LocationDisplay.DataSource != _locationSource)
            {
                MyMapView.LocationDisplay.DataSource = _locationSource;
                MyMapView.LocationDisplay.DataSource.StartAsync();
            }
            if (e.Location != null)
            {
                _locationSource.PushLocation(e.Location, TestTextbox.Text);
            }
        }
    }

    public class ClickLocationSource : LocationDataSource
    {
        private bool _isStarted;
        protected override Task OnStartAsync()
        {
            _isStarted = true;
            return Task.CompletedTask;
        }

        protected override Task OnStopAsync()
        {
            _isStarted = false;
            return Task.CompletedTask;
        }
        public void PushLocation(MapPoint pointLocation, string? floor)
        {
            if (int.TryParse(floor, out int id))
            {
                UpdateLocation(new Location(DateTimeOffset.Now, pointLocation, 50, 60, 0, 0, false, new[] { new KeyValuePair<string, object?>(LocationSourcePropertyKeys.Floor, id) }));
            }
            else if (!string.IsNullOrEmpty(floor))
            {
                UpdateLocation(new Location(DateTimeOffset.Now, pointLocation, 50, 60, 0, 0, false, new[] { new KeyValuePair<string, object?>(LocationSourcePropertyKeys.Floor, floor) }));
            }
            else
            {
                UpdateLocation(new Location(DateTimeOffset.Now, pointLocation, 50, 60, 0, 0, false));
            }
        }
    }
}