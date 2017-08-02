using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using CoreGraphics;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class ScaleLine
    {
        private RectangleView _firstMetricTickLine;
        private RectangleView _secondMetricTickLine;
        private RectangleView _scaleLineStartSegment;
        private RectangleView _firstUsTickLine;
        private RectangleView _secondUsTickLine;
        private RectangleView _combinedScaleLine;

        private void Initialize()
        {
            var rootStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            var font = UIFont.SystemFontOfSize(11);
            _combinedScaleLine = new RectangleView(200, 2) { BackgroundColor = ForegroundColor };
            _metricScaleLine = new RectangleView(200, 2);
            _metricValue = new UILabel() { Text = "100", Font = font };
            _metricUnit = new UILabel() { Text = "m", Font = font };
            _usScaleLine = new RectangleView(200, 2);
            _usValue = new UILabel() { Text = "USValue", Font = font };
            _usUnit = new UILabel() { Text = "USUnit", Font = font };

            _metricScaleLine.PropertyChanged += ScaleLine_PropertyChanged;
            _usScaleLine.PropertyChanged += ScaleLine_PropertyChanged;

            // ===============================================================
            // First row - placeholder, numeric text, and units text
            // ===============================================================
            var firstRowStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            firstRowStackView.AddArrangedSubview(_metricScaleLine);
            firstRowStackView.AddArrangedSubview(_metricValue);
            firstRowStackView.AddArrangedSubview(_metricUnit);

            // ================================================================================
            // Second row - first metric tick line, placeholder, and second metric tick line
            // ================================================================================
            var secondRowStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            var tickWidth = 2;
            var tickHeight = 5;

            _firstMetricTickLine = new RectangleView(tickWidth, tickHeight) { BackgroundColor = ForegroundColor };

            var metricWidthPlaceholder = new UIView();
            _secondMetricTickLine = new RectangleView(tickWidth, tickHeight) { BackgroundColor = ForegroundColor };

            secondRowStackView.AddArrangedSubview(_firstMetricTickLine);
            secondRowStackView.AddArrangedSubview(metricWidthPlaceholder);
            secondRowStackView.AddArrangedSubview(_secondMetricTickLine);

            // ==============================================================================================
            // Third row - filler segment at start of scale line, metric scale line, imperial scale line
            // ==============================================================================================
            var thirdRowStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            _scaleLineStartSegment = new RectangleView(4, 2) { BackgroundColor = ForegroundColor };

            thirdRowStackView.AddArrangedSubview(_scaleLineStartSegment);
            thirdRowStackView.AddArrangedSubview(_combinedScaleLine);

            // ==============================================================================================
            // Fourth row - first imperial tick line, placeholder, second imperial tick line
            // ==============================================================================================
            var fourthRowStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            _firstUsTickLine = new RectangleView(tickWidth, tickHeight) { BackgroundColor = ForegroundColor };
            _secondUsTickLine = new RectangleView(tickWidth, tickHeight) { BackgroundColor = ForegroundColor };

            fourthRowStackView.AddArrangedSubview(_firstUsTickLine);
            fourthRowStackView.AddArrangedSubview(_usScaleLine);
            fourthRowStackView.AddArrangedSubview(_secondUsTickLine);

            // ==========================================================================
            // Fifth row - placeholder, imperial numeric text, imperial unit text
            // ==========================================================================
            var fifthRowStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            var usWidthPlaceholder = new UIView();

            fifthRowStackView.AddArrangedSubview(usWidthPlaceholder);
            fifthRowStackView.AddArrangedSubview(_usValue);
            fifthRowStackView.AddArrangedSubview(_usUnit);

            // Add all rows to the root stack view
            rootStackView.AddArrangedSubview(firstRowStackView);
            rootStackView.AddArrangedSubview(secondRowStackView);
            rootStackView.AddArrangedSubview(thirdRowStackView);
            rootStackView.AddArrangedSubview(fourthRowStackView);
            rootStackView.AddArrangedSubview(fifthRowStackView);

            AddSubview(rootStackView);

            rootStackView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            rootStackView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;

            metricWidthPlaceholder.WidthAnchor.ConstraintEqualTo(_metricScaleLine.WidthAnchor).Active = true;
            metricWidthPlaceholder.HeightAnchor.ConstraintEqualTo(_firstMetricTickLine.HeightAnchor).Active = true;
            usWidthPlaceholder.WidthAnchor.ConstraintEqualTo(_usScaleLine.WidthAnchor).Active = true;
            usWidthPlaceholder.HeightAnchor.ConstraintEqualTo(_usScaleLine.HeightAnchor).Active = true;
        }

        private void ScaleLine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update the scale line to be the longer of the metric or imperial lines
            _combinedScaleLine.Width = _metricScaleLine.Width > _usScaleLine.Width ? _metricScaleLine.Width : _usScaleLine.Width;
        }

        private UIColor _foregroundColor;
        public UIColor ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;

                if (_metricScaleLine == null)
                    return;

                _combinedScaleLine.BackgroundColor = value;
                _metricUnit.TextColor = value;
                _metricValue.TextColor = value;
                _usUnit.TextColor = value;
                _usValue.TextColor = value;
                _firstMetricTickLine.BackgroundColor = value;
                _secondMetricTickLine.BackgroundColor = value;
                _firstUsTickLine.BackgroundColor = value;
                _secondUsTickLine.BackgroundColor = value;
                _scaleLineStartSegment.BackgroundColor = value;
            }
        }
    }
}