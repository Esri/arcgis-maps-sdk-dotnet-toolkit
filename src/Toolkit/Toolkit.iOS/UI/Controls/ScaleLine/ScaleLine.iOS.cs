// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using System.ComponentModel;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
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
        private UIStackView _rootStackView;

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ScaleLine(IntPtr handle)
            : base(handle)
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
            {
                Hidden = true;
            }

            // Vertically-oriented stack view for containing all scalebar components
            _rootStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0,
            };

            // Initialize scalebar components with placeholder sizes and values
            var font = UIFont.SystemFontOfSize(11);
            _combinedScaleLine = new RectangleView(200, 2) { BackgroundColor = ForegroundColor };
            _metricScaleLine = new RectangleView(200, 2);
            _metricValue = new UILabel() { Text = "100", Font = font, TextColor = ForegroundColor };
            _metricUnit = new UILabel() { Text = "m", Font = font, TextColor = ForegroundColor };
            _usScaleLine = new RectangleView(_metricScaleLine.Width * .9144, 2);
            _usValue = new UILabel() { Text = "300", Font = font, TextColor = ForegroundColor };
            _usUnit = new UILabel() { Text = "ft", Font = font, TextColor = ForegroundColor };

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
                Spacing = 0,
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
                Spacing = 0,
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
                Spacing = 0,
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
                Spacing = 0,
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
                Spacing = 0,
            };

            var usWidthPlaceholder = new UIView();

            fifthRowStackView.AddArrangedSubview(usWidthPlaceholder);
            fifthRowStackView.AddArrangedSubview(_usValue);
            fifthRowStackView.AddArrangedSubview(_usUnit);

            // Add all scalebar rows to the root stack view
            _rootStackView.AddArrangedSubview(firstRowStackView);
            _rootStackView.AddArrangedSubview(secondRowStackView);
            _rootStackView.AddArrangedSubview(thirdRowStackView);
            _rootStackView.AddArrangedSubview(fourthRowStackView);
            _rootStackView.AddArrangedSubview(fifthRowStackView);

            AddSubview(_rootStackView);

            // Anchor the root stack view to the bottom left of the view
            _rootStackView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _rootStackView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;

            // Set up constraints to resize scalebar components when scale line resizes
            metricWidthPlaceholder.WidthAnchor.ConstraintEqualTo(_metricScaleLine.WidthAnchor).Active = true;
            metricWidthPlaceholder.HeightAnchor.ConstraintEqualTo(_firstMetricTickLine.HeightAnchor).Active = true;
            usWidthPlaceholder.WidthAnchor.ConstraintEqualTo(_usScaleLine.WidthAnchor).Active = true;
            usWidthPlaceholder.HeightAnchor.ConstraintEqualTo(_usValue.HeightAnchor).Active = true;

            InvalidateIntrinsicContentSize();
        }

        private void ScaleLine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update the scale line to be the longer of the metric or imperial lines
            _combinedScaleLine.Width = _metricScaleLine.Width > _usScaleLine.Width ? _metricScaleLine.Width : _usScaleLine.Width;
            InvalidateIntrinsicContentSize();
        }

        private bool _isSizeValid = false;

        /// <inheritdoc />
        public override void InvalidateIntrinsicContentSize()
        {
            _isSizeValid = false;
            base.InvalidateIntrinsicContentSize();
        }

        private UIColor _foregroundColor = UIColorHelper.LabelColor;

        /// <summary>
        /// Gets or sets the color of the foreground elements of the <see cref="ScaleLine"/>.
        /// </summary>
        public UIColor ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;

                if (_metricScaleLine == null)
                {
                    return;
                }

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

        private CGSize _intrinsicContentSize;

        /// <inheritdoc />
        public override CGSize IntrinsicContentSize
        {
            get
            {
                if (!_isSizeValid)
                {
                    _isSizeValid = true;
                    _intrinsicContentSize = MeasureSize();
                }

                return _intrinsicContentSize;
            }
        }

        /// <inheritdoc />
        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
        }

        /// <inheritdoc />
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

        private void SetVisibility(bool isVisible)
        {
            Hidden = !isVisible;
        }

        /// <summary>
        /// Aggregates the size of the view's sub-views.
        /// </summary>
        /// <returns>The size of the view.</returns>
        private CGSize MeasureSize()
        {
            var totalHeight = 0d;
            var totalWidth = 0d;
            foreach (var row in _rootStackView.ArrangedSubviews)
            {
                var rowWidth = 0d;
                var rowHeight = 0d;
                foreach (var view in ((UIStackView)row).ArrangedSubviews)
                {
                    var elementSize = view.IntrinsicContentSize;
                    if (elementSize.Height > rowHeight)
                    {
                        rowHeight = elementSize.Height;
                    }

                    rowWidth += elementSize.Width;
                }

                if (rowWidth > totalWidth)
                {
                    totalWidth = rowWidth;
                }

                totalHeight += rowHeight;
            }

            return new CGSize(totalWidth, totalHeight);
        }
    }
}