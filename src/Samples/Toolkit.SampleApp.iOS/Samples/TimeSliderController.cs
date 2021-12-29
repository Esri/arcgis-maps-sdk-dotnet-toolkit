using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "TimeSlider", DisplayName = "TimeSlider", Description = "TimeSlider used with a MapView")]
    public partial class TimeSliderViewController : UIViewController
    {
        MapView _mapView;
        TimeSlider _timeSlider;
        UISegmentedControl _layerSegment;
        Map _map = new Map(Basemap.CreateLightGrayCanvas());
        UIBarButtonItem _stepForwardButton;
        UIBarButtonItem _stepBackButton;
        UIBarButtonItem _stepCountButton;
        UIAlertController _textInputAlertController;
        UILabel _statusLabel;

        private Dictionary<string, Uri> _namedLayers = new Dictionary<string, Uri>
        {
            {"Hurricanes", new Uri("https://services.arcgis.com/XSeYKQzfXnEgju9o/ArcGIS/rest/services/Hurricanes_1950_to_2015/FeatureServer/0") },
            {"Human Life Expectancy", new Uri("http://services1.arcgis.com/VAI453sU9tG9rSmh/arcgis/rest/services/WorldGeo_HumanCulture_LifeExpectancy_features/FeatureServer/0") }
        };

        private async Task HandleSelectionChanged()
        {
            _map.OperationalLayers.Clear();
            var layers = _namedLayers.Keys.ToList();
            var selectedLayer = _namedLayers[layers.ElementAt((int)_layerSegment.SelectedSegment)];

            var layer = new FeatureLayer(selectedLayer);
            _map.OperationalLayers.Add(layer);
            await _timeSlider.InitializeTimePropertiesAsync(layer);

            _statusLabel.Text = layer.SupportsTimeFiltering ? "Time supported for current layer." : "Time not supported for current layer.";
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = UIColor.SystemBackgroundColor;

            _mapView = new MapView()
            {
                Map = _map,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _timeSlider = new TimeSlider
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _layerSegment = new UISegmentedControl
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            foreach(var layer in _namedLayers.Keys.Reverse())
            {
                _layerSegment.InsertSegment(layer, 0, false);
            }

            var toolbar = new UIToolbar { TranslatesAutoresizingMaskIntoConstraints = false };

            _stepCountButton = new UIBarButtonItem("Adjust step count", null);
            _stepForwardButton = new UIBarButtonItem(UIBarButtonSystemItem.FastForward);
            _stepBackButton = new UIBarButtonItem(UIBarButtonSystemItem.Rewind);

            toolbar.Items = new[]
            {
                _stepCountButton,
                new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                _stepBackButton,
                _stepForwardButton
            };

            _statusLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = UIColor.FromWhiteAlpha(0f, 0.6f),
                TextColor = UIColor.White,
                TextAlignment = UITextAlignment.Center,
                Text = "Select a layer to begin.",
                Lines = 1,
                AdjustsFontSizeToFitWidth = true
            };

            View.AddSubviews(_mapView, _timeSlider, toolbar, _layerSegment, _statusLabel);

            NSLayoutConstraint.ActivateConstraints(new []
            {
                _mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _mapView.BottomAnchor.ConstraintEqualTo(_timeSlider.TopAnchor),
                _layerSegment.CenterXAnchor.ConstraintEqualTo(_mapView.CenterXAnchor),
                _layerSegment.TopAnchor.ConstraintEqualTo(_statusLabel.BottomAnchor, 8),
                _timeSlider.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor),
                _timeSlider.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor),
                _timeSlider.BottomAnchor.ConstraintEqualTo(toolbar.TopAnchor),
                _timeSlider.HeightAnchor.ConstraintEqualTo(100),
                toolbar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                toolbar.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),
                toolbar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _statusLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _statusLabel.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _statusLabel.TopAnchor.ConstraintEqualTo(_mapView.TopAnchor),
                _statusLabel.HeightAnchor.ConstraintEqualTo(40)
            });
        }

        public override void ViewWillAppear(bool animated)
        {
            _timeSlider.CurrentExtentChanged += _timeSlider_CurrentExtentChanged;
            _layerSegment.ValueChanged += _layerSegment_ValueChanged;
            _stepForwardButton.Clicked += _stepForwardButton_Clicked;
            _stepBackButton.Clicked += _stepBackButton_Clicked;
            _stepCountButton.Clicked += _stepCountButton_Clicked;
            base.ViewWillAppear(animated);
        }

        private void _stepCountButton_Clicked(object sender, EventArgs e)
        {
            if (_textInputAlertController == null)
            {
                _textInputAlertController = UIAlertController.Create("Set number of time steps to show.", "", UIAlertControllerStyle.Alert);
                _textInputAlertController.AddTextField(textField => { textField.KeyboardType = UIKeyboardType.NumberPad; });

                void HandleAlertAction(UIAlertAction action)
                {
                    try
                    {
                        var intervals = int.Parse(_textInputAlertController.TextFields[0].Text);
                        _timeSlider.InitializeTimeSteps(intervals);
                    }
                    catch(Exception)
                    {
                        // Ignore
                    }
                }

                // Add Actions.
                _textInputAlertController.AddAction(UIAlertAction.Create("Done", UIAlertActionStyle.Default, HandleAlertAction));
            }

            // Show the alert.
            PresentViewController(_textInputAlertController, true, null);
        }

        private void _stepBackButton_Clicked(object sender, EventArgs e)
        {
            _timeSlider.StepBack(1);
        }

        private void _stepForwardButton_Clicked(object sender, EventArgs e)
        {
            _timeSlider.StepForward(1);
        }

        private void _layerSegment_ValueChanged(object sender, EventArgs e)
        {
            _ = HandleSelectionChanged();
        }

        private void _timeSlider_CurrentExtentChanged(object sender, UI.TimeExtentChangedEventArgs e)
        {
            _mapView.TimeExtent = e.NewExtent;
        }

        public override void ViewDidDisappear(bool animated)
        {
            _timeSlider.CurrentExtentChanged -= _timeSlider_CurrentExtentChanged;
            _layerSegment.ValueChanged -= _layerSegment_ValueChanged;
            _stepBackButton.Clicked -= _stepBackButton_Clicked;
            _stepForwardButton.Clicked -= _stepForwardButton_Clicked;
            _stepCountButton.Clicked -= _stepCountButton_Clicked;
            base.ViewDidDisappear(animated);
        }
    }
}