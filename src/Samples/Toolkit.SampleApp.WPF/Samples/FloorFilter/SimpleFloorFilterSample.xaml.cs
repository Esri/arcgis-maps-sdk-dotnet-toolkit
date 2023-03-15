
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.FloorFilter
{
    public partial class SimpleFloorFilterSample: UserControl
    {
        private ClickLocationSource _locationSource = new ClickLocationSource();
        public SimpleFloorFilterSample()
        {
            InitializeComponent();
            MyMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=b4b599a43a474d33946cf0df526426f5"));
            MyMapView.LocationDisplay.DataSource = _locationSource;
            MyMapView.GeoViewTapped += MyMapView_GeoViewTapped;
            MyMapView.LocationDisplay.DataSource.StartAsync();
        }

        private void MyMapView_GeoViewTapped(object sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if(e.Location != null)
            _locationSource.PushLocation(e.Location, TestTextbox.Text);
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
                UpdateLocation(new Location.Location(DateTimeOffset.Now, pointLocation, 50, 60, 0, 0, false, new[] { new KeyValuePair<string, object>(LocationSourcePropertyKeys.Floor, id) }));
            }
            else if (!string.IsNullOrEmpty(floor))
            {
                UpdateLocation(new Location.Location(DateTimeOffset.Now, pointLocation, 50, 60, 0, 0, false, new[] { new KeyValuePair<string,object>(LocationSourcePropertyKeys.Floor, floor) }));
            }
            else
            {
                UpdateLocation(new Location.Location(DateTimeOffset.Now, pointLocation, 50, 60, 0, 0, false));
            }
        }
    }
}