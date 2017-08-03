using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CoreGraphics;
using Foundation;
using UIKit;


namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("ScaleLine")]
    [DisplayName("Scaleline")]
    [Category("ArcGIS Runtime Controls")]
    public partial class ScaleLine : IComponent
    {
        private RectangleView _firstMetricTickLine;
        private RectangleView _secondMetricTickLine;
        private RectangleView _scaleLineStartSegment;
        private RectangleView _firstUsTickLine;
        private RectangleView _secondUsTickLine;
        private RectangleView _combinedScaleLine;

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        public ScaleLine(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        /// <inheritdoc />
        public override void AwakeFromNib()
        {
            var component = (IComponent)this;
            DesignTime.IsDesignMode = component.Site != null && component.Site.DesignMode;

            Initialize();

            base.AwakeFromNib();
        }

        private void Initialize()
        {
            BackgroundColor = UIColor.Clear;

            // At run-time, don't display the sub-views until their dimensions have been calculated
            if (!DesignTime.IsDesignMode)
                Hidden = true;

            // Vertically-oriented stack view for containing all scalebar components
            var rootStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            // Initialize scalebar components with placeholder sizes and values
            var font = UIFont.SystemFontOfSize(11);
            _combinedScaleLine = new RectangleView(200, 2) { BackgroundColor = ForegroundColor };
            _metricScaleLine = new RectangleView(200, 2);
            _metricValue = new UILabel() { Text = "100", Font = font };
            _metricUnit = new UILabel() { Text = "m", Font = font };
            _usScaleLine = new RectangleView(_metricScaleLine.Width * .9144, 2);
            _usValue = new UILabel() { Text = "300", Font = font };
            _usUnit = new UILabel() { Text = "ft", Font = font };

            // Listen for width updates on metric and imperial scale lines to update the combined scale line
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

            // Add all scalebar rows to the root stack view
            rootStackView.AddArrangedSubview(firstRowStackView);
            rootStackView.AddArrangedSubview(secondRowStackView);
            rootStackView.AddArrangedSubview(thirdRowStackView);
            rootStackView.AddArrangedSubview(fourthRowStackView);
            rootStackView.AddArrangedSubview(fifthRowStackView);

            AddSubview(rootStackView);

            // Anchor the root stack view to the bottom left of the view
            rootStackView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            rootStackView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;

            // Set up constraints to resize scalebar components when scale line resizes
            metricWidthPlaceholder.WidthAnchor.ConstraintEqualTo(_metricScaleLine.WidthAnchor).Active = true;
            metricWidthPlaceholder.HeightAnchor.ConstraintEqualTo(_firstMetricTickLine.HeightAnchor).Active = true;
            usWidthPlaceholder.WidthAnchor.ConstraintEqualTo(_usScaleLine.WidthAnchor).Active = true;
            usWidthPlaceholder.HeightAnchor.ConstraintEqualTo(_usValue.HeightAnchor).Active = true;
        }

        private void ScaleLine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update the scale line to be the longer of the metric or imperial lines
            _combinedScaleLine.Width = _metricScaleLine.Width > _usScaleLine.Width ? _metricScaleLine.Width : _usScaleLine.Width;
        }

        private UIColor _foregroundColor = UIColor.Black;

        /// <summary>
        /// Gets or sets the color of the foreground elements of the <see cref="ScaleLine"/>
        /// </summary>
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

        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        ISite IComponent.Site { get; set; }

        private EventHandler _disposed;

        /// <summary>
        /// Internal use only
        /// </summary>
        event EventHandler IComponent.Disposed
        {
            add { _disposed += value; }
            remove { _disposed -= value; }
        }
    }
}