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

#if __IOS__

using CoreGraphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class TimeSlider : UIControl
    {
        private RectangleView SliderTrack;
        private RectangleView _startTimeTickmark;
        private RectangleView _endTimeTickmark;
        private UIView _endTimeTickmarkContainer;
        private UIStackView _rootStackView;
        private UIView someUIView;

        private void Initialize()
        {
            //_rootStackView = new UIStackView()
            //{
            //    Axis = UILayoutConstraintAxis.Horizontal,
            //    Alignment = UIStackViewAlignment.Leading,
            //    Distribution = UIStackViewDistribution.Fill,
            //    TranslatesAutoresizingMaskIntoConstraints = false,
            //    Spacing = 0
            //};


            //someUIView = new UIView()
            //{
            //    BackgroundColor = UIColor.Purple
            //};
            //AddSubview(someUIView);

            var fullExtentLabelFormat = string.IsNullOrEmpty(FullExtentLabelFormat) ? _defaultFullExtentLabelFormat : FullExtentLabelFormat;
            FullExtentStartTimeLabel = new UILabel()
            {
                Text = FullExtent?.StartTime.ToString(fullExtentLabelFormat) ?? "",
                Font = UIFont.SystemFontOfSize(11),
                TextColor = FullExtentLabelColor
            };
            AddSubview(FullExtentStartTimeLabel);

            FullExtentEndTimeLabel = new UILabel()
            {
                Text = FullExtent?.EndTime.ToString(fullExtentLabelFormat) ?? "",
                Font = UIFont.SystemFontOfSize(11),
                TextColor = FullExtentLabelColor
            };
            AddSubview(FullExtentEndTimeLabel);

            var endTickWidth = 1;
            var endTickHeight = 6;

            _startTimeTickmark = new RectangleView(endTickWidth, endTickHeight)
            {
                BackgroundColor = FullExtentStroke
            };
            AddSubview(_startTimeTickmark);
            //_rootStackView.AddArrangedSubview(_startTimeTickmark);

            _endTimeTickmark = new RectangleView(endTickWidth, endTickHeight) { BackgroundColor = FullExtentStroke };
            AddSubview(_endTimeTickmark);
            //_endTimeTickmarkContainer = new UIView();
            //_endTimeTickmarkContainer.AddSubview(_endTimeTickmark);
            //_rootStackView.AddArrangedSubview(_endTimeTickmarkContainer);

            //AddSubview(_rootStackView);

            // Anchor the root stack view to the bottom left of the view
            //_rootStackView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            //_rootStackView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;

            //InvalidateIntrinsicContentSize();

            SliderTrack = new RectangleView(100, 7)
            {
                BackgroundColor = FullExtentFill,
                BorderColor = FullExtentStroke.CGColor,
                BorderWidth = 1
            };
            AddSubview(SliderTrack);

            Tickmarks = new Primitives.Tickbar()
            {
                TickFill = TimeStepIntervalTickFill,
                TickLabelColor = TimeStepIntervalLabelColor,
                ShowTickLabels = LabelMode == TimeSliderLabelMode.TimeStepInterval
            };
            AddSubview(Tickmarks);

            var currentExtentLabelFormat = string.IsNullOrEmpty(CurrentExtentLabelFormat) ?
                _defaultCurrentExtentLabelFormat : CurrentExtentLabelFormat;
            MinimumThumbLabel = new UILabel()
            {
                Text = CurrentExtent?.StartTime.ToString(currentExtentLabelFormat) ?? string.Empty,
                Font = UIFont.SystemFontOfSize(11),
                TextColor = CurrentExtentLabelColor
            };
            AddSubview(MinimumThumbLabel);

            MaximumThumbLabel = new UILabel()
            {
                Text = CurrentExtent?.StartTime.ToString(currentExtentLabelFormat) ?? string.Empty,
                Font = UIFont.SystemFontOfSize(11),
                TextColor = CurrentExtentLabelColor
            };
            AddSubview(MaximumThumbLabel);

            PositionTickmarks();
            //SetButtonVisibility();
            //ApplyLabelMode(LabelMode);
        }

        private bool _isSizeValid = false;

        /// <inheritdoc />
        public override bool BeginTracking(UITouch uitouch, UIEvent uievent)
        {
            return base.BeginTracking(uitouch, uievent);
        }

        /// <inheritdoc />
        public override bool ContinueTracking(UITouch uitouch, UIEvent uievent)
        {
            return base.ContinueTracking(uitouch, uievent);
        }

        /// <inheritdoc />
        public override void InvalidateIntrinsicContentSize()
        {
            _isSizeValid = false;
            base.InvalidateIntrinsicContentSize();
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
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            ArrangeTrackElements();

            //someUIView.Frame = Bounds;
            //_endTimeTickmarkContainer.Frame = new CGRect(0, 0, Bounds.Width - 1, 6);
        }

        private CGSize MeasureSize()
        {
            return new CGSize(Bounds.Width, 6);
        }

        private void ArrangeTrackElements()
        {
            var availableWidth = Bounds.Width;

            var fullExtentStartLabelSize = FullExtentStartTimeLabel.SizeThatFits(Bounds.Size);
            FullExtentStartTimeLabel.Frame = new CGRect(CGPoint.Empty, fullExtentStartLabelSize);

            var fullExtentEndLabelSize = FullExtentEndTimeLabel.SizeThatFits(Bounds.Size);
            FullExtentEndTimeLabel.Frame = new CGRect(new CGPoint(Bounds.Right - fullExtentEndLabelSize.Width, 0), fullExtentEndLabelSize);
            var fullExtentLabelBottom = Math.Max(FullExtentStartTimeLabel.Bounds.Bottom, FullExtentStartTimeLabel.Bounds.Bottom);

            var endTickTop = fullExtentLabelBottom + 2;
            var startTickLeft = (fullExtentStartLabelSize.Width - _startTimeTickmark.Width) / 2;
            _startTimeTickmark.Frame = new CGRect(startTickLeft, endTickTop, _startTimeTickmark.Width, _startTimeTickmark.Height);

            var endTickLeft = Bounds.Width - ((fullExtentEndLabelSize.Width - _endTimeTickmark.Width) / 2);
            _endTimeTickmark.Frame = new CGRect(endTickLeft, endTickTop, _endTimeTickmark.Width, _endTimeTickmark.Height);

            var sliderTrackLeft = startTickLeft;
            var sliderTrackWidth = _endTimeTickmark.Frame.Right - sliderTrackLeft;
            var sliderTrackTop = _startTimeTickmark.Frame.Bottom;
            SliderTrack.Width = sliderTrackWidth;
            SliderTrack.Frame = new CGRect(sliderTrackLeft, sliderTrackTop, sliderTrackWidth, SliderTrack.Height);

            Tickmarks.Frame = new CGRect(sliderTrackLeft, SliderTrack.Frame.Bottom, sliderTrackWidth, SliderTrack.Frame.Height);
        }

        private void UpdateFullExtentLabels()
        {
            var fullExtentLabelFormat = string.IsNullOrEmpty(FullExtentLabelFormat) ? _defaultFullExtentLabelFormat : FullExtentLabelFormat;
            FullExtentStartTimeLabel.Text = FullExtent?.StartTime.ToString(fullExtentLabelFormat) ?? "";
            FullExtentEndTimeLabel.Text = FullExtent?.EndTime.ToString(fullExtentLabelFormat) ?? "";
        }
    }
}

#endif