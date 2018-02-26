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
        private RectangleView HorizontalTrackThumb;
        private RectangleView _startTimeTickmark;
        private RectangleView _endTimeTickmark;
        private bool _elementsArranged = false;
        private bool _isSizeValid = false;
        private bool _isHorizontalThumbFocused = false;
        private CGPoint _lastTouchLocation;

        private void Initialize()
        {
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

            _endTimeTickmark = new RectangleView(endTickWidth, endTickHeight) { BackgroundColor = FullExtentStroke };
            AddSubview(_endTimeTickmark);

            SliderTrack = new RectangleView(100, 7)
            {
                BackgroundColor = FullExtentFill,
                BorderColor = FullExtentStroke,
                BorderWidth = 1.5,
                UserInteractionEnabled = false,
                UseShadow = false
            };
            AddSubview(SliderTrack);

            HorizontalTrackThumb = new RectangleView(100, 7)
            {
                BackgroundColor = CurrentExtentFill,
                BorderColor = FullExtentStroke,
                BorderWidth = SliderTrack.BorderWidth
            };
            HorizontalTrackThumb.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(RectangleView.Frame))
                {
                    HorizontalTrackThumb.Width = HorizontalTrackThumb.Frame.Width;
                }
            };

            SliderTrack.AddSubview(HorizontalTrackThumb);

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

        /// <inheritdoc />
        public override bool BeginTracking(UITouch uitouch, UIEvent uievent)
        {
            var location = uitouch.LocationInView(SliderTrack);
            if (MinimumThumb.Frame.Contains(location))
            {
                MinimumThumb.IsFocused = true;
                _lastTouchLocation = uitouch.LocationInView(MinimumThumb);
            }
            else if (MaximumThumb.Frame.Contains(location))
            {
                MaximumThumb.IsFocused = true;
                _lastTouchLocation = uitouch.LocationInView(MaximumThumb);
            }
            // TODO: Allow dragging middle thumb
            // else if (HorizontalTrackThumb.Frame.Contains(location))
            // {
            //    _isHorizontalThumbFocused = true;
            //    _lastTouchLocation = uitouch.LocationInView(HorizontalTrackThumb);
            // }

            return true;
        }

        /// <inheritdoc />
        public override bool ContinueTracking(UITouch uitouch, UIEvent uievent)
        {
            if (!MinimumThumb.IsFocused && !MaximumThumb.IsFocused && !_isHorizontalThumbFocused)
                return true;

            var trackedView = MinimumThumb.IsFocused ? MinimumThumb : MaximumThumb.IsFocused ? MaximumThumb : HorizontalTrackThumb;

            var current = uitouch.LocationInView(trackedView);
            //var translateX = current.X - trackedView.Frame.Width / 2;
            var translateX = current.X - _lastTouchLocation.X;
            if (MinimumThumb.IsFocused)
                OnMinimumThumbDrag(translateX);
            if (MaximumThumb.IsFocused)
                OnMaximumThumbDrag(translateX);
            // TODO: Allow dragging middle thumb
            // if (_isHorizontalThumbFocused)
                // OnCurrentExtentThumbDrag(translateX);

            return true;
        }

        /// <inheritdoc />
        public override void EndTracking(UITouch uitouch, UIEvent uievent)
        {
            MinimumThumb.IsFocused = false;
            MaximumThumb.IsFocused = false;
            _isHorizontalThumbFocused = false;
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
        }

        private CGSize MeasureSize()
        {
            return new CGSize(Bounds.Width, 6);
        }

        private void ArrangeTrackElements()
        {
            if (_elementsArranged) // Ensure this is only done once per instance
                return;
            
            _elementsArranged = true;

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

            HorizontalTrackThumb.Frame = new CGRect(0, 0, sliderTrackWidth, SliderTrack.Height);
            //HorizontalTrackThumb.FrameUpdated += (o, e) =>
            //{
            //    var l = HorizontalTrackThumb.Frame.Left;
            //    var r = HorizontalTrackThumb.Frame.Right;
            //};

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

        private void ApplyCurrentExtentFill() => HorizontalTrackThumb.BackgroundColor = CurrentExtentFill;
    }
}

#endif