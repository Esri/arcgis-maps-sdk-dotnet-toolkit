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

// Implementation adapted and enhanced from https://github.com/Esri/arcgis-toolkit-sl-wpf
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Key = Windows.System.VirtualKey;
#elif __IOS__
using System.Drawing;
using Brush = UIKit.UIColor;
using TextBlock = UIKit.UILabel;
#elif __ANDROID__
using System.Drawing;
using Android.Content;
using Brush = Android.Graphics.Color;
using Size = Android.Util.Size;
using TextBlock = Android.Widget.TextView;
#else
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The TimeSlider is a utility Control that emits TimeExtent values typically for use with the Map Control
    /// to enhance the viewing of geographic features that have attributes based upon Date/Time information.
    /// </summary>
    public partial class TimeSlider
    {
        #region Fields

#pragma warning disable SA1306 // Field names must begin with lower-case letter (names are made to match template names)
#pragma warning disable SX1309 // Field names must begin with underscore (names are made to match template names)
        private TextBlock MinimumThumbLabel;
        private TextBlock MaximumThumbLabel;
        private TextBlock FullExtentStartTimeLabel;
        private TextBlock FullExtentEndTimeLabel;
        private Primitives.Tickbar Tickmarks;
#pragma warning restore SX1309 // Field names must begin with underscore
#pragma warning restore SA1306 // Field names must begin with lower-case letter
        private DispatcherTimer _playTimer;
        private TimeExtent _currentValue;
        private double _totalHorizontalChange;
        private TimeExtent _horizontalChangeExtent;
        private ThrottleAwaiter _layoutTimeStepsThrottler = new ThrottleAwaiter(1);
        private TaskCompletionSource<bool> _calculateTimeStepsTcs = new TaskCompletionSource<bool>();

#endregion // Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
#if !__ANDROID__
        public TimeSlider()
            : base()
        {
            Initialize();
        }
#endif

        private void Initialize()
        {
            InitializeImpl();
            _playTimer = new DispatcherTimer() { Interval = PlaybackInterval };
            _playTimer.Tick += PlayTimer_Tick;
        }

        /// <summary>
        /// Updates slider track UI components to display the specified time extent.
        /// </summary>
        /// <param name="extent">The time extent to display on the slider track.</param>
        private void UpdateTrackLayout(TimeExtent extent)
        {
            if (extent == null || extent.StartTime < ValidFullExtent.StartTime || extent.EndTime > ValidFullExtent.EndTime ||
            MinimumThumb == null || MaximumThumb == null || ValidFullExtent.EndTime <= ValidFullExtent.StartTime || SliderTrack == null ||
            TimeSteps == null || !TimeSteps.GetEnumerator().MoveNext())
            {
                return;
            }

            var sliderWidth = SliderTrack.GetActualWidth();
            var minimum = ValidFullExtent.StartTime.Ticks;
            var maximum = ValidFullExtent.EndTime.Ticks;

            // Snap the passed-in extent to valid time step intervals
            TimeExtent snapped = Snap(extent);

            // Convert the start and end time to ticks
            var start = snapped.StartTime.DateTime.Ticks;
            var end = snapped.EndTime.DateTime.Ticks;

            // margins
            double left, right, thumbLeft, thumbRight = 0;

            // rate = (distance) / (time)
            var rate = SliderTrack.GetActualWidth() / (maximum - minimum);

            // Position left repeater
            right = Math.Min(sliderWidth, ((maximum - start) * rate) + MaximumThumb.GetActualWidth());
            SliderTrackStepBackRepeater?.SetMargin(0, 0, right, 0);
            SliderTrackStepBackRepeater?.SetWidth(Math.Max(0, sliderWidth - right));

            // Margin adjustment for the minimum thumb label
            var thumbLabelWidthAdjustment = 0d;
            var minLabelLeftMargin = -1d;
            var minLabelRightMargin = -1d;
            var minThumbLabelWidth = 0d;

#if __ANDROID__
            // On Android, the slider track is positioned relative to the bounds of the parent.  So get the distance between the parent left and
            // track left to incorporate that into element positioning.
            var trackMarginOffset = SliderTrack.GetX();
#else
            var trackMarginOffset = 0;
#endif

            // Position minimum thumb
            if (!IsCurrentExtentTimeInstant)
            {
                // Check for two thumbs
                // There are two thumbs, so position minimum (max is used in both the one and two thumb case)
                left = Math.Max(0, (start - minimum) * rate);
                right = Math.Min(sliderWidth, (maximum - start) * rate);
#if NETFX_CORE
                // Accommodate issue on UWP where element sometimes actually renders with a width of one pixel less than margin values dictate
                left -= 0.5;
                right -= 0.5;
#endif
                thumbLeft = left - (MinimumThumb.GetActualWidth() / 2) + trackMarginOffset;
                thumbRight = right - (MinimumThumb.GetActualWidth() / 2) + trackMarginOffset;
                MinimumThumb.SetMargin(thumbLeft, 0, thumbRight, 0);

                var isVisible = LabelMode == TimeSliderLabelMode.CurrentExtent && start != minimum;

                // TODO: Change visibility instead of opacity.  Doing so throws an exception that start time cannot be
                // greater than end time when dragging minimum thumb.
                MinimumThumbLabel.SetOpacity(isVisible ? 1 : 0);

                // Calculate thumb label position
                minThumbLabelWidth = CalculateTextSize(MinimumThumbLabel).Width;
                thumbLabelWidthAdjustment = minThumbLabelWidth / 2;
                minLabelLeftMargin = left - thumbLabelWidthAdjustment + trackMarginOffset;
                minLabelRightMargin = Math.Min(sliderWidth, right - thumbLabelWidthAdjustment) + trackMarginOffset;
            }
            else
            {
                // There's only one thumb, so hide the min thumb
                MinimumThumb.SetMargin(0, 0, sliderWidth, 0);
                MinimumThumb.SetOpacity(0);
                MinimumThumbLabel.SetOpacity(0);
            }

            // Position middle thumb (filled area between min and max thumbs is actually a thumb and can be dragged)
            if (IsCurrentExtentTimeInstant)
            {
                // One thumb
                // Hide the middle thumb
                HorizontalTrackThumb.SetIsVisible(false);
            }
            else
            {
                // !IsCurrentExtentTimeInstant
                // Position the middle thumb
                left = Math.Min(sliderWidth, (start - minimum) * rate);
                right = Math.Min(sliderWidth, (maximum - end) * rate);
                HorizontalTrackThumb.SetMargin(left, 0, right, 0);
                HorizontalTrackThumb.SetWidth(Math.Max(0, sliderWidth - right - left));
                HorizontalTrackThumb.SetIsVisible(true);
#if !XAMARIN
                HorizontalTrackThumb.HorizontalAlignment = HorizontalAlignment.Left;
#endif
            }

            // Position maximum thumb
            left = Math.Min(sliderWidth, (end - minimum) * rate);
            right = Math.Min(sliderWidth, (maximum - end) * rate);
#if NETFX_CORE
            // Accommodate issue on UWP where element sometimes actually renders with a width of one pixel less than margin values dictate
            left -= 0.5;
            right -= 0.5;
#endif
            thumbLeft = left - (MaximumThumb.GetActualWidth() / 2) + trackMarginOffset;
            thumbRight = right - (MaximumThumb.GetActualWidth() / 2) + trackMarginOffset;
            MaximumThumb.SetMargin(thumbLeft, 0, thumbRight, 0);

            // Update maximum thumb label visibility
            MaximumThumbLabel.SetIsVisible(LabelMode == TimeSliderLabelMode.CurrentExtent && end != maximum && (!IsCurrentExtentTimeInstant || start != minimum));

            // Position maximum thumb label
            var maxThumbLabelWidth = CalculateTextSize(MaximumThumbLabel).Width;
            thumbLabelWidthAdjustment = maxThumbLabelWidth / 2;
            var maxLabelLeftMargin = left - thumbLabelWidthAdjustment + trackMarginOffset;
            var maxLabelRightMargin = Math.Min(sliderWidth, right - thumbLabelWidthAdjustment) + trackMarginOffset;

            // Handle possible thumb label collision and apply label positions
            if (!IsCurrentExtentTimeInstant && MinimumThumbLabel.GetOpacity() == 1)
            {
                if (MaximumThumbLabel.GetIsVisible())
                {
                    // Slider has min and max thumbs with both labels visible - check for label collision
                    var minLabelRight = minLabelLeftMargin + minThumbLabelWidth;

                    // Calculate the width of two characters and use that as the space to preserve between the min and max labels
                    var spaceBetweenLabels = CalculateTextSize(MinimumThumbLabel, text: "88").Width;

                    if (minLabelRight + spaceBetweenLabels > maxLabelLeftMargin)
                    {
                        // Labels will collide if centered on thumbs.  Adjust the position of each.
                        var overlap = minLabelRight + spaceBetweenLabels - maxLabelLeftMargin;
                        var collisionAdjustment = overlap / 2;
                        minLabelLeftMargin -= collisionAdjustment;
                        minLabelRightMargin += collisionAdjustment;
                        maxLabelLeftMargin += collisionAdjustment;
                        maxLabelRightMargin -= collisionAdjustment;
                    }
                }

                // Apply position to min label
                MinimumThumbLabel.SetMargin(minLabelLeftMargin, 0, minLabelRightMargin, 0);
            }

            MaximumThumbLabel.SetMargin(maxLabelLeftMargin, 0, maxLabelRightMargin, 0);

            // Position right repeater
            left = Math.Min(sliderWidth, ((end - minimum) * rate) + MaximumThumb.GetActualWidth());
            SliderTrackStepForwardRepeater?.SetMargin(left, 0, 0, 0);
            SliderTrackStepForwardRepeater?.SetWidth(Math.Max(0, sliderWidth - left));
        }

        /// <summary>
        /// Calculates the size of the specified text using the specified TextBlock's font properites.
        /// </summary>
        /// <param name="textBlock">The TextBlock to use in the size calculation.</param>
        /// <param name="text">The text to calculate the size for.  If unspecified, the size of the TextBlock's text will be calculated.</param>
        /// <returns>The size of the text.</returns>
        /// <remarks>This method is useful in cases where a TextBlock's text has updated, but its layout has not.  In such cases,
        /// the ActualWidth and ActualHeight properties are not representative of the new text.</remarks>
        private Size CalculateTextSize(TextBlock textBlock, string text = null)
        {
#if NETFX_CORE
            // Create a dummy TextBlock to calculate the size of the text.  Note that only a limited number of properties
            // are copied here.  This may yield an incorrect size if additional properties are specified in the slider's
            // style that affect the text size,
            var tb = new TextBlock()
            {
                FontFamily = textBlock.FontFamily,
                FontSize = textBlock.FontSize,
                FontStyle = textBlock.FontStyle,
                FontWeight = textBlock.FontWeight,
                Text = text ?? textBlock.Text,
            };
            tb.Measure(new Size(0, 0));
            tb.Arrange(new Rect(0, 0, 0, 0));
            return new Size(tb.ActualWidth, tb.ActualHeight);
#elif __IOS__
            var label = new UIKit.UILabel()
            {
                Text = text ?? textBlock.Text,
                Font = textBlock.Font,
                LineBreakMode = textBlock.LineBreakMode,
            };
            return (Size)label.SizeThatFits(new CoreGraphics.CGSize(double.MaxValue, double.MaxValue));
#elif __ANDROID__
            string oldText = textBlock.Text;
            if (!string.IsNullOrEmpty(text))
            {
                textBlock.Text = text;
            }

            var size = GetDesiredSize(textBlock);

            if (!string.IsNullOrEmpty(text))
            {
                textBlock.Text = oldText;
            }

            return size;
#else
            var typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
            var formattedText = new FormattedText(text ?? textBlock.Text, CultureInfo.CurrentCulture, textBlock.FlowDirection, typeface,
                textBlock.FontSize, textBlock.Foreground, new NumberSubstitution(), TextFormattingMode.Display);
            return new Size(formattedText.Width, formattedText.Height);
#endif
        }

#region Drag event handlers

        private void OnCurrentExtentThumbDrag(double translateX)
        {
            IsPlaying = false;

            if (translateX == 0 || IsStartTimePinned || IsEndTimePinned)
            {
                return;
            }

            if (_currentValue == null)
            {
                _currentValue = CurrentValidExtent;
            }

            _totalHorizontalChange = translateX;

            _horizontalChangeExtent = new TimeExtent(_currentValue.StartTime, _currentValue.EndTime);

            // time ratio
            long timeRate = (ValidFullExtent.EndTime.Ticks - ValidFullExtent.StartTime.Ticks) / (long)SliderTrack.GetActualWidth();

            // time change
            long timeChange = (long)(timeRate * _totalHorizontalChange);

            TimeSpan difference = new TimeSpan(_currentValue.EndTime.DateTime.Ticks - _currentValue.StartTime.DateTime.Ticks);

            TimeExtent tempChange = null;
            try
            {
                tempChange = new TimeExtent(_horizontalChangeExtent.StartTime.DateTime.AddTicks(timeChange),
                    _horizontalChangeExtent.EndTime.DateTime.AddTicks(timeChange));
            }
            catch (ArgumentOutOfRangeException)
            {
                if (_totalHorizontalChange < 0)
                {
                    tempChange = new TimeExtent(ValidFullExtent.StartTime, ValidFullExtent.StartTime.Add(difference));
                }
                else if (_totalHorizontalChange > 0)
                {
                    tempChange = new TimeExtent(ValidFullExtent.EndTime.Subtract(difference), ValidFullExtent.EndTime);
                }
            }

            if (tempChange.StartTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks)
            {
                _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime, ValidFullExtent.StartTime.Add(difference)));
            }
            else if (tempChange.EndTime.DateTime.Ticks > ValidFullExtent.EndTime.Ticks)
            {
                _currentValue = Snap(new TimeExtent(ValidFullExtent.EndTime.Subtract(difference), ValidFullExtent.EndTime));
            }
            else
            {
                _currentValue = Snap(new TimeExtent(tempChange.StartTime, tempChange.EndTime));
            }

            UpdateTrackLayout(_currentValue);
            if (_currentValue.StartTime != CurrentExtent.StartTime || _currentValue.EndTime != CurrentExtent.EndTime)
            {
                UpdateCurrentExtent();
            }
        }

        private void OnMinimumThumbDrag(double translateX)
        {
            IsPlaying = false;
            if (translateX == 0 || CurrentExtent == null)
            {
                return;
            }

            if (_currentValue == null)
            {
                _currentValue = CurrentValidExtent;
            }

            _totalHorizontalChange = translateX;
            _horizontalChangeExtent = new TimeExtent(_currentValue.StartTime, _currentValue.EndTime);

            // time ratio
            long timeRate = (ValidFullExtent.EndTime.Ticks - ValidFullExtent.StartTime.Ticks) / (long)SliderTrack.GetActualWidth();

            // time change
            long timeChange = (long)(timeRate * _totalHorizontalChange);

            TimeExtent tempChange = null;
            try
            {
                var newStart = _horizontalChangeExtent.StartTime.DateTime.AddTicks(timeChange);
                if (newStart >= _horizontalChangeExtent.EndTime)
                {
                    return; // Don't allow moving thumb so that min would be equal or greater than max
                }

                tempChange = new TimeExtent(newStart, _horizontalChangeExtent.EndTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                if (_totalHorizontalChange < 0)
                {
                    tempChange = new TimeExtent(ValidFullExtent.StartTime, _currentValue.EndTime);
                }
                else if (_totalHorizontalChange > 0)
                {
                    tempChange = new TimeExtent(_currentValue.EndTime);
                }
            }

            if (tempChange.StartTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks)
            {
                _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime, _currentValue.EndTime));
            }
            else if (tempChange.StartTime >= _currentValue.EndTime)
            {
                _currentValue = Snap(new TimeExtent(_currentValue.EndTime));
            }
            else
            {
                _currentValue = Snap(new TimeExtent(tempChange.StartTime, tempChange.EndTime));
            }

            UpdateTrackLayout(_currentValue);
            if (_currentValue.StartTime != CurrentExtent.StartTime)
            {
                UpdateCurrentExtent();
            }
        }

        private void OnMaximumThumbDrag(double translateX)
        {
            IsPlaying = false;
            if (translateX == 0 || CurrentExtent == null)
            {
                return;
            }

            if (_currentValue == null)
            {
                _currentValue = CurrentValidExtent;
            }

            _totalHorizontalChange = translateX;
            _horizontalChangeExtent = new TimeExtent(_currentValue.StartTime, _currentValue.EndTime);

            // time ratio
            long timeRate = (ValidFullExtent.EndTime.Ticks - ValidFullExtent.StartTime.Ticks) / (long)SliderTrack.GetActualWidth();

            // time change
            long timeChange = (long)(timeRate * _totalHorizontalChange);

            TimeExtent tempChange = null;
            if (IsCurrentExtentTimeInstant)
            {
                try
                {
                    // If the mouse drag creates a date thats year is before
                    // 1/1/0001 or after 12/31/9999 then an out of renge
                    // exception will be trown.
                    tempChange = new TimeExtent(_horizontalChangeExtent.EndTime.DateTime.AddTicks(timeChange));
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (_totalHorizontalChange > 0)
                    {
                        // date is after 12/31/9999
                        tempChange = new TimeExtent(ValidFullExtent.EndTime);
                    }
                    else if (_totalHorizontalChange < 0)
                    {
                        // date is before 1/1/0001
                        tempChange = new TimeExtent(ValidFullExtent.StartTime);
                    }
                }
            }
            else
            {
                try
                {
                    var newEnd = _horizontalChangeExtent.EndTime.DateTime.AddTicks(timeChange);
                    if (newEnd <= _horizontalChangeExtent.StartTime)
                    {
                        return; // Don't allow moving thumb so that max would be equal or less than min
                    }

                    // If the mouse drag creates a date thats year is before
                    // 1/1/0001 or after 12/31/9999 then an out of range
                    // exception will be trown.
                    tempChange = new TimeExtent(_horizontalChangeExtent.StartTime, newEnd);
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (_totalHorizontalChange > 0)
                    {
                        // date is after 12/31/9999
                        tempChange = new TimeExtent(_currentValue.StartTime, ValidFullExtent.EndTime);
                    }
                    else if (_totalHorizontalChange < 0)
                    {
                        // date is before 1/1/0001
                        tempChange = new TimeExtent(_currentValue.StartTime);
                    }
                }
            }

            // validate change
            if (IsCurrentExtentTimeInstant)
            {
                if (tempChange.EndTime.DateTime.Ticks > ValidFullExtent.EndTime.Ticks)
                {
                    _currentValue = Snap(new TimeExtent(ValidFullExtent.EndTime));
                }
                else if (tempChange.EndTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks)
                {
                    _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime));
                }
                else
                {
                    _currentValue = Snap(new TimeExtent(tempChange.EndTime));
                }
            }
            else
            {
                if (tempChange.EndTime.DateTime.Ticks > ValidFullExtent.EndTime.Ticks)
                {
                    _currentValue = Snap(new TimeExtent(_currentValue.StartTime, ValidFullExtent.EndTime));
                }
                else if (tempChange.EndTime <= _currentValue.StartTime && !IsCurrentExtentTimeInstant)
                {
                    // TODO: Preserve one time step between min and max thumbs
                    _currentValue = Snap(new TimeExtent(_currentValue.StartTime, _currentValue.StartTime.DateTime.AddMilliseconds(1)));
                }
                else if (tempChange.EndTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks)
                {
                    // TODO: Preserve one time step between min and max thumbs
                    _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime));
                }
                else
                {
                    _currentValue = Snap(new TimeExtent(_currentValue.StartTime, tempChange.EndTime));
                }
            }

            UpdateTrackLayout(_currentValue);
            if (_currentValue.EndTime != CurrentExtent.EndTime)
            {
                UpdateCurrentExtent();
            }
        }

        private void OnDragCompleted()
        {
            if (_currentValue == null)
            {
                return;
            }

            UpdateCurrentExtent();
        }

        private void UpdateCurrentExtent()
        {
            var newStartTime = IsStartTimePinned ? CurrentValidExtent.StartTime : _currentValue.StartTime;
            var newEndTime = IsEndTimePinned ? CurrentValidExtent.EndTime : _currentValue.EndTime;
            if (newStartTime != CurrentExtent.StartTime || newEndTime != CurrentExtent.EndTime)
            {
                var newTimeExtent = Snap(new TimeExtent(newStartTime, newEndTime));
                CurrentExtent = newTimeExtent;
            }
        }

