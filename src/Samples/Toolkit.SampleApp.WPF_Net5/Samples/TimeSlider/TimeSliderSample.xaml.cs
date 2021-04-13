using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples.TimeSlider
{
    /// <summary>
    /// Interaction logic for TimeSliderSample.xaml
    /// </summary>
    public partial class TimeSliderSample : UserControl
    {
        public TimeSliderSample()
        {
            InitializeComponent();
            //var layer = new FeatureLayer(new Uri("https://services.arcgis.com/XSeYKQzfXnEgju9o/ArcGIS/rest/services/Hurricanes_1950_to_2015/FeatureServer/0"));
            var layer = new FeatureLayer(new Uri("http://services1.arcgis.com/VAI453sU9tG9rSmh/arcgis/rest/services/WorldGeo_HumanCulture_LifeExpectancy_features/FeatureServer/0"));
            Map.OperationalLayers.Add(layer);
            this.DataContext = this;
            InitSlider(layer);
        }

        private async void InitSlider(FeatureLayer layer)
        {
            await layer.LoadAsync();
            if(layer.IsTimeFilteringEnabled)
            {
                //Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider.
                await slider.InitializeTimePropertiesAsync(layer);
            }
        }

        public Map Map { get; } = new Map(Basemap.CreateLightGrayCanvas());
    }
}
