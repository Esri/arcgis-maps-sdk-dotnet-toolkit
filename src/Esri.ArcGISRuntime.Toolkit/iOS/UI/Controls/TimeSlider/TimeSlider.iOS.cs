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
        private Thumb MinimumThumb;
        private Thumb MaximumThumb;
        private RectangleView HorizontalTrackThumb;
        private DrawActionButton NextButton;
        private DrawActionButton PreviousButton;
        private DrawActionToggleButton PlayPauseButton;
        private RectangleView _startTimeTickmark;
        private RectangleView _endTimeTickmark;
        private bool _thumbsArranged = false;
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

            PreviousButton = new DrawActionButton()
            {
                BackgroundColor = PlaybackButtonsFill,
                BorderColor = PlaybackButtonsStroke,
                BorderWidth = 0.5,
                DrawContentAction = (context, button) =>
                {
                    var spacing = 3 + button.BorderWidth;
                    var barWidth = 3;
                    var triangleWidth = button.Bounds.Width - spacing - barWidth;
                    var triangleHeight = button.Bounds.Height - (button.BorderWidth * 2);
                    DrawTriangle(context, triangleWidth, triangleHeight, button.BackgroundColor.CGColor, button.BorderWidth,
                                 button.BorderColor.CGColor, pointOnRight: false, left: 0, top: button.BorderWidth);

                    var barLeft = triangleWidth + spacing - button.BorderWidth;
                    DrawRectangle(context, barWidth, button.Bounds.Height, button.BackgroundColor.CGColor,
                                  button.BorderWidth, button.BorderColor.CGColor, left: barLeft);
                }
            };
            PreviousButton.TouchUpInside += (o, e) => OnPreviousButtonClick();
            AddSubview(PreviousButton);

            NextButton = new DrawActionButton()
            {
                BackgroundColor = PlaybackButtonsFill,
                BorderColor = PlaybackButtonsStroke,
                BorderWidth = 0.5,
                DrawContentAction = (context, button) =>
                {
                    var spacing = 3 + button.BorderWidth;
                    var barWidth = 3;
                    var triangleWidth = button.Bounds.Width - spacing - barWidth;
                    var triangleHeight = button.Bounds.Height - (button.BorderWidth * 2);
                    DrawRectangle(context, barWidth, button.Bounds.Height, button.BackgroundColor.CGColor,
                                  button.BorderWidth, button.BorderColor.CGColor, left: button.BorderWidth);

                    DrawTriangle(context, triangleWidth, triangleHeight, button.BackgroundColor.CGColor, button.BorderWidth,
                                 button.BorderColor.CGColor, pointOnRight: true, left: barWidth + spacing, top: button.BorderWidth);
                }
            };
            NextButton.TouchUpInside += (o, e) => OnNextButtonClick();
            AddSubview(NextButton);

            PlayPauseButton = new DrawActionToggleButton()
            {
                BackgroundColor = PlaybackButtonsFill,
                BorderColor = PlaybackButtonsStroke,
                BorderWidth = 0.5,
                DrawCheckedContentAction = (context, button) =>
                {
                    var spacing = 4d;
                    var barWidth = 7d;
                    var left = button.Bounds.GetMidX() - (spacing / 2) - barWidth - 2;
                    DrawRectangle(context, barWidth, button.Bounds.Height, button.BackgroundColor.CGColor, button.BorderWidth, button.BorderColor.CGColor, left);

                    left = button.Bounds.GetMidX() + (spacing / 2);
                    DrawRectangle(context, barWidth, button.Bounds.Height, button.BackgroundColor.CGColor, button.BorderWidth, button.BorderColor.CGColor, left);
                },
                DrawUncheckedContentAction = (context, button) =>
                {
                    var triangleWidth = button.Bounds.Width - button.BorderWidth;
                    DrawTriangle(context, triangleWidth, button.Bounds.Height, button.BackgroundColor.CGColor, button.BorderWidth,
                                 button.BorderColor.CGColor, pointOnRight: true, left: button.BorderWidth);
                }
            };
            PlayPauseButton.CheckedChanged += (o, e) => IsPlaying = PlayPauseButton.IsChecked;
            AddSubview(PlayPauseButton);

            var endTickWidth = 1;
            var endTickHeight = 12;

            _startTimeTickmark = new RectangleView(endTickWidth, endTickHeight)
            {
                BackgroundColor = FullExtentStroke
            };
            AddSubview(_startTimeTickmark);

            _endTimeTickmark = new RectangleView(endTickWidth, endTickHeight) { BackgroundColor = FullExtentStroke };
            AddSubview(_endTimeTickmark);

            var sliderTrackHeight = 2;
            SliderTrack = new RectangleView(100, sliderTrackHeight)
            {
                BackgroundColor = FullExtentFill,
                BorderColor = FullExtentStroke,
                BorderWidth = FullExtentBorderWidth
            };
            AddSubview(SliderTrack);

            HorizontalTrackThumb = new RectangleView(100, sliderTrackHeight)
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

            var thumbSize = 36;
            var disabledSize = new CGSize(7, 16);
            MinimumThumb = new Thumb()
            {
                Width = thumbSize,
                Height = thumbSize,
                DisabledSize = disabledSize,
                CornerRadius = thumbSize / 2d,
                UseShadow = true,
                Enabled = !IsStartTimePinned
            };
            SliderTrack.AddSubview(MinimumThumb);

            MaximumThumb = new Thumb()
            {
                Width = thumbSize,
                Height = thumbSize,
                DisabledSize = disabledSize,
                CornerRadius = thumbSize / 2d,
                UseShadow = true,
                Enabled = !IsEndTimePinned
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

            // Add pan gesture recognizer to handle thumb manipulation
            var panRecognizer = new UIPanGestureRecognizer() { CancelsTouchesInView = false };
            panRecognizer.AddTarget(() =>
            {
                switch (panRecognizer.State)
                {
                    case UIGestureRecognizerState.Began:
                        // Check whether gesture started on one of the thumbs.
                        // Use a minimum target size of 44 x 44 for hit testing.
                        var minTargetSize = 44;
                        var minThumbHitTestFrame = ExpandFrame(MinimumThumb.Frame, minTargetSize);
                        var maxThumbHitTestFrame = ExpandFrame(MaximumThumb.Frame, minTargetSize);
                        var location = panRecognizer.LocationInView(SliderTrack);
                        if (minThumbHitTestFrame.Contains(location) && !IsStartTimePinned)                            
                        {
                            MinimumThumb.IsFocused = true;
                            _lastTouchLocation = panRecognizer.LocationInView(MinimumThumb);
                        }
                        else if (maxThumbHitTestFrame.Contains(location) && !IsEndTimePinned)
                        {
                            MaximumThumb.IsFocused = true;
                            _lastTouchLocation = panRecognizer.LocationInView(MaximumThumb);
                        }
                        // TODO: Allow dragging middle thumb?
                        //else if (HorizontalTrackThumb.Frame.Contains(location))
                        //{
                        //   _isHorizontalThumbFocused = true;
                        //   _lastTouchLocation = panRecognizer.LocationInView(HorizontalTrackThumb);
                        //}
                        break;
                    case UIGestureRecognizerState.Changed:
                        if (!MinimumThumb.IsFocused && !MaximumThumb.IsFocused && !(MinimumThumb.IsFocused && IsStartTimePinned) && !(MaximumThumb.IsFocused && IsEndTimePinned)) // && !_isHorizontalThumbFocused)
                            return;

                        var trackedView = MinimumThumb.IsFocused ? MinimumThumb : MaximumThumb;

                        var currentLocation = panRecognizer.LocationInView(trackedView);
                        var translateX = currentLocation.X - _lastTouchLocation.X;

                        if (MinimumThumb.IsFocused)
                            OnMinimumThumbDrag(translateX);
                        if (MaximumThumb.IsFocused)
                            OnMaximumThumbDrag(translateX);
                        // TODO: Allow dragging middle thumb?
                        //if (_isHorizontalThumbFocused)
                            //OnCurrentExtentThumbDrag(translateX);
                        break;
                    case UIGestureRecognizerState.Ended:
                    case UIGestureRecognizerState.Cancelled:
                    case UIGestureRecognizerState.Failed:
                        MinimumThumb.IsFocused = false;
                        MaximumThumb.IsFocused = false;
                        _isHorizontalThumbFocused = false;
                        OnDragCompleted();
                        break;
                }
            });
            AddGestureRecognizer(panRecognizer);

            PositionTickmarks();
            //SetButtonVisibility();
            ApplyLabelMode(LabelMode);
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

            ArrangeElements();
        }

        private CGSize MeasureSize()
        {
            return new CGSize(Bounds.Width, 6);
        }

        private void ArrangeElements()
        {
            var verticalSpacing = 4;
            var playButtonSpacing = 12;

            var playPauseButtonWidth = 26;
            var playPauseButtonHeight = 34;
            var playPauseButtonLeft = Bounds.GetMidX() - playPauseButtonWidth / 2;
            PlayPauseButton.Frame = new CGRect(playPauseButtonLeft, 0, playPauseButtonWidth, playPauseButtonHeight);

            var previousNextButtonWidth = 24;
            var previousNextButtonHeight = 24;
            var previousNextButtonTop = (playPauseButtonHeight - previousNextButtonHeight) / 2;
            var previousButtonLeft = playPauseButtonLeft - playButtonSpacing - previousNextButtonWidth - 2;
            PreviousButton.Frame = new CGRect(previousButtonLeft, previousNextButtonTop, previousNextButtonWidth, previousNextButtonHeight);

            var nextButtonLeft = playPauseButtonLeft + playPauseButtonWidth + playButtonSpacing;
            NextButton.Frame = new CGRect(nextButtonLeft, previousNextButtonTop, previousNextButtonWidth, previousNextButtonHeight);

            var playbackButtonsBottom = playPauseButtonHeight;

            var fullExtentStartLabelSize = FullExtentStartTimeLabel.SizeThatFits(Bounds.Size);
            var fullExtentEndLabelSize = FullExtentEndTimeLabel.SizeThatFits(Bounds.Size);

            var maxThumbHeight = Math.Max(MinimumThumb.Height, MaximumThumb.Height);
            var thumbOverhang = (maxThumbHeight - SliderTrack.Height) / 2;
            var sliderTrackTop = playbackButtonsBottom + thumbOverhang;

            var endTickTop = sliderTrackTop - _startTimeTickmark.Height;
            var startTickLeft = (fullExtentStartLabelSize.Width - _startTimeTickmark.Width) / 2;
            _startTimeTickmark.Frame = new CGRect(startTickLeft, endTickTop, _startTimeTickmark.Width, _startTimeTickmark.Height);

            var endTickLeft = Bounds.Width - ((fullExtentEndLabelSize.Width - _endTimeTickmark.Width) / 2);
            _endTimeTickmark.Frame = new CGRect(endTickLeft, endTickTop, _endTimeTickmark.Width, _endTimeTickmark.Height);

            var fullExtentStartLabelTop = endTickTop - verticalSpacing - fullExtentStartLabelSize.Height;
            FullExtentStartTimeLabel.Frame = new CGRect(0, fullExtentStartLabelTop,
                fullExtentStartLabelSize.Width, fullExtentStartLabelSize.Height);

            var fullExtentEndLabelTop = endTickTop - verticalSpacing - fullExtentEndLabelSize.Height;
            FullExtentEndTimeLabel.Frame = new CGRect(Bounds.Right - fullExtentEndLabelSize.Width, fullExtentEndLabelTop, fullExtentEndLabelSize.Width, fullExtentEndLabelSize.Height);

            var sliderTrackLeft = startTickLeft;
            var sliderTrackWidth = _endTimeTickmark.Frame.Right - sliderTrackLeft;
            SliderTrack.Width = sliderTrackWidth;
            SliderTrack.Frame = new CGRect(sliderTrackLeft, sliderTrackTop, sliderTrackWidth, SliderTrack.Height);
            Tickmarks.Frame = new CGRect(0, SliderTrack.Height, sliderTrackWidth, 50);

            HorizontalTrackThumb.Frame = new CGRect(0, 0, sliderTrackWidth, SliderTrack.Height);

            if (_thumbsArranged) // Ensure initial arrangement of thumbs is only done once per instance
                return;
            _thumbsArranged = true;

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

        private void DrawTriangle(CGContext context, double width, double height, CGColor fillColor, double strokeWidth,
                                  CGColor strokeColor, bool pointOnRight, double left = 0, double top = 0)
        {
            var trianglePath = new CGPath();
            var right = left + width;
            var pointedSideX = pointOnRight ? right : left;
            var flatSideX = pointOnRight ? left : right;
            var bottom = top + height;
            trianglePath.AddLines(new CGPoint[]
            {
                new CGPoint(pointedSideX, bottom / 2d),
                new CGPoint(flatSideX, 0),
                new CGPoint(flatSideX, bottom)
            });
            trianglePath.CloseSubpath();

            DrawPath(context, trianglePath, fillColor, strokeWidth, strokeColor);
        }

        private void DrawRectangle(CGContext context, double width, double height, CGColor fillColor, double strokeWidth,
                                   CGColor strokeColor, double left = 0, double top = 0)
        {
            var bottom = top + height;
            var right = left + width;
            var rectPath = new CGPath();
            rectPath.AddLines(new CGPoint[]
            {
                new CGPoint(left, top),
                new CGPoint(right, top),
                new CGPoint(right, bottom),
                new CGPoint(left, bottom)
            });
            rectPath.CloseSubpath();
            DrawPath(context, rectPath, fillColor, strokeWidth, strokeColor);
        }

        private void DrawPath(CGContext context, CGPath path, CGColor fillColor, double strokeWidth, CGColor strokeColor)
        {
            context.SetFillColor(fillColor);
            context.AddPath(path);
            context.FillPath();

            if (strokeWidth > 0)
            {
                context.SetLineWidth((nfloat)strokeWidth);
                context.SetStrokeColor(strokeColor);
                context.AddPath(path);
                context.StrokePath();
            }
        }

        private CGRect ExpandFrame(CGRect frame, double minTargetSize)
        {
            var minWidthAdjustment = minTargetSize > frame.Width ? (minTargetSize - frame.Width) / 2 : 0;
            var minHeightAdjustment = minTargetSize > frame.Height ? (minTargetSize - frame.Height) / 2 : 0;
            var expandedFrame = new CGRect(frame.Left - minWidthAdjustment, frame.Top - minHeightAdjustment,
                                           frame.Width + (minWidthAdjustment * 2), frame.Height + (minHeightAdjustment * 2));
            return expandedFrame;
        }
    }
}

#endif