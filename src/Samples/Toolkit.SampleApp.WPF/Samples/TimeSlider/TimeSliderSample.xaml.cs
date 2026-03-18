using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.TimeSlider
{
    public partial class TimeSliderSample : UserControl
    {
        public Map Map { get; } = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));

        private Dictionary<string, ITimeAware> _namedLayers = new()
        {
            {"Sentinel-2 Land Cover", new RasterLayer(new ImageServiceRaster(new Uri("https://ic.imagery1.arcgis.com/arcgis/rest/services/Sentinel2_10m_LandCover/ImageServer"))) },
            {"Earthquakes", new FeatureLayer(new Uri("https://services9.arcgis.com/RHVPKKiFTONKtxq3/arcgis/rest/services/Historical_Quakes/FeatureServer/0")) }
        };

        public TimeSliderSample()
        {
            InitializeComponent();
            slider.CurrentExtentChanged += Slider_CurrentExtentChanged;
            this.DataContext = this;
            LayerSelectionBox.ItemsSource = _namedLayers.Keys;
            LayerSelectionBox.SelectedIndex = 0;
            _ = HandleSelectionChanged();
            LayerSelectionBox.SelectionChanged += LayerSelectionBox_SelectionChanged;
        }

        private void Slider_CurrentExtentChanged(object sender, UI.TimeExtentChangedEventArgs e)
        {
            mapView.TimeExtent = e.NewExtent;
        }

        private void LayerSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = HandleSelectionChanged();
        }

        private async Task HandleSelectionChanged()
        {
            Map.OperationalLayers.Clear();
            var selectedLayer = LayerSelectionBox.SelectedItem.ToString();

            var layer = _namedLayers[selectedLayer];
            Map.OperationalLayers.Add(layer as Layer);
            await slider.InitializeTimePropertiesAsync(layer);

            IsTimeAwareLabel.Text = layer.SupportsTimeFiltering ? "Yes" : "No";
        }

        private void StepForward_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var intervals = int.Parse(StepCountBox.Text);
                slider.StepForward(intervals);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StepBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var intervals = int.Parse(StepCountBox.Text);
                slider.StepBack(intervals);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ConfigureIntervals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var intervals = int.Parse(IntervalCountBox.Text);
                slider.InitializeTimeSteps(intervals);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