#endregion // Drag event handlers

        /// <summary>
        /// Adjusts the specified time extent so that it starts and ends at a valid time step interval.
        /// </summary>
        /// <param name="extent">The time extent to adjust.</param>
        /// <returns>The snapped time extent.</returns>
        private TimeExtent Snap(TimeExtent extent)
        {
            if (extent == null)
            {
                return null;
            }

            if (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext())
            {
                if (TimeSteps.Contains(extent.StartTime) && TimeSteps.Contains(extent.EndTime))
                {
                    return extent; // Extent already starts and ends at valid time steps
                }

                var start = extent.StartTime < ValidFullExtent.StartTime ? ValidFullExtent.StartTime : extent.StartTime;
                var end = extent.EndTime > ValidFullExtent.EndTime ? ValidFullExtent.EndTime : extent.EndTime;
                var result = new TimeExtent(start, end);

                // snap min thumb.
                var d0 = long.MaxValue;
                foreach (DateTimeOffset step in TimeSteps)
                {
                    var delta = Math.Abs((step - start).Ticks);
                    if (delta < d0)
                    {
                        if (CurrentExtent?.StartTime == CurrentExtent?.EndTime)
                        {
                            d0 = delta;
                            result = new TimeExtent(step);
                        }
                        else if (step < end)
                        {
                            d0 = delta;
                            result = new TimeExtent(step, result.EndTime);
                        }
                    }
                }

                var d1 = long.MaxValue;
                foreach (DateTimeOffset d in TimeSteps)
                {
                    var delta = Math.Abs((d - end).Ticks);
                    if (delta < d1)
                    {
                        if (CurrentExtent?.StartTime == CurrentExtent?.EndTime)
                        {
                            d1 = delta;
                            result = new TimeExtent(d);
                        }
                        else if (d > result.StartTime)
                        {
                            d1 = delta;
                            result = new TimeExtent(result.StartTime, d);
                        }
                    }
                }

                // Return snapped extent
                return result;
            }
            else
            {
                return extent;
            }
        }

        /// <summary>
        /// Positions the tickmarks along the slider's tick bar.
        /// </summary>
        private void PositionTickmarks()
        {
            if (Tickmarks == null || ValidFullExtent.StartTime >= ValidFullExtent.EndTime)
            {
                return;
            }

            if (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext())
            {
                var span = ValidFullExtent.EndTime.Ticks - ValidFullExtent.StartTime.Ticks;
                var intervals = new List<double>();
                var tickMarkDates = new List<DateTimeOffset>();

                // Create a tick mark for every time step from the 2nd to the 2nd to last.  We don't create ticks
                // here for the first and last time step  because those are explicitly placed in the control template.
                for (int i = 1; i < TimeSteps.Count() - 1; i++)
                {
                    var d = TimeSteps.ElementAt(i);
                    intervals.Add((d.Ticks - ValidFullExtent.StartTime.Ticks) / (double)span);
                    tickMarkDates.Add(d);
                }

                Tickmarks.TickmarkDataSources = tickMarkDates.Cast<object>();
                Tickmarks.TickmarkPositions = intervals;
                Tickmarks.ShowTickLabels = LabelMode == TimeSliderLabelMode.TimeStepInterval;
            }
            else
            {
                Tickmarks.TickmarkPositions = null;
            }
        }

        #region Properties

        /// <summary>
        /// Gets a value indicating whether or not the current extent represents a time instant.
        /// </summary>
        private bool IsCurrentExtentTimeInstant => CurrentValidExtent?.IsTimeInstant() ?? false;

        /// <summary>
        /// Gets the current FullExtent or, if unavailable, a valid substitute.
        /// </summary>
        private TimeExtent ValidFullExtent => FullExtent ?? new TimeExtent(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

        /// <summary>
        /// Gets the CurrentExtent or, if invalid, a valid substitute.
        /// </summary>
        private TimeExtent CurrentValidExtent
        {
            get
            {
                if (CurrentExtent == null)
                {
                    return FullExtent ?? ValidFullExtent;
                }
                else
                {
                    return Snap(CurrentExtent);
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> associated with the visual thumbs(s) displayed on the TimeSlider.
        /// </summary>
#if !NETFX_CORE && !XAMARIN
        [TypeConverter(typeof(TimeExtentConverter))]
#endif
        public TimeExtent CurrentExtent
        {
            get => CurrentExtentImpl;
            set => CurrentExtentImpl = value;
        }

        private void OnCurrentExtentPropertyChanged(TimeExtent newExtent, TimeExtent oldExtent)
        {
            _currentValue = newExtent;

#if !XAMARIN
            // Explicitly update the thumb labels' bindings to ensure that their text is updated prior to calculating layout
            MinimumThumbLabel?.RefreshBinding(TextBlock.TextProperty);
            MaximumThumbLabel?.RefreshBinding(TextBlock.TextProperty);
#endif

            UpdateTrackLayout(CurrentValidExtent);

            // If the new extent represents a time instant, enforce the pinning of start and end time being in sync
            if (newExtent.IsTimeInstant() && IsStartTimePinned != IsEndTimePinned)
            {
                IsStartTimePinned = false;
                IsEndTimePinned = false;
            }

            CurrentExtentChanged?.Invoke(this, new TimeExtentChangedEventArgs(newExtent, oldExtent));
        }

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> that specifies the overall start and end time of the time slider instance.
        /// </summary>
#if !NETFX_CORE && !XAMARIN
        [TypeConverter(typeof(TimeExtentConverter))]
#endif
        public TimeExtent FullExtent
        {
            get => FullExtentImpl;
            set => FullExtentImpl = value;
        }

        private void OnFullExtentPropertyChanged()
        {
            // TODO: For consideration - should FullExtent be snapped to a multiple of the specified time step?  E.g. if the time step interval
            // is 1 week and the full extent is 3 months (suppose 92 days in this case), should the full extent be snapped to 91 or 98 days?
            CalculateTimeSteps();
        }

        /// <summary>
        /// Gets or sets the time step intervals for the time slider.  The slider thumbs will snap to and tick marks will be shown at this interval.
        /// </summary>
        public TimeValue TimeStepInterval
        {
            get => TimeStepIntervalImpl;
            set => TimeStepIntervalImpl = value;
        }

        private void OnTimeStepIntervalPropertyChanged()
        {
            CalculateTimeSteps();
        }

        /// <summary>
        /// Updates time steps based on the current TimeStepInterval and FullExtent.  Executes asynchronously to ensure that pending updates to
        /// both TimeStepInterval and FullExtent are taken into account before intervals and subsequent layout updates are calculated.
        /// </summary>
        private void CalculateTimeSteps()
        {
            if (TimeStepInterval == null || FullExtent == null)
            {
                return;
            }

            var timeStep = TimeStepInterval;
            var startTime = FullExtent.StartTime;
            var endTime = FullExtent.EndTime;

            var steps = new List<DateTimeOffset> { startTime };

            for (var nextStep = startTime.AddTimeValue(timeStep); nextStep <= endTime; nextStep = nextStep.AddTimeValue(timeStep))
            {
                steps.Add(nextStep);
            }

            TimeSteps = steps.AsReadOnly();
            _calculateTimeStepsTcs.TrySetResult(true);
        }

        /// <summary>
        /// Gets the time steps that can be used to set the slider instance's current extent.
        /// </summary>
        public IReadOnlyList<DateTimeOffset> TimeSteps
        {
            get => TimeStepsImpl;
            private set => TimeStepsImpl = value;
        }

        private async void OnTimeStepsPropertyChanged()
        {
            try
            {
                // Time step application is throttled because FullExtent and TimeStepInterval are often changed in concert, and immediately updating the layout
                // based on changes in one can result in undesirable behavior.  For instance, suppose TimeStepInterval is 1 day and FullExtent is 14 days, yielding
                // 14 time steps.  Then suppose FullExtent is changed to 10 years, and TimeStepInterval to 1 year.  If the FullExtent change is responded to
                // without accounting for the change in TimeStepInterval, the slider will try to update to accommodate ~3650 time step intervals before updating
                // again to reduce this number to 10.  In this case, the unwieldy number of intervals will make the slider seem to become unresponsive for a
                // time.  But in any cases where both TimeStepInterval and FullExtent are updated together, there will be an inefficient double-execution of
                // layout logic.
                await _layoutTimeStepsThrottler.ThrottleDelay();

                // Update the slider UI to reflect the new time steps
                PositionTickmarks();
                SetButtonVisibility();
                UpdateTrackLayout(CurrentValidExtent);
            }
            catch
            {
                // This would be unexpected, but this is here just in case we encounter an exception.  This is in an async void method, so
                // there's no way to bubble this exception to user code.
            }
        }

        /// <summary>
        /// Gets or sets the interval at which the time slider's current extent will move to the next or previous time step.
        /// </summary>
        public TimeSpan PlaybackInterval
        {
            get => PlaybackIntervalImpl;
            set => PlaybackIntervalImpl = value;
        }

        private void OnPlaybackIntervalPropertyChanged(TimeSpan interval) => _playTimer.Interval = interval;

        /// <summary>
        /// Gets or sets whether the current extent will move to the next or the previous time step during playback.
        /// </summary>
        public PlaybackDirection PlaybackDirection
        {
            get => PlaybackDirectionImpl;
            set => PlaybackDirectionImpl = value;
        }

        /// <summary>
        /// Gets or sets the behavior when the current extent reaches the end of the slider during playback.
        /// </summary>
        public LoopMode PlaybackLoopMode
        {
            get => PlaybackLoopModeImpl;
            set => PlaybackLoopModeImpl = value;
        }

        private void SetButtonVisibility()
        {
            var viz = TimeSteps != null && TimeSteps.GetEnumerator().MoveNext();
            PlayPauseButton?.SetIsVisible(viz);
            NextButton?.SetIsVisible(viz);
            PreviousButton?.SetIsVisible(viz);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the start time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        public bool IsStartTimePinned
        {
            get => IsStartTimePinnedImpl;
            set => IsStartTimePinnedImpl = value;
        }

        private void OnIsStartTimePinnedChanged(bool isStartTimePinned)
        {
            // Enable or disable the start time thumb
            MinimumThumb.SetIsEnabled(!isStartTimePinned);

            // If the slider extent is a time instant, keep whether start time and end time are pinned in sync
            if (IsCurrentExtentTimeInstant && IsEndTimePinned != isStartTimePinned)
            {
                IsEndTimePinned = isStartTimePinned;
            }

#if !__IOS__
            // Enable or disable the middle thumb based on whether both the start and end thumbs are pinned
            var enableHorizontalTrackThumb = MaximumThumb.GetIsEnabled() && MinimumThumb.GetIsEnabled();
            HorizontalTrackThumb.SetIsEnabled(enableHorizontalTrackThumb);
#if __ANDROID__
            MinimumThumb.Visibility = isStartTimePinned ? Android.Views.ViewStates.Invisible : Android.Views.ViewStates.Visible;
            PinnedMinimumThumb.Visibility = isStartTimePinned ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
#endif
#endif
        }

        /// <summary>
        /// Gets or sets a value indicating whether the end time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        public bool IsEndTimePinned
        {
            get => IsEndTimePinnedImpl;
            set => IsEndTimePinnedImpl = value;
        }

        private void OnIsEndTimePinnedChanged(bool isEndTimePinned)
        {
            // Enable or disable the end time thumb
            MaximumThumb.SetIsEnabled(!isEndTimePinned);

            // If the slider extent is a time instant, keep whether start time and end time are pinned in sync
            if (IsCurrentExtentTimeInstant && IsStartTimePinned != isEndTimePinned)
            {
                IsStartTimePinned = isEndTimePinned;
            }

#if !__IOS__
            // Enable or disable the middle thumb based on whether both the start and end thumbs are pinned
            var enableHorizontalTrackThumb = MaximumThumb.GetIsEnabled() && MinimumThumb.GetIsEnabled();
            HorizontalTrackThumb.SetIsEnabled(enableHorizontalTrackThumb);
#if __ANDROID__
            MaximumThumb.Visibility = isEndTimePinned ? Android.Views.ViewStates.Invisible : Android.Views.ViewStates.Visible;
            PinnedMaximumThumb.Visibility = isEndTimePinned ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
#endif
#endif
        }

        /// <summary>
        /// Gets or sets a value indicating whether the time slider is animating playback.
        /// </summary>
        public bool IsPlaying
        {
            get => IsPlayingImpl;
            set => IsPlayingImpl = value;
        }

        private void OnIsPlayingPropertyChanged(bool isPlaying)
        {
            // Start or stop playback
            if (isPlaying && TimeSteps != null && TimeSteps.GetEnumerator().MoveNext())
            {
                if ((IsStartTimePinned && IsEndTimePinned) ||
                (IsCurrentExtentTimeInstant && IsEndTimePinned))
                {
                    // Can't start playback because current time extent is pinned
                    IsPlaying = false;
                    return;
                }
                else
                {
                    _playTimer.Start();
                }
            }
            else
            {
                _playTimer.Stop();
            }

            // Update the state of the play/pause button
            if (PlayPauseButton != null)
            {
#if __ANDROID__
                PlayPauseButton.Checked = isPlaying;
#else
                PlayPauseButton.IsChecked = isPlaying;
#endif
            }
        }

#region Appearance Properties

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="FullExtent"/>.
        /// </summary>
        public string FullExtentLabelFormat
        {
            get => FullExtentLabelFormatImpl;
            set => FullExtentLabelFormatImpl = value;
        }

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="CurrentExtent"/>.
        /// </summary>
        public string CurrentExtentLabelFormat
        {
            get => CurrentExtentLabelFormatImpl;
            set => CurrentExtentLabelFormatImpl = value;
        }

        private void OnCurrentExtentLabelFormatPropertyChanged(string labelFormat)
        {
            // Layout the slider to accommodate updated label text
            UpdateTrackLayout(CurrentExtent);
        }

        /// <summary>
        /// Gets or sets the string format to use for displaying the labels for the tick marks representing each time step interval.
        /// </summary>
        public string TimeStepIntervalLabelFormat
        {
            get => TimeStepIntervalLabelFormatImpl;
            set => TimeStepIntervalLabelFormatImpl = value;
        }

        private void OnTimeStepIntervalLabelFormatPropertyChanged(string tickLabelFormat)
        {
            if (Tickmarks != null)
            {
                Tickmarks.TickLabelFormat = tickLabelFormat;
            }
        }

        /// <summary>
        /// Gets or sets the mode to use for labels along the time slider.
        /// </summary>
        public TimeSliderLabelMode LabelMode
        {
            get => LabelModeImpl;
            set => LabelModeImpl = value;
        }

        private void OnLabelModePropertyChanged(TimeSliderLabelMode labelMode) => ApplyLabelMode(labelMode);

        /// <summary>
        /// Updates the slider for the specified label mode.
        /// </summary>
        /// <param name="labelMode">The label mode to apply.</param>
        private void ApplyLabelMode(TimeSliderLabelMode labelMode)
        {
            if (Tickmarks == null || MinimumThumbLabel == null || MaximumThumbLabel == null)
            {
                return;
            }

            switch (labelMode)
            {
                case TimeSliderLabelMode.None:
                    Tickmarks.ShowTickLabels = false;
                    MinimumThumbLabel.SetIsVisible(false);
                    MaximumThumbLabel.SetIsVisible(false);
                    break;
                case TimeSliderLabelMode.CurrentExtent:
                    Tickmarks.ShowTickLabels = false;
                    MinimumThumbLabel.SetIsVisible(true);
                    MaximumThumbLabel.SetIsVisible(true);
                    break;
                case TimeSliderLabelMode.TimeStepInterval:
                    Tickmarks.ShowTickLabels = true;
                    MinimumThumbLabel.SetIsVisible(false);
                    MaximumThumbLabel.SetIsVisible(false);
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the border color of the thumbs.
        /// </summary>
        public Brush ThumbStroke
        {
            get => ThumbStrokeImpl;
            set => ThumbStrokeImpl = value;
        }

        /// <summary>
        /// Gets or sets the fill color of the thumbs.
        /// </summary>
        public Brush ThumbFill
        {
            get => ThumbFillImpl;
            set => ThumbFillImpl = value;
        }

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="CurrentExtent"/>.
        /// </summary>
        public Brush CurrentExtentFill
        {
            get => CurrentExtentFillImpl;
            set => CurrentExtentFillImpl = value;
        }

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        public Brush FullExtentFill
        {
            get => FullExtentFillImpl;
            set => FullExtentFillImpl = value;
        }

        /// <summary>
        /// Gets or sets the border color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        public Brush FullExtentStroke
        {
            get => FullExtentStrokeImpl;
            set => FullExtentStrokeImpl = value;
        }

        /// <summary>
        /// Gets or sets the color of the slider's tickmarks.
        /// </summary>
        public Brush TimeStepIntervalTickFill
        {
            get => TimeStepIntervalTickFillImpl;
            set => TimeStepIntervalTickFillImpl = value;
        }

        /// <summary>
        /// Gets or sets the fill color of the playback buttons.
        /// </summary>
        public Brush PlaybackButtonsFill
        {
            get => PlaybackButtonsFillImpl;
            set => PlaybackButtonsFillImpl = value;
        }

        /// <summary>
        /// Gets or sets the border color of the playback buttons.
        /// </summary>
        public Brush PlaybackButtonsStroke
        {
            get => PlaybackButtonsStrokeImpl;
            set => PlaybackButtonsStrokeImpl = value;
        }

        /// <summary>
        /// Gets or sets the color of the full extent labels.
        /// </summary>
        public Brush FullExtentLabelColor
        {
            get => FullExtentLabelColorImpl;
            set => FullExtentLabelColorImpl = value;
        }

        /// <summary>
        /// Gets or sets the color of the current extent labels.
        /// </summary>
        public Brush CurrentExtentLabelColor
        {
            get => CurrentExtentLabelColorImpl;
            set => CurrentExtentLabelColorImpl = value;
        }

        /// <summary>
        /// Gets or sets the color of the time step interval labels.
        /// </summary>
        public Brush TimeStepIntervalLabelColor
        {
            get => TimeStepIntervalLabelColorImpl;
            set => TimeStepIntervalLabelColorImpl = value;
        }

#endregion // Appearance Properties

#endregion // Properties

#region Initialization Helper Methods

        /// <summary>
        /// Updates the time slider to have the specified number of time steps.
        /// </summary>
        /// <param name="count">The number of time steps.</param>
        /// <remarks>This method divides the TimeSlider instance's <see cref="FullExtent"/> into the number of steps specified,
        /// updating the <see cref="TimeStepInterval"/> and <see cref="TimeSteps"/> properties.  The method will attempt to set
        /// the interval to a TimeValue with the smallest duration and largest time unit that will fit evenly (i.e. without
        /// fractional duration values) into the TimeSlider's full extent.  If there is no TimeValue that will fit evenly, then
        /// the interval will be initialized to the smallest possible fractional duration that is greater than one with a time
        /// unit of days or smaller.
        ///
        /// Note that, if the TimeSlider instance's FullExtent property is not set, invoking this method will have no effect.</remarks>
        public void InitializeTimeSteps(int count)
        {
            if (FullExtent == null)
            {
                return;
            }

            TimeStepInterval = FullExtent.Divide(count);
        }

        /// <summary>
        /// Initializes the time slider's temporal properties based on the specified GeoView. Specifically,
        /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>.
        /// </summary>
        /// <param name="geoView">The GeoView to use to initialize the time-slider's properties.</param>
        /// <returns>Task.</returns>
        public async Task InitializeTimePropertiesAsync(GeoView geoView)
        {
            // Get all the layers from the geoview
            var allLayers = geoView is MapView mapView ? mapView.Map.AllLayers : ((SceneView)geoView).Scene.AllLayers;

            // Get all the layers that are visible and are participating in time-based filtering
            var temporallyActiveLayers = allLayers.Where(l =>
                l is ITimeAware timeAware && timeAware.IsTimeFilteringEnabled && l.IsVisible).Select(l => (ITimeAware)l);

            TimeExtent fullExtent = null;
            TimeValue timeStepInterval = null;
            var canUseInstantaneousTime = true;

            // Iterate each temporal layer to determine their combined temporal extent, the maximum time step interval among them, and whether
            // each of them can be filtered by a time instant.  If any cannot be filtered by a time instant, then a temporal range will be used
            // for filtering.
            foreach (var layer in temporallyActiveLayers)
            {
                fullExtent = fullExtent == null ? layer.FullTimeExtent : fullExtent.Union(layer.FullTimeExtent);
                var layerTimeStepInterval = await GetTimeStepIntervalAsync(layer);
                timeStepInterval = timeStepInterval == null ? layerTimeStepInterval :
                    layerTimeStepInterval.IsGreaterThan(timeStepInterval) ? layerTimeStepInterval : timeStepInterval;

                // Only check whether a time instant can be used for filtering if the GeoView doesn't have a defined temporal extent and all
                // the layers checked so far allow instantaneous filtration.
                if (geoView.TimeExtent == null && canUseInstantaneousTime && !(await CanUseInstantaneousTimeAsync(layer)))
                {
                    canUseInstantaneousTime = false;
                }
            }

            // Apply the calculated full extent, time step interval, and current extent
            FullExtent = fullExtent;
            TimeStepInterval = timeStepInterval;

            // If the geoview has a temporal extent defined, use that.  Otherwise, initialize the current extent to either the
            // full extent's start (if instantaneous time-filtering can be used), or to the entire full extent.
            CurrentExtent = geoView.TimeExtent == null ? geoView.TimeExtent : canUseInstantaneousTime ?
                new TimeExtent(FullExtent.StartTime) : new TimeExtent(FullExtent.StartTime, FullExtent.EndTime);
        }

        /// <summary>
        /// Initializes the time slider's temporal properties based on the specified time-aware layer. Specifically,
        /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>.
        /// </summary>
        /// <param name="timeAwareLayer">The layer to use to initialize the time slider.</param>
        /// <returns>Task.</returns>
        public async Task InitializeTimePropertiesAsync(ITimeAware timeAwareLayer)
        {
            if (timeAwareLayer is ILoadable loadable)
            {
                await loadable.LoadAsync();
            }

            // Apply full extent and time step interval to slider properties
            var fullExtent = timeAwareLayer.FullTimeExtent;
            var timeStepInterval = await GetTimeStepIntervalAsync(timeAwareLayer);

            if (timeStepInterval != null)
            {
                // Check whether the time-aware layer supports filtering based on a time instant
                var canUseInstantaneousTime = await CanUseInstantaneousTimeAsync(timeAwareLayer);

                // Apply full extent and time step interval to slider properties
                FullExtent = fullExtent;
                TimeStepInterval = timeStepInterval;

                // TODO: Double-check whether we can choose a better default for current extent - does not seem to be exposed
                // at all in service metadata
                CurrentExtent = canUseInstantaneousTime ?
                    new TimeExtent(FullExtent.StartTime) : new TimeExtent(FullExtent.StartTime, TimeSteps.ElementAt(1));

                // TODO: Initialize time-zone (will require converting time zone string to strong type)
            }
            else
            {
                // TODO: What should happen in this case?  Take a guess at a time step interval and current
                // extent?  Currently, this is a no-op.
            }
        }

        /// <summary>
        /// Gets the default time-step interval for the specified time-aware layer.
        /// </summary>
        /// <param name="timeAwareLayer">The time-aware layer to retrieve the interval for.</param>
        /// <returns>The interval, represented as a <see cref="TimeValue"/> instance.</returns>
        private static async Task<TimeValue> GetTimeStepIntervalAsync(ITimeAware timeAwareLayer)
        {
            if (timeAwareLayer is ILoadable loadable)
            {
                await loadable.LoadAsync();
            }

            var timeStepInterval = timeAwareLayer.TimeInterval;

            // For map image layers, if the map service does not have a time step interval, check the time step intervals
            // of the service's sub-layers
            if (timeStepInterval == null)
            {
                if (timeAwareLayer is ArcGISMapImageLayer mapImageLayer)
                {
                    // Get the largest time-step interval defined by the service's sub-layers
                    foreach (var sublayer in mapImageLayer.Sublayers)
                    {
                        if (sublayer.IsVisible)
                        {
                            // Only use visible sub-layers
                            var timeInfo = await GetTimeInfoAsync(sublayer);
                            if (timeInfo == null)
                            {
                                continue;
                            }

                            if (timeInfo != null && (timeStepInterval == null || timeInfo.Interval.IsGreaterThan(timeStepInterval)))
                            {
                                timeStepInterval = timeInfo.Interval;
                            }
                        }
                    }
                }
                else if (timeAwareLayer is ILoadable loadableLayer)
                {
                    var timeInfo = await GetTimeInfoAsync(loadableLayer);
                    timeStepInterval = timeInfo?.Interval;
                }
            }

            return timeStepInterval;
        }

        /// <summary>
        /// Gets whether an instantaneous time filter can be applied to the specified time-aware layer.
        /// </summary>
        /// <param name="timeAwareLayer">The time-aware layer to check.</param>
        /// <returns><c>true</c> if the layer can be filtered based on a time instant, otherwise <c>false</c>.</returns>
        private static async Task<bool> CanUseInstantaneousTimeAsync(ITimeAware timeAwareLayer)
        {
            var canUseInstantaneousTime = true;

            if (timeAwareLayer is ArcGISMapImageLayer mapImageLayer)
            {
                // Check visible sub-layers for start-time and end-time fields.  If every one has both start-time
                // and end-time fields, then the map image layer can be filtered based on a time instant.  Otherwise,
                // it must be filtered by a time range.
                foreach (var sublayer in mapImageLayer.Sublayers)
                {
                    if (sublayer.IsVisible)
                    {
                        var timeInfo = await GetTimeInfoAsync(sublayer);
                        if (timeInfo == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(timeInfo.StartTimeField) || string.IsNullOrEmpty(timeInfo.EndTimeField))
                        {
                            canUseInstantaneousTime = false; // Instantaneous time filtering won't work for this sub-layer
                            break;
                        }
                    }
                }
            }
            else
            {
                // !(timeAwareLayer is ArcGISMapImageLayer)
                var timeInfo = await GetTimeInfoAsync(timeAwareLayer as ILoadable);
                canUseInstantaneousTime = !string.IsNullOrEmpty(timeInfo?.StartTimeField) && !string.IsNullOrEmpty(timeInfo?.EndTimeField);
            }

            return canUseInstantaneousTime;
        }

        /// <summary>
        /// Returns the layer's time-info, if applicable.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task<LayerTimeInfo> GetTimeInfoAsync(ILoadable layer) // Can't be of type Layer since ArcGISSublayer doesn't inherit from that
        {
            if (!(layer is ArcGISSublayer) && !(layer is FeatureLayer) && !(layer is RasterLayer))
            {
                return null;
            }

            try
            {
                await layer.LoadAsync();
                if (layer.LoadStatus != LoadStatus.Loaded)
                {
                    return null;
                }
            }
            catch
            {
                // Return null if there was an exception thrown while loading the layer
                return null;
            }

            switch (layer)
            {
                case ArcGISSublayer a:
                    return a.MapServiceSublayerInfo?.TimeInfo;
                case FeatureLayer fl:
                    return fl.FeatureTable is ServiceFeatureTable sft ? sft.LayerInfo?.TimeInfo : null;
                case RasterLayer rl:
                    return rl.Raster is ImageServiceRaster isr ? isr.ServiceInfo?.TimeInfo : null;
                default:
                    return null;
            }
        }

#endregion

#region Events

        /// <summary>
        /// Occurs when the selected time extent has changed.
        /// </summary>
        public event EventHandler<TimeExtentChangedEventArgs> CurrentExtentChanged;

#endregion

#region Playback Methods

        /// <summary>
        /// Moves the slider position forward by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the position could not be moved as requested.</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position.</param>
        /// <remarks>When the current time extent represents a time range and neither the start nor end time are pinned, then the number of
        /// time steps between the start and end time will always be preserved.  In that case, a value of false will be returned if the
        /// extent could not be moved by the specified number of time steps without going beyond the end of the time slider's full extent.
        /// If the current time extent is a time instant and either the start or end time are pinned, then the method call will attempt to
        /// move the unpinned end of the time extent.  In that case, the method will return false if the unpinned end could not be moved by
        /// the specified number of steps without going beyond the end of the full extent or the pinned end of the current extent.  In all
        /// cases, when the method returns false, the time slider's current extent will be unchanged.</remarks>
        public bool StepForward(int timeSteps = 1)
        {
            // TODO: design question - should users be allowed to specify whether to preserve the time window?
            if (timeSteps < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSteps), $"{nameof(timeSteps)} must be greater than zero");
            }

            var preserveSpan = !IsStartTimePinned && !IsEndTimePinned;
            return MoveTimeStep(timeSteps, preserveSpan);
        }

        /// <summary>
        /// Moves the slider position back by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the position could not be moved as requested.</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position.</param>
        /// <remarks>When the current time extent represents a time range and neither the start nor end time are pinned, then the number of
        /// time steps between the start and end time will always be preserved.  In that case, a value of false will be returned if the
        /// extent could not be moved by the specified number of time steps without going beyond the start of the time slider's full extent.
        /// If the current time extent is a time instant and either the start or end time are pinned, then the method call will attempt to
        /// move the unpinned end of the time extent.  In that case, the method will return false if the unpinned end could not be moved by
        /// the specified number of steps without going beyond the start of the full extent or the pinned end of the current extent.  In all
        /// cases, when the method returns false, the time slider's current extent will be unchanged.</remarks>
        public bool StepBack(int timeSteps = 1)
        {
            if (timeSteps < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSteps), $"{nameof(timeSteps)} must be greater than zero");
            }

            var preserveSpan = !IsStartTimePinned && !IsEndTimePinned;
            return MoveTimeStep(0 - timeSteps, preserveSpan);
        }

        /// <summary>
        /// Moves the current start and end times by the specified number of time steps.
        /// </summary>
        /// <param name="timeSteps">The number of time steps by which to move the current time.  A positive number will advance the time step forward, while
        /// a negative value will move the current time step backward.</param>
        /// <param name="preserveSpan">Whether to preserve the number of time steps between the current start and end time.</param>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the time step could not be moved as requested.</returns>
        private bool MoveTimeStep(int timeSteps, bool preserveSpan)
        {
            if (TimeSteps == null || CurrentValidExtent == null || (IsStartTimePinned && IsEndTimePinned))
            {
                return false;
            }

            // Always preserve the number of intervals between start and end when the current time is a time instant
            preserveSpan = IsCurrentExtentTimeInstant ? true : preserveSpan;

            // We want to rely on step indexes, so we use the known backing type here since that's most efficent.
            // If the backing type changes, or if the property is changed to be settable, the implemetation here
            // will need to be updated accordingly.
            var timeStepsList = TimeSteps.ToList();

            // Get the current start and end time step indexes
            var startTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.StartTime);
            var endTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.EndTime);

            // Get the minimum and maximum allowable time step indexes.  This is not necessarily the end of the time slider since
            // the start and end times may be pinned.
            var minTimeStepIndex = !IsStartTimePinned ? 0 : startTimeStepIndex;
            var maxTimeStepIndex = !IsEndTimePinned ? timeStepsList.Count - 1 : endTimeStepIndex;

            // Get the number of steps by which to move the current time.  If the number specified in the method call would move the current time extent
            // beyond the valid range, clamp the number of steps to the maximum number that the extent can move in the specified direction.
            var validTimeStepDelta = 0;
            if (timeSteps > 0)
            {
                validTimeStepDelta = startTimeStepIndex + timeSteps <= maxTimeStepIndex ? timeSteps : maxTimeStepIndex - startTimeStepIndex;
            }
            else
            {
                validTimeStepDelta = endTimeStepIndex + timeSteps >= minTimeStepIndex ? timeSteps : minTimeStepIndex - endTimeStepIndex;
            }

            // Get the new start and end time step indexes
            var newStartTimeStepIndex = !IsStartTimePinned && (validTimeStepDelta > 0 || startTimeStepIndex + validTimeStepDelta >= minTimeStepIndex) ?
                startTimeStepIndex + validTimeStepDelta : startTimeStepIndex;
            var newEndTimeStepIndex = !IsEndTimePinned && (validTimeStepDelta < 0 || endTimeStepIndex + validTimeStepDelta <= maxTimeStepIndex) ?
                endTimeStepIndex + validTimeStepDelta : endTimeStepIndex;

            // Adjust the new index in the event that it's coincident with the max or min and the current time extent is a time range (i.e. not a
            // time instant.  In that case, we need to preserve at least one time step between the start and end times.
            if (newStartTimeStepIndex == maxTimeStepIndex && !IsCurrentExtentTimeInstant)
            {
                newStartTimeStepIndex--;
            }

            if (newEndTimeStepIndex == minTimeStepIndex && !IsCurrentExtentTimeInstant)
            {
                newEndTimeStepIndex++;
            }

            // Evaluate how many time steps the start and end were moved by and whether they were able to be moved by the requested number of steps
            var startDelta = newStartTimeStepIndex - startTimeStepIndex;
            var endDelta = newEndTimeStepIndex - endTimeStepIndex;
            var canMoveStartAndEndByTimeSteps = startDelta == timeSteps && endDelta == timeSteps;
            var canMoveStartOrEndByTimeSteps = startDelta == timeSteps || endDelta == timeSteps;

            var isRequestedMoveValid = (preserveSpan && canMoveStartAndEndByTimeSteps) || (!preserveSpan && canMoveStartOrEndByTimeSteps);

            // Apply the new extent if the new time indexes represent a valid change
            if (isRequestedMoveValid)
            {
                var newStartTime = timeStepsList[newStartTimeStepIndex];
                var newEndTime = timeStepsList[newEndTimeStepIndex];

                // Update the current time extent
                if (newStartTimeStepIndex == newEndTimeStepIndex)
                {
                    CurrentExtent = new TimeExtent(newStartTime);
                }
                else
                {
                    CurrentExtent = new TimeExtent(newStartTime, newEndTime);
                }
            }

            // Return whether or not the time extent was moved by the specified number of steps
            return isRequestedMoveValid;
        }

        /// <summary>
        /// Moves the time slider's current extent upon expiration of the playback interval.
        /// </summary>
        private void PlayTimer_Tick(object sender, object e)
        {
            var isFinished = false;

            // Try moving the current extent forward or back by one time step
            var timeStepsToMove = PlaybackDirection == PlaybackDirection.Forward ? 1 : -1;
            var preserveSpan = !IsStartTimePinned && !IsEndTimePinned;
            isFinished = !MoveTimeStep(timeStepsToMove, preserveSpan);

            if (isFinished)
            {
                // The current extent could not be moved.  Check whether playback looping is enabled
                if (PlaybackLoopMode == LoopMode.None)
                {
                    IsPlaying = false;
                }
                else
                {
                    // Looping is enabled - calculate the number of time steps to move the current extent based on the loop mode.
                    var timeStepsList = TimeSteps.ToList();

                    // Get the current start and end time step indexes
                    var startTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.StartTime);
                    var endTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.EndTime);

                    if (PlaybackLoopMode == LoopMode.Repeat)
                    {
                        // Playback is set to repeat, so the current extent should be moved to the beginning or end, depending on the playback
                        // direction.  Note that this may be the beginning or end of the slider, or if the start or end time is pinned, it may
                        // only be back to the pinned end.
                        if (PlaybackDirection == PlaybackDirection.Forward)
                        {
                            timeStepsToMove = !IsStartTimePinned ? 0 - startTimeStepIndex : 0 - (endTimeStepIndex - startTimeStepIndex - 1);
                        }
                        else
                        {
                            timeStepsToMove = !IsEndTimePinned ? timeStepsList.Count - endTimeStepIndex - 1
                                : endTimeStepIndex - startTimeStepIndex - 1;
                        }
                    }
                    else if (PlaybackLoopMode == LoopMode.Reverse)
                    {
                        // Playback is set to reverse direction when the end is reached.  Simply flip the playback direction and move one time
                        // step in the new direction.
                        if (PlaybackDirection == PlaybackDirection.Forward)
                        {
                            PlaybackDirection = PlaybackDirection.Backward;
                            timeStepsToMove = -1;
                        }
                        else
                        {
                            PlaybackDirection = PlaybackDirection.Forward;
                            timeStepsToMove = 1;
                        }
                    }

                    MoveTimeStep(timeStepsToMove, preserveSpan: false);
                }
            }
        }

        private void OnNextButtonClick() => MoveTimeStep(1, preserveSpan: !IsStartTimePinned && !IsEndTimePinned);

        private void OnPreviousButtonClick() => MoveTimeStep(-1, preserveSpan: !IsStartTimePinned && !IsEndTimePinned);
#endregion
    }
}