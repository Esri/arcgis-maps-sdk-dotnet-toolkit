using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.TimeSlider
{
    public sealed partial class TimeSliderSample : Page
    {
        public Map Map { get; } = new Map(Basemap.CreateLightGrayCanvas());

        private Dictionary<string, Uri> _namedLayers = new Dictionary<string, Uri>
        {
            {"Hurricanes", new Uri("https://services.arcgis.com/XSeYKQzfXnEgju9o/ArcGIS/rest/services/Hurricanes_1950_to_2015/FeatureServer/0") },
            {"Human Life Expectancy", new Uri("https://services1.arcgis.com/VAI453sU9tG9rSmh/arcgis/rest/services/WorldGeo_HumanCulture_LifeExpectancy_features/FeatureServer/0") }
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

            var layer = new FeatureLayer(_namedLayers[selectedLayer]);
            Map.OperationalLayers.Add(layer);
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
                _ = new MessageDialog(ex.Message).ShowAsync();
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
                _ = new MessageDialog(ex.Message).ShowAsync();
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
                _ = new MessageDialog(ex.Message).ShowAsync();
            }
        }
    }
}
