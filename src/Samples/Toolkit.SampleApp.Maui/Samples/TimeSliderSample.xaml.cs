using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Maui;
using Esri.ArcGISRuntime.Toolkit.UI;
#if WINDOWS
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.TimeExtentChangedEventArgs;
#else
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.Maui.TimeExtentChangedEventArgs;
#endif

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "TimeSlider", Description = "TimeSlider")]
    public partial class TimeSliderSample : ContentPage
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
            mapView.Map = this.Map;
            #if WINDOWS || __IOS__ || __ANDROID__
            slider.CurrentExtentChanged += Slider_CurrentExtentChanged;
            #endif
            LayerSelectionBox.ItemsSource = _namedLayers.Keys.ToList();
            LayerSelectionBox.SelectedIndex = 0;
            _ = HandleSelectionChanged();
            LayerSelectionBox.SelectedIndexChanged += LayerSelectionBox_SelectedIndexChanged;
        }

        #if WINDOWS || __IOS__ || __ANDROID__
        private void Slider_CurrentExtentChanged(object sender, TimeExtentChangedEventArg e)
        {
            mapView.TimeExtent = e.NewExtent;
        }
        #endif

        private void LayerSelectionBox_SelectedIndexChanged(object sender, EventArgs e)
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

        private void StepForward_Click(object sender, EventArgs e)
        {
            try
            {
                var intervals = int.Parse(StepCountBox.Text);
                slider.StepForward(intervals);
            }
            catch (Exception ex)
            {
                this.DisplayAlert("Error", ex.Message, "Ok");
            }
        }

        private void StepBack_Click(object sender, EventArgs e)
        {
            try
            {
                var intervals = int.Parse(StepCountBox.Text);
                slider.StepBack(intervals);
            }
            catch (Exception ex)
            {
                this.DisplayAlert("Error", ex.Message, "Ok");
            }
        }

        private void ConfigureIntervals_Click(object sender, EventArgs e)
        {
            try
            {
                var intervals = int.Parse(IntervalCountBox.Text);
                slider.InitializeTimeSteps(intervals);
            }
            catch (Exception ex)
            {
                this.DisplayAlert("Error", ex.Message, "Ok");
            }
        }
    }
}