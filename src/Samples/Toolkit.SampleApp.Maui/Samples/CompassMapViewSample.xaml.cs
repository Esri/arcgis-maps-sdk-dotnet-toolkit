using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "Compass", Description = "Compass with MapView sample")]
    public partial class CompassMapViewSample : ContentPage
    {
        public CompassMapViewSample()
        {
            InitializeComponent();
            mapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
        }

        private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            compass.WidthRequest = compass.HeightRequest = e.NewValue;
        }
    }
}