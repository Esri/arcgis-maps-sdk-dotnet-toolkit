using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [Activity(Label = "TimeSlider", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "TimeSlider", Description = "TimeSlider used with a MapView")]
    public class TimeSliderSample : Activity
    {
        private Map _map { get; } = new Map(Basemap.CreateLightGrayCanvas());

        private Dictionary<string, Uri> _namedLayers = new Dictionary<string, Uri>
        {
            {"Hurricanes", new Uri("https://services.arcgis.com/XSeYKQzfXnEgju9o/ArcGIS/rest/services/Hurricanes_1950_to_2015/FeatureServer/0") },
            {"Human Life Expectancy", new Uri("https://services1.arcgis.com/VAI453sU9tG9rSmh/arcgis/rest/services/WorldGeo_HumanCulture_LifeExpectancy_features/FeatureServer/0") }
        };

        private MapView _mapView;
        private TimeSlider _timeSlider;
        private Button _stepForwardButton;
        private Button _stepBackButton;
        private Button _stepIntervalButton;
        private Button _layerButton;
        private TextView _statusLabel;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.TimeSliderSample);
            _mapView = FindViewById<MapView>(Resource.Id.mapView);
            _timeSlider = FindViewById<TimeSlider>(Resource.Id.timeSlider);
            _stepForwardButton = FindViewById<Button>(Resource.Id.stepForwardButton);
            _stepBackButton = FindViewById<Button>(Resource.Id.stepBackButton);
            _stepIntervalButton = FindViewById<Button>(Resource.Id.stepIntervalButton);
            _layerButton = FindViewById<Button>(Resource.Id.layerButton);
            _statusLabel = FindViewById<TextView>(Resource.Id.statusLabel);

            _stepForwardButton.Click += StepForward_Click;
            _stepBackButton.Click += StepBack_Click;
            _stepIntervalButton.Click += StepInterval_Click;
            _layerButton.Click += LayerButton_Click;

            _mapView.Map = _map;
            _timeSlider.CurrentExtentChanged += Slider_CurrentExtentChanged;
            _statusLabel.Text = "No layer selected";
        }

        private void Slider_CurrentExtentChanged(object sender, Esri.ArcGISRuntime.Toolkit.UI.TimeExtentChangedEventArgs e)
        {
            _mapView.TimeExtent = e.NewExtent;
        }

        private void StepForward_Click(object sender, EventArgs e)
        {
            GetInput("Step forward", "How many steps? Leave blank for one.", (input) =>
            {
                if (int.TryParse(input, out int steps))
                {
                    _timeSlider.StepForward(steps);
                }
                else
                {
                    _timeSlider.StepForward();
                }
            });
        }

        private void StepBack_Click(object sender, EventArgs e)
        {
            GetInput("Step back", "How many steps? Leave blank for one.", (input) =>
            {
                if (int.TryParse(input, out int steps))
                {
                    _timeSlider.StepBack(steps);
                }
                else
                {
                    _timeSlider.StepBack();
                }
            });
        }

        private void StepInterval_Click(object sender, EventArgs e)
        {
            GetInput("Set Interval Count", "How many intervals?", (input) =>
            {
                if (int.TryParse(input, out int steps))
                {
                    _timeSlider.InitializeTimeSteps(steps);
                }
                else
                {
                    ShowMessage("Invalid input", "Couldn't parse integer");
                }
            });
        }

        private void LayerButton_Click(object sender, EventArgs e)
        {
            StartLayerSelection();
        }

        private async Task HandleSelectionChanged(string key)
        {
            _map.OperationalLayers.Clear();
            var layer = new FeatureLayer(_namedLayers[key]);
            _map.OperationalLayers.Add(layer);
            await _timeSlider.InitializeTimePropertiesAsync(layer);

            _statusLabel.Text = layer.SupportsTimeFiltering ? "Time supported" : "No time support";
        }

        // Function shows an alert to get input, then calls callback with value
        private void GetInput(string title, string message, Action<string> callback)
        {
            var alert = new AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(message);

            var input = new EditText(this);
            alert.SetView(input);

            alert.SetPositiveButton("Ok", (senderAlert, args) =>
            {
                callback(input.Text);
            });

            alert.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                callback(null);
            });

            alert.Show();
        }


        // Show alert to choose from a list of options, and call callback with value
        private void StartLayerSelection()
        {
            var alert = new AlertDialog.Builder(this);
            alert.SetTitle("Choose layer to display");

            var keys = _namedLayers.Keys.ToList();

            // Add list of options to alert
            alert.SetSingleChoiceItems(keys.ToArray(), 0, (senderAlert, args) =>
            {
                _ = HandleSelectionChanged(keys[args.Which]);
            });

            alert.SetPositiveButton("Ok", (IDialogInterfaceOnClickListener)null);

            alert.Show();
        }

        private void ShowMessage(string message, string title)
        {
            // Display the message to the user.
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(message).SetTitle(title).Show();
        }
    }
}