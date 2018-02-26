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
        private RectangleView MinimumThumb;
        private RectangleView MaximumThumb;
        private RectangleView _startTimeTickmark;
        private RectangleView _endTimeTickmark;

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

            //SliderTrack = new UIView
            //{
            //    UserInteractionEnabled = false
            //};
            //AddSubview(SliderTrack);

            SliderTrack = new RectangleView(100, 7)
            {
                BackgroundColor = FullExtentFill,
                BorderColor = FullExtentStroke,
                BorderWidth = 1.5,
                UserInteractionEnabled = false,
                UseShadow = false
            };
            AddSubview(SliderTrack);

            Tickmarks = new Primitives.Tickbar()
            {
                TickFill = TimeStepIntervalTickFill,
                TickLabelColor = TimeStepIntervalLabelColor,
                ShowTickLabels = LabelMode == TimeSliderLabelMode.TimeStepInterval
            };
            SliderTrack.AddSubview(Tickmarks);

            var thumbSize = 23;
            MinimumThumb = new RectangleView()
            {
                BackgroundColor = ThumbFill,
                BorderColor = ThumbStroke,
                Width = thumbSize,
                Height = thumbSize,
                CornerRadius = thumbSize / 2d,
                BorderWidth = 0.5,
                UseShadow = true,
                UserInteractionEnabled = false
            };
            SliderTrack.AddSubview(MinimumThumb);

            MaximumThumb = new RectangleView()
            {
                BackgroundColor = ThumbFill,
                BorderColor = ThumbStroke,
                Width = thumbSize,
                Height = thumbSize,
                CornerRadius = thumbSize / 2d,
                BorderWidth = 0.5,
                UseShadow = true,
                UserInteractionEnabled = false
            };
            SliderTrack.AddSubview(MaximumThumb);

            var currentExtentLabelFormat = string.IsNullOrEmpty(CurrentExtentLabelFormat) ?
                _defaultCurrentExtentLabelFormat : CurrentExtentLabelFormat;
            MinimumThumbLabel = new UILabel()
            {
                Text = CurrentExtent?.StartTime.ToString(currentExtentLabelFormat) ?? string.Empty,
                Font = UIFont.SystemFontOfSize(11),
                TextColor = CurrentExtentLabelColor,
                Hidden = LabelMode != TimeSliderLabelMode.CurrentExtent,
                LineBreakMode = UILineBreakMode.Clip
            };
            SliderTrack.AddSubview(MinimumThumbLabel);

            MaximumThumbLabel = new UILabel()
            {
                Text = CurrentExtent?.StartTime.ToString(currentExtentLabelFormat) ?? string.Empty,
                Font = UIFont.SystemFontOfSize(11),
                TextColor = CurrentExtentLabelColor,
                Hidden = LabelMode != TimeSliderLabelMode.CurrentExtent,
                LineBreakMode = UILineBreakMode.Clip
            };
            SliderTrack.AddSubview(MaximumThumbLabel);

            PositionTickmarks();
            //SetButtonVisibility();
            ApplyLabelMode(LabelMode);
        }

        private bool _isSizeValid = false;

        /// <inheritdoc />
        public override bool BeginTracking(UITouch uitouch, UIEvent uievent)
        {
            var location = uitouch.LocationInView(SliderTrack);
            //UIView trackedView;
            //if (MinimumThumb.Frame.Contains(location))
            //{
            //    var loc = uitouch.LocationInView(MinimumThumb);
            //    _lastTouchLocation = new CGPoint(loc.X - MinimumThumb.Width / 2, loc.Y - MinimumThumb.Height / 2);
            //    MinimumThumb.IsFocused = true;
            //}
            //else if (MaximumThumb.Frame.Contains(location))
            //{
            //    var loc = uitouch.LocationInView(MaximumThumb);
            //    _lastTouchLocation = new CGPoint(loc.X - MaximumThumb.Width / 2, loc.Y - MaximumThumb.Height / 2);
            //    MaximumThumb.IsFocused = true;
            //}
            MinimumThumb.IsFocused = MinimumThumb.Frame.Contains(location);
            MaximumThumb.IsFocused = MaximumThumb.Frame.Contains(location);

            //_lastTouchLocation = location;

            return true;
        }

        /// <inheritdoc />
        public override bool ContinueTracking(UITouch uitouch, UIEvent uievent)
        {
            if (!MinimumThumb.IsFocused && !MaximumThumb.IsFocused)
                return true;

            var trackedView = MinimumThumb.IsFocused ? MinimumThumb : MaximumThumb;

            var current = uitouch.LocationInView(trackedView);
            var translateX = current.X - trackedView.Width / 2;
            if (MinimumThumb.IsFocused)
                OnMinimumThumbDrag(translateX);
            if (MaximumThumb.IsFocused)
                OnMaximumThumbDrag(translateX);

            return true;
        }

        /// <inheritdoc />
        public override void EndTracking(UITouch uitouch, UIEvent uievent)
        {
            //_lastTouchLocation = null;
            MinimumThumb.IsFocused = false;
            MaximumThumb.IsFocused = false;
            OnDragCompleted();
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

            //Tickmarks.BackgroundColor = UIColor.Red;
            Tickmarks.Frame = new CGRect(0, SliderTrack.Height, sliderTrackWidth, 50);

            var thumbTop = (SliderTrack.Frame.Height - MinimumThumb.Height) / 2; // (SliderTrack.Frame.Top + SliderTrack.Frame.Height / 2) - (MinimumThumb.Height / 2);
            var thumbLeft = 0 - (MinimumThumb.Width / 2);
            MinimumThumb.Frame = new CGRect(thumbLeft, thumbTop, MinimumThumb.Width, MinimumThumb.Height);
            MaximumThumb.Frame = new CGRect(thumbLeft, thumbTop, MaximumThumb.Width, MaximumThumb.Height);

            var thumbLabelTop = MinimumThumb.Frame.Bottom + 1;
            var minLabelSize = CalculateTextSize(MinimumThumbLabel);
            var maxLabelSize = CalculateTextSize(MaximumThumbLabel);
            MinimumThumbLabel.Frame = new CGRect(0, thumbLabelTop, Frame.Width, minLabelSize.Height);
            MaximumThumbLabel.Frame = new CGRect(0, thumbLabelTop, Frame.Width, maxLabelSize.Height);
        }

        private void UpdateFullExtentLabels()
        {
            var fullExtentLabelFormat = string.IsNullOrEmpty(FullExtentLabelFormat) ? _defaultFullExtentLabelFormat : FullExtentLabelFormat;
            FullExtentStartTimeLabel.Text = FullExtent?.StartTime.ToString(fullExtentLabelFormat) ?? "";
            FullExtentEndTimeLabel.Text = FullExtent?.EndTime.ToString(fullExtentLabelFormat) ?? "";
        }

        private void UpdateCurrentExtentLabels()
        {
            var currentExtentLabelFormat = string.IsNullOrEmpty(CurrentExtentLabelFormat) ? _defaultCurrentExtentLabelFormat : CurrentExtentLabelFormat;
            MinimumThumbLabel.Text = CurrentExtent?.StartTime.ToString(currentExtentLabelFormat) ?? "";
            MaximumThumbLabel.Text = CurrentExtent?.EndTime.ToString(currentExtentLabelFormat) ?? "";
        }
    }
}

#endif