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

#if !__IOS__ && !__ANDROID__

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI.Controls;
using Windows.Foundation;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Key = Windows.System.VirtualKey;
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
    [TemplatePart(Name = "HorizontalTrack", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "HorizontalTrackThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "MinimumThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "MinimumThumbLabel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "MaximumThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "MaximumThumbLabel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "Tickmarks", Type = typeof(Primitives.Tickbar))]
    [TemplatePart(Name = "SliderTrackStepBackRepeater", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "SliderTrackStepForwardRepeater", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PlayPauseButton", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "NextButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PreviousButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "FullExtentStartTimeLabel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "FullExtentEndTimeLabel", Type = typeof(TextBlock))]
    [TemplateVisualState(GroupName = "CommonStates", Name = "Normal")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "MouseOver")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "Disabled")]
    [TemplateVisualState(GroupName = "FocusStates", Name = "Focused")]
    [TemplateVisualState(GroupName = "FocusStates", Name = "Unfocused")]
    public class TimeSlider : Control
    {
#region Fields

        private FrameworkElement SliderTrack;
        private Thumb MinimumThumb;
        private TextBlock MinimumThumbLabel;
        private Thumb MaximumThumb;
        private TextBlock MaximumThumbLabel;
        private Thumb HorizontalTrackThumb;
        private RepeatButton SliderTrackStepBackRepeater;
        private RepeatButton SliderTrackStepForwardRepeater;
        private ToggleButton PlayPauseButton;
        private ButtonBase NextButton;
        private ButtonBase PreviousButton;
        private TextBlock FullExtentStartTimeLabel;
        private TextBlock FullExtentEndTimeLabel;
        private Primitives.Tickbar Tickmarks;
        private DispatcherTimer _playTimer;
        private TimeExtent _currentValue;
        private bool _isFocused;
        private bool _isMouseOver;
        private double _totalHorizontalChange;
        private TimeExtent _horizontalChangeExtent;
        private string _originalFullExtentLabelFormat;
        private string _originalCurrentExtentLabelFormat;
        private ThrottleAwaiter _calculateTimeStepsThrottler = new ThrottleAwaiter(1);
        private TaskCompletionSource<bool> _calculateTimeStepsTcs = new TaskCompletionSource<bool>();

#endregion // Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
        public TimeSlider()
        {
            DefaultStyleKey = typeof(TimeSlider);
            _playTimer = new DispatcherTimer() { Interval = PlaybackInterval };
            _playTimer.Tick += PlayTimer_Tick;
            SizeChanged += TimeSlider_SizeChanged;
        }

        private void TimeSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTrackLayout(CurrentValidExtent);
        }

        #region Overrides

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application
        /// code or internal processes (such as a rebuilding layout pass) call
        /// <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>.
        /// </summary>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            SliderTrack = GetTemplateChild(nameof(SliderTrack)) as FrameworkElement;
            if (SliderTrack != null)
                SliderTrack.SizeChanged += TimeSlider_SizeChanged;
            HorizontalTrackThumb = GetTemplateChild(nameof(HorizontalTrackThumb)) as Thumb;
            MinimumThumb = GetTemplateChild(nameof(MinimumThumb)) as Thumb;
            MinimumThumbLabel = GetTemplateChild(nameof(MinimumThumbLabel)) as TextBlock;
            MaximumThumb = GetTemplateChild(nameof(MaximumThumb)) as Thumb;
            MaximumThumbLabel = GetTemplateChild(nameof(MaximumThumbLabel)) as TextBlock;
            SliderTrackStepBackRepeater = GetTemplateChild(nameof(SliderTrackStepBackRepeater)) as RepeatButton;
            SliderTrackStepForwardRepeater = GetTemplateChild(nameof(SliderTrackStepForwardRepeater)) as RepeatButton;
            PlayPauseButton = GetTemplateChild(nameof(PlayPauseButton)) as ToggleButton;
            NextButton = GetTemplateChild(nameof(NextButton)) as ButtonBase;
            PreviousButton = GetTemplateChild(nameof(PreviousButton)) as ButtonBase;
            Tickmarks = GetTemplateChild(nameof(Tickmarks)) as Primitives.Tickbar;
            FullExtentStartTimeLabel = GetTemplateChild(nameof(FullExtentStartTimeLabel)) as TextBlock;
            FullExtentEndTimeLabel = GetTemplateChild(nameof(FullExtentEndTimeLabel)) as TextBlock;

            if (MinimumThumb != null)
            {
#if NETFX_CORE
                MinimumThumb.ManipulationMode = ManipulationModes.TranslateX;
                MinimumThumb.ManipulationDelta += (s, e) =>
                {
                    // Position is reported relative to the left edge of the thumb.  Adjust it so it is relative to the thumb's center.
                    var translateX = e.Position.X - (MinimumThumb.ActualWidth / 2);
                    OnMinimumThumbDrag(translateX);
                };
#else
                MinimumThumb.DragDelta += (s, e) => OnMinimumThumbDrag(e.HorizontalChange);
#endif
                MinimumThumb.DragCompleted += DragCompleted;
                MinimumThumb.DragStarted += (s, e) => SetFocus();
            }
            if (MaximumThumb != null)
            {
#if NETFX_CORE
                MaximumThumb.ManipulationMode = ManipulationModes.TranslateX;
                MaximumThumb.ManipulationDelta += (s, e) =>
                {
                    // Position is reported relative to the left edge of the thumb.  Adjust it so it is relative to the thumb's center.
                    var translateX = e.Position.X - (MaximumThumb.ActualWidth / 2);
                    System.Diagnostics.Debug.WriteLine($"X position: {e.Position.X}");
                    OnMaximumThumbDrag(translateX);
                };
#else
                MaximumThumb.DragDelta += (s, e) => OnMaximumThumbDrag(e.HorizontalChange);
#endif
                MaximumThumb.DragCompleted += DragCompleted;
                MaximumThumb.DragStarted += (s, e) => SetFocus();
            }
            if (HorizontalTrackThumb != null)
            {
                HorizontalTrackThumb.DragDelta += HorizontalTrackThumb_DragDelta;
                HorizontalTrackThumb.DragCompleted += DragCompleted;
                HorizontalTrackThumb.DragStarted += (s, e) => SetFocus();
            }
            if (SliderTrackStepBackRepeater != null)
            {
                SliderTrackStepBackRepeater.Click += (s, e) =>
                {
                    SetFocus();
                    IsPlaying = false;
                    StepBack();
                };
            }
            if (SliderTrackStepForwardRepeater != null)
            {
                SliderTrackStepForwardRepeater.Click += (s, e) =>
                {
                    SetFocus();
                    IsPlaying = false;
                    StepForward();
                };
            }
            if (PlayPauseButton != null)
            {
                IsPlaying = PlayPauseButton.IsChecked.Value;
                PlayPauseButton.Checked += (s, e) => IsPlaying = true;
                PlayPauseButton.Unchecked += (s, e) => IsPlaying = false;
            }
            if (NextButton != null)
            {
                NextButton.Click += (s, e) => MoveTimeStep(1, preserveSpan: !IsStartTimePinned && !IsEndTimePinned);
            }
            if (PreviousButton != null)
            {
                PreviousButton.Click += (s, e) => MoveTimeStep(-1, preserveSpan: !IsStartTimePinned && !IsEndTimePinned);
            }
            PositionTickmarks();
            SetButtonVisibility();
            ApplyLabelMode(LabelMode);
        }

        private void SetFocus()
        {
#if NETFX_CORE
            Focus(FocusState.Pointer);
#else
            Focus();
#endif
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.LostFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            ChangeVisualState(true);
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.GotFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = true;
            ChangeVisualState(true);
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
#else
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
#endif
            _isMouseOver = true;
            if (MinimumThumb != null && !MinimumThumb.IsDragging ||
                MaximumThumb != null && !MaximumThumb.IsDragging ||
                MinimumThumb == null && MaximumThumb == null)
            {
                ChangeVisualState(true);
            }
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
#else
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
#endif
            _isMouseOver = false;
            if (MinimumThumb != null && !MinimumThumb.IsDragging ||
                MaximumThumb != null && !MaximumThumb.IsDragging ||
                MinimumThumb == null && MaximumThumb == null)
            {
                ChangeVisualState(true);
            }
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
#else
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
#endif
            if (!e.Handled && IsEnabled)
            {
                SetFocus();
            }
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.KeyDown"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
#if NETFX_CORE
        protected override void OnKeyDown(KeyRoutedEventArgs e)
#else
        protected override void OnKeyDown(KeyEventArgs e)
#endif
        {
            base.OnKeyDown(e);
            if (!e.Handled && IsEnabled)
            {
                if ((e.Key == Key.Left))
                {
                    StepBack();
                }
                else if ((e.Key == Key.Right))
                {
                    StepForward();
                }
            }
        }

#endregion // Overrides

        private void ChangeVisualState(bool useTransitions)
        {
            //CommonStates
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, "Disabled", useTransitions);
            }
            else if (_isMouseOver)
            {
                VisualStateManager.GoToState(this, "MouseOver", useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal", useTransitions);
            }
            //FocusStates
            if (_isFocused && IsEnabled)
            {
                VisualStateManager.GoToState(this, "Focused", useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, "Unfocused", useTransitions);
            }
        }

        /// <summary>
        /// Updates slider track UI components to display the specified time extent
        /// </summary>
        /// <param name="extent">The time extent to display on the slider track</param>
        private void UpdateTrackLayout(TimeExtent extent)
        {
            if (extent == null || extent.StartTime < ValidFullExtent.StartTime || extent.EndTime > ValidFullExtent.EndTime ||
            MinimumThumb == null || MaximumThumb == null || ValidFullExtent.EndTime <= ValidFullExtent.StartTime || SliderTrack == null ||
            TimeSteps == null || !TimeSteps.GetEnumerator().MoveNext())
                return;

            var sliderWidth = SliderTrack.ActualWidth;
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
            var rate = SliderTrack.ActualWidth / (maximum - minimum);


            // Position left repeater							
            right = Math.Min(sliderWidth, ((maximum - start) * rate) + MaximumThumb.ActualWidth);
            SliderTrackStepBackRepeater.Margin = new Thickness(0, 0, right, 0);
            SliderTrackStepBackRepeater.Width = Math.Max(0, sliderWidth - right);

            // Margin adjustment for the minimum thumb label
            var thumbLabelWidthAdjustment = 0d;
            var minLabelLeftMargin = -1d;
            var minLabelRightMargin = -1d;
            var minThumbLabelWidth = 0d;

            // Position minimum thumb
            if (!IsCurrentExtentTimeInstant) // Check for two thumbs
            {
                // There are two thumbs, so position minimum (max is used in both the one and two thumb case)
                left = Math.Max(0, (start - minimum) * rate);
                right = Math.Min(sliderWidth, ((maximum - start) * rate));
#if NETFX_CORE
                // Accommodate issue on UWP where element sometimes actually renders with a width of one pixel less than margin values dictate
                left -= 0.5;
                right -= 0.5;
#endif
                thumbLeft = left - MinimumThumb.ActualWidth / 2;
                thumbRight = right - MinimumThumb.ActualWidth / 2;
                MinimumThumb.Margin = new Thickness(thumbLeft, 0, thumbRight, 0);

                // TODO: Change visibility instead of opacity.  Doing so throws an exception that start time cannot be
                // greater than end time when dragging minimum thumb.                    
                MinimumThumbLabel.Opacity = (LabelMode == TimeSliderLabelMode.CurrentExtent) && start == minimum ? 0 : 1;

                // Calculate thumb label position
                minThumbLabelWidth = CalculateTextSize(MinimumThumbLabel).Width;
                thumbLabelWidthAdjustment = minThumbLabelWidth / 2;
                minLabelLeftMargin = left - thumbLabelWidthAdjustment;
                minLabelRightMargin = Math.Min(sliderWidth, right - thumbLabelWidthAdjustment);
            }
            else
            {
                // There's only one thumb, so hide the min thumb
                MinimumThumb.Margin = new Thickness(0, 0, sliderWidth, 0);
                MinimumThumbLabel.Opacity = 0;
            }

            // Position middle thumb (filled area between min and max thumbs is actually a thumb and can be dragged)
            if (IsCurrentExtentTimeInstant) // One thumb
            {
                // Hide the middle thumb
                HorizontalTrackThumb.Margin = new Thickness(0, 0, sliderWidth, 0);
                HorizontalTrackThumb.Width = 0;
            }
            else // !IsCurrentExtentTimeInstant
            {
                // Position the middle thumb
                left = Math.Min(sliderWidth, ((start - minimum) * rate));
                right = Math.Min(sliderWidth, (maximum - end) * rate);
                HorizontalTrackThumb.Margin = new Thickness(left, 0, right, 0);
                HorizontalTrackThumb.Width = Math.Max(0, (sliderWidth - right - left));
                HorizontalTrackThumb.HorizontalAlignment = HorizontalAlignment.Left;
            }

            // Position maximum thumb
            left = Math.Min(sliderWidth, (end - minimum) * rate);
            right = Math.Min(sliderWidth, ((maximum - end) * rate));
#if NETFX_CORE
            // Accommodate issue on UWP where element sometimes actually renders with a width of one pixel less than margin values dictate
            left -= 0.5;
            right -= 0.5;
#endif
            thumbLeft = left - MaximumThumb.ActualWidth / 2;
            thumbRight = right - MaximumThumb.ActualWidth / 2;
            MaximumThumb.Margin = new Thickness(thumbLeft, 0, thumbRight, 0);

            // Update maximum thumb label visibility
            MaximumThumbLabel.Visibility = LabelMode != TimeSliderLabelMode.CurrentExtent || end == maximum || (IsCurrentExtentTimeInstant && start == minimum)
                ? Visibility.Collapsed : Visibility.Visible;

            // Position maximum thumb label
            var maxThumbLabelWidth = CalculateTextSize(MaximumThumbLabel).Width;
            thumbLabelWidthAdjustment = maxThumbLabelWidth / 2;
            var maxLabelLeftMargin = left - thumbLabelWidthAdjustment;
            var maxLabelRightMargin = Math.Min(sliderWidth, right - thumbLabelWidthAdjustment);

            // Handle possible thumb label collision and apply label positions
            if (!IsCurrentExtentTimeInstant && MinimumThumbLabel.Opacity == 1)
            {
                if (MaximumThumbLabel.Visibility == Visibility.Visible)
                {
                    // Slider has min and max thumbs with both labels visible - check for label collision
                    var minLabelRight = minLabelLeftMargin + minThumbLabelWidth;
                    var spaceBetweenLabels = 6;

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
                MinimumThumbLabel.Margin = new Thickness(minLabelLeftMargin, 0, minLabelRightMargin, 0);
            }

            MaximumThumbLabel.Margin = new Thickness(maxLabelLeftMargin, 0, maxLabelRightMargin, 0);

            // Position right repeater
            left = Math.Min(sliderWidth, ((end - minimum) * rate) + MaximumThumb.ActualWidth);
            SliderTrackStepForwardRepeater.Margin = new Thickness(left, 0, 0, 0);
            SliderTrackStepForwardRepeater.Width = Math.Max(0, sliderWidth - left);
        }

        /// <summary>
        /// Calculates the size of the specified TextBlock's text
        /// </summary>
        /// <param name="textBlock">The TextBlock to calculate size for</param>
        /// <returns>The size of the text</returns>
        /// <remarks>This method is useful in cases where a TextBlock's text has updated, but its layout has not.  In such cases,
        /// the ActualWidth and ActualHeight properties are not representative of the new text.</remarks>
        private Size CalculateTextSize(TextBlock textBlock)
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
                Text = textBlock.Text
            };
            tb.Measure(new Size(0, 0));
            tb.Arrange(new Rect(0, 0, 0, 0));
            return new Size(tb.ActualWidth, tb.ActualHeight);
#else
            var typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
            var formattedText = new FormattedText(textBlock.Text, CultureInfo.CurrentCulture, textBlock.FlowDirection, typeface,
                textBlock.FontSize, textBlock.Foreground, new NumberSubstitution(), TextFormattingMode.Display);
            return new Size(formattedText.Width, formattedText.Height);
#endif
        }

#region Drag event handlers

        private void HorizontalTrackThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            IsPlaying = false;

            if (e.HorizontalChange == 0 || IsStartTimePinned || IsEndTimePinned)
                return;

            if (_currentValue == null)
                _currentValue = CurrentValidExtent;

            _totalHorizontalChange = e.HorizontalChange;

            _horizontalChangeExtent = new TimeExtent(_currentValue.StartTime, _currentValue.EndTime);

            // time ratio 
            long TimeRate = (ValidFullExtent.EndTime.Ticks - ValidFullExtent.StartTime.Ticks) / (long)SliderTrack.ActualWidth;

            // time change
            long TimeChange = (long)(TimeRate * _totalHorizontalChange);

            TimeSpan difference = new TimeSpan(_currentValue.EndTime.DateTime.Ticks - _currentValue.StartTime.DateTime.Ticks);

            TimeExtent tempChange = null;
            try
            {
                tempChange = new TimeExtent(_horizontalChangeExtent.StartTime.DateTime.AddTicks(TimeChange),
                    _horizontalChangeExtent.EndTime.DateTime.AddTicks(TimeChange));
            }
            catch (ArgumentOutOfRangeException)
            {
                if (_totalHorizontalChange < 0)
                    tempChange = new TimeExtent(ValidFullExtent.StartTime, ValidFullExtent.StartTime.Add(difference));
                else if (_totalHorizontalChange > 0)
                    tempChange = new TimeExtent(ValidFullExtent.EndTime.Subtract(difference), ValidFullExtent.EndTime);
            }

            if (tempChange.StartTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks)
                _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime, ValidFullExtent.StartTime.Add(difference)));
            else if (tempChange.EndTime.DateTime.Ticks > ValidFullExtent.EndTime.Ticks)
                _currentValue = Snap(new TimeExtent(ValidFullExtent.EndTime.Subtract(difference), ValidFullExtent.EndTime));
            else
                _currentValue = Snap(new TimeExtent(tempChange.StartTime, tempChange.EndTime));

            UpdateTrackLayout(_currentValue);
            if (_currentValue.StartTime != CurrentExtent.StartTime || _currentValue.EndTime != CurrentExtent.EndTime)
                UpdateCurrentExtent();
        }

        private void OnMinimumThumbDrag(double translateX)
        {
            IsPlaying = false;
            if (translateX == 0)
                return;

            if (_currentValue == null)
                _currentValue = CurrentValidExtent;

            _totalHorizontalChange = translateX;
            _horizontalChangeExtent = new TimeExtent(_currentValue.StartTime, _currentValue.EndTime);
            // time ratio 
            long TimeRate = (ValidFullExtent.EndTime.Ticks - ValidFullExtent.StartTime.Ticks) / (long)SliderTrack.ActualWidth;

            // time change
            long TimeChange = (long)(TimeRate * _totalHorizontalChange);

            TimeExtent tempChange = null;
            try
            {
                tempChange = new TimeExtent(_horizontalChangeExtent.StartTime.DateTime.AddTicks(TimeChange), _horizontalChangeExtent.EndTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                if (_totalHorizontalChange < 0)
                    tempChange = new TimeExtent(ValidFullExtent.StartTime, _currentValue.EndTime);
                else if (_totalHorizontalChange > 0)
                    tempChange = new TimeExtent(_currentValue.EndTime);
            }

            if (tempChange.StartTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks)
                _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime, _currentValue.EndTime));
            else if (tempChange.StartTime >= _currentValue.EndTime)
                _currentValue = Snap(new TimeExtent(_currentValue.EndTime));
            else
                _currentValue = Snap(new TimeExtent(tempChange.StartTime, tempChange.EndTime));

            UpdateTrackLayout(_currentValue);
            if (_currentValue.StartTime != CurrentExtent.StartTime)
                UpdateCurrentExtent();
        }

        private void OnMaximumThumbDrag(double translateX)
        {

            IsPlaying = false;
            if (translateX == 0)
                return;

            if (_currentValue == null)
                _currentValue = CurrentValidExtent;

            _totalHorizontalChange = translateX;
            _horizontalChangeExtent = new TimeExtent(_currentValue.StartTime, _currentValue.EndTime);

            // time ratio 
            long TimeRate = (ValidFullExtent.EndTime.Ticks - ValidFullExtent.StartTime.Ticks) / (long)SliderTrack.ActualWidth;

            // time change
            long TimeChange = (long)(TimeRate * _totalHorizontalChange);

            TimeExtent tempChange = null;
            if (IsCurrentExtentTimeInstant)
            {
                try
                {
                    // If the mouse drag creates a date thats year is before 
                    // 1/1/0001 or after 12/31/9999 then an out of renge 
                    // exception will be trown.
                    tempChange = new TimeExtent(_horizontalChangeExtent.EndTime.DateTime.AddTicks(TimeChange));
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (_totalHorizontalChange > 0) // date is after 12/31/9999
                        tempChange = new TimeExtent(ValidFullExtent.EndTime);
                    else if (_totalHorizontalChange < 0) // date is before 1/1/0001
                        tempChange = new TimeExtent(ValidFullExtent.StartTime);
                }
            }
            else
            {
                try
                {
                    // If the mouse drag creates a date thats year is before 
                    // 1/1/0001 or after 12/31/9999 then an out of range 
                    // exception will be trown.
                    tempChange = new TimeExtent(_horizontalChangeExtent.StartTime, _horizontalChangeExtent.EndTime.DateTime.AddTicks(TimeChange));
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (_totalHorizontalChange > 0) // date is after 12/31/9999
                        tempChange = new TimeExtent(_currentValue.StartTime, ValidFullExtent.EndTime);
                    else if (_totalHorizontalChange < 0) // date is before 1/1/0001
                        tempChange = new TimeExtent(_currentValue.StartTime);
                }
            }

            var oldValue = _currentValue;
            // validate change
            if (IsCurrentExtentTimeInstant)
            {
                if (tempChange.EndTime.DateTime.Ticks > ValidFullExtent.EndTime.Ticks)
                    _currentValue = Snap(new TimeExtent(ValidFullExtent.EndTime));
                else if (tempChange.EndTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks)
                    _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime));
                else
                    _currentValue = Snap(new TimeExtent(tempChange.EndTime));
            }
            else
            {
                if (tempChange.EndTime.DateTime.Ticks > ValidFullExtent.EndTime.Ticks)
                    _currentValue = Snap(new TimeExtent(_currentValue.StartTime, ValidFullExtent.EndTime));
                else if (tempChange.EndTime <= _currentValue.StartTime && !IsCurrentExtentTimeInstant) // TODO: Preserve one time step between min and max thumbs
                    _currentValue = Snap(new TimeExtent(_currentValue.StartTime, _currentValue.StartTime.DateTime.AddMilliseconds(1)));
                else if (tempChange.EndTime.DateTime.Ticks < ValidFullExtent.StartTime.Ticks) // TODO: Preserve one time step between min and max thumbs
                    _currentValue = Snap(new TimeExtent(ValidFullExtent.StartTime));
                else
                    _currentValue = Snap(new TimeExtent(_currentValue.StartTime, tempChange.EndTime));
            }

            UpdateTrackLayout(_currentValue);
            if (_currentValue.EndTime != CurrentExtent.EndTime)
            {
                UpdateCurrentExtent();
            }
        }

        private void DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (_currentValue == null)
                return;
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
        /// Adjusts the specified time extent so that it starts and ends at a valid time step interval
        /// </summary>
        /// <param name="extent">The time extent to adjust</param>
        /// <returns>The snapped time extent</returns>
        private TimeExtent Snap(TimeExtent extent)
        {
            if (extent == null)
                return null;

            if (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext())
            {
                if (TimeSteps.Contains(extent.StartTime) && TimeSteps.Contains(extent.EndTime))
                    return extent; // Extent already starts and ends at valid time steps

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
        /// Positions the tickmarks along the slider's tick bar
        /// </summary>
        private void PositionTickmarks()
        {
            if (Tickmarks == null || ValidFullExtent.StartTime >= ValidFullExtent.EndTime)
                return;

            Tickmarks.TickmarkPositions = null;
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
                Tickmarks.TickmarkPositions = intervals;
                Tickmarks.TickmarkDataSources = tickMarkDates.Cast<object>();
                Tickmarks.ShowTickLabels = LabelMode == TimeSliderLabelMode.TimeStepInterval;
            }
        }

#region Properties

        /// <summary>
        /// Gets whether or not the current extent represents a time instant
        /// </summary>
        private bool IsCurrentExtentTimeInstant => CurrentValidExtent?.IsTimeInstant() ?? false;

        /// <summary>
        /// Gets the current FullExtent or, if unavailable, a valid substitute
        /// </summary>
        private TimeExtent ValidFullExtent => FullExtent ?? new TimeExtent(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

        /// <summary>
        /// Gets the CurrentExtent or, if invalid, a valid substitute
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
#if !NETFX_CORE
        [TypeConverter(typeof(TimeExtentConverter))]
#endif
        public TimeExtent CurrentExtent
        {
            get { return (TimeExtent)GetValue(CurrentExtentProperty); }
            set { SetValue(CurrentExtentProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentExtentProperty =
            DependencyProperty.Register(nameof(CurrentExtent), typeof(TimeExtent), typeof(TimeSlider),
                new PropertyMetadata(default(TimeExtent), OnCurrentExtentPropertyChanged));

        private static void OnCurrentExtentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            var newExtent = e.NewValue as TimeExtent;

            slider._currentValue = newExtent;

            // Explicitly update the thumb labels' bindings to ensure that their text is updated prior to calculating layout
            slider.MinimumThumbLabel?.RefreshBinding(TextBlock.TextProperty);
            slider.MaximumThumbLabel?.RefreshBinding(TextBlock.TextProperty);

            slider.UpdateTrackLayout(slider.CurrentValidExtent);

            // If the new extent represents a time instant, enforce the pinning of start and end time being in sync
            if (newExtent.IsTimeInstant() && slider.IsStartTimePinned != slider.IsEndTimePinned)
            {
                slider.IsStartTimePinned = false;
                slider.IsEndTimePinned = false;
            }

            slider.CurrentExtentChanged?.Invoke(slider, new CurrentExtentChangedEventArgs(newExtent, e.OldValue as TimeExtent));
        }

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> that specifies the overall start and end time of the time slider instance
        /// </summary>
#if !NETFX_CORE
		[TypeConverter(typeof(TimeExtentConverter))]
#endif
        public TimeExtent FullExtent
        {
            get { return (TimeExtent)GetValue(FullExtentProperty); }
            set { SetValue(FullExtentProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FullExtentProperty =
            DependencyProperty.Register(nameof(FullExtent), typeof(TimeExtent), typeof(TimeSlider),
                new PropertyMetadata(default(TimeExtent), OnFullExtentPropertyChanged));

        private async static void OnFullExtentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // TODO: For consideration - should FullExtent be snapped to a multiple of the specified time step?  E.g. if the time step interval
            // is 1 week and the full extent is 3 months (suppose 92 days in this case), should the full extent be snapped to 91 or 98 days?

            var slider = (TimeSlider)d;
            await slider.CalculateTimeStepsAsync();
            slider.PositionTickmarks();
            slider.UpdateTrackLayout(slider.CurrentValidExtent);
        }

        /// <summary>
        /// Gets or sets the time step intervals for the time slider.  The slider thumbs will snap to and tick marks will be shown at this interval.
        /// </summary>
        public TimeValue TimeStepInterval
        {
            get { return (TimeValue)GetValue(TimeStepIntervalProperty); }
            set { SetValue(TimeStepIntervalProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepInterval"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeStepIntervalProperty =
            DependencyProperty.Register(nameof(TimeStepInterval), typeof(TimeValue), typeof(TimeSlider),
                new PropertyMetadata(default(TimeValue), OnTimeStepIntervalPropertyChanged));

        private static async void OnTimeStepIntervalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                await ((TimeSlider)d).CalculateTimeStepsAsync();
            }
            catch
            {
                // This would be unexpected, but this is here just in case we encounter an exception.  This is in an async void method, so
                // there's no way to bubble this exception to user code.
            }
        }

        /// <summary>
        /// Updates time steps based on the current TimeStepInterval and FullExtent.  Executes asynchronously to ensure that pending updates to
        /// both TimeStepInterval and FullExtent are taken into account before intervals and subsequent layout updates are calculated.
        /// </summary>
        private async Task CalculateTimeStepsAsync()
        {
            // Time step calculation is throttled because FullExtent and TimeStepInterval are often changed in concert, and immediately responding to
            // changes in one can result in undesirable behavior.  For instance, suppose TimeStepInterval is 1 day and FullExtent is 14 days, yielding
            // 14 time steps.  Then suppose FullExtent is changed to 10 years, and TimeStepInterval to 1 year.  If the FullExtent change is responded to
            // without accounting for the change in TimeStepInterval, the slider will try to update to accommodate ~3650 time step intervals before updating
            // again to reduce this number to 10.  In this case, the unwieldy number of intervals will make the slider seem to become unresponsive for a
            // time.  But in any cases where both TimeStepInterval and FullExtent are updated together, there will be an inefficient double-execution of
            // layout logic.
            await _calculateTimeStepsThrottler.ThrottleDelay();

            if (TimeStepInterval == null || FullExtent == null)
                return;

            var timeStep = TimeStepInterval;
            var startTime = FullExtent.StartTime;
            var endTime = FullExtent.EndTime;

            var steps = new List<DateTimeOffset> { startTime };

            for (var nextStep = startTime.AddTimeValue(timeStep); nextStep <= endTime; nextStep = nextStep.AddTimeValue(timeStep))
                steps.Add(nextStep);

            TimeSteps = steps;
            _calculateTimeStepsTcs.TrySetResult(true);
        }

        /// <summary>
        /// Gets the time steps that can be used to set the slider instance's current extent
        /// </summary>
        public IEnumerable<DateTimeOffset> TimeSteps
        {
            get { return (IEnumerable<DateTimeOffset>)GetValue(TimeStepsProperty); }
            private set { SetValue(TimeStepsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeSteps"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeStepsProperty =
            DependencyProperty.Register(nameof(TimeSteps), typeof(IEnumerable<DateTimeOffset>), typeof(TimeSlider),
                new PropertyMetadata(default(IEnumerable<DateTimeOffset>), OnTimeStepsPropertyChanged));

        private static void OnTimeStepsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;

            // Update the slider UI to reflect the new time steps
            slider.PositionTickmarks();
            slider.SetButtonVisibility();
            slider.UpdateTrackLayout(slider.CurrentValidExtent);
        }

        /// <summary>
        /// Gets or sets the interval at which the time slider's current extent will move to the next or previous time step
        /// </summary>
		public TimeSpan PlaybackInterval
        {
            get { return (TimeSpan)GetValue(PlaybackIntervalProperty); }
            set { SetValue(PlaybackIntervalProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackInterval"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaybackIntervalProperty =
            DependencyProperty.Register(nameof(PlaybackInterval), typeof(TimeSpan), typeof(TimeSlider),
                new PropertyMetadata(TimeSpan.FromSeconds(1), OnPlaybackIntervalPropertyChanged));

        private static void OnPlaybackIntervalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            slider._playTimer.Interval = (TimeSpan)e.NewValue;
        }

        /// <summary>
        /// Gets or sets whether the current extent will move to the next or the previous time step during playback
        /// </summary>
		public PlaybackDirection PlaybackDirection
        {
            get { return (PlaybackDirection)GetValue(PlaybackDirectionProperty); }
            set { SetValue(PlaybackDirectionProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackDirection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaybackDirectionProperty =
            DependencyProperty.Register(nameof(PlaybackDirection), typeof(PlaybackDirection), typeof(TimeSlider),
                new PropertyMetadata(PlaybackDirection.Forward));

        /// <summary>
        /// Gets or sets the behavior when the current extent reaches the end of the slider during playback
        /// </summary>
		public LoopMode PlaybackLoopMode
        {
            get { return (LoopMode)GetValue(LoopModeProperty); }
            set { SetValue(LoopModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackLoopMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LoopModeProperty =
            DependencyProperty.Register(nameof(PlaybackLoopMode), typeof(LoopMode), typeof(TimeSlider), new PropertyMetadata(LoopMode.None));

        private void SetButtonVisibility()
        {
            var viz = (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext()) ? Visibility.Visible : Visibility.Collapsed;

            // Play Button
            if (PlayPauseButton != null)
                PlayPauseButton.Visibility = viz;

            // Next Button
            if (NextButton != null)
                NextButton.Visibility = viz;

            // Previous Button
            if (PreviousButton != null)
                PreviousButton.Visibility = viz;
        }

        /// <summary>
        /// Gets or sets whether the start time of the <see cref="CurrentExtent"/> is locked into place
        /// </summary>
		public bool IsStartTimePinned
        {
            get { return (bool)GetValue(IsStartTimePinnedProperty); }
            set { SetValue(IsStartTimePinnedProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStartTimePinned"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStartTimePinnedProperty =
            DependencyProperty.Register(nameof(IsStartTimePinned), typeof(bool), typeof(TimeSlider),
                new PropertyMetadata(default(bool), OnIsStartTimePinnedChanged));

        private static void OnIsStartTimePinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;

            // Enable or disable the start time thumb 
            var isStartTimePinned = (bool)e.NewValue;
            slider.MinimumThumb.IsEnabled = !isStartTimePinned;

            // If the slider extent is a time instant, keep whether start time and end time are pinned in sync
            if (slider.IsCurrentExtentTimeInstant && slider.IsEndTimePinned != isStartTimePinned)
                slider.IsEndTimePinned = isStartTimePinned;

            // Enable or disable the middle thumb based on whether both the start and end thumbs are pinned
            slider.HorizontalTrackThumb.IsEnabled = slider.MaximumThumb.IsEnabled && slider.MinimumThumb.IsEnabled;
        }

        /// <summary>
        /// Gets or sets whether the end time of the <see cref="CurrentExtent"/> is locked into place
        /// </summary>
        public bool IsEndTimePinned
        {
            get { return (bool)GetValue(IsEndTimePinnedProperty); }
            set { SetValue(IsEndTimePinnedProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsEndTimePinned"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsEndTimePinnedProperty =
            DependencyProperty.Register(nameof(IsEndTimePinned), typeof(bool), typeof(TimeSlider),
                new PropertyMetadata(default(bool), OnIsEndTimePinnedChanged));

        private static void OnIsEndTimePinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;

            // Enable or disable the end time thumb 
            var isEndTimePinned = (bool)e.NewValue;
            slider.MaximumThumb.IsEnabled = !isEndTimePinned;

            // If the slider extent is a time instant, keep whether start time and end time are pinned in sync
            if (slider.IsCurrentExtentTimeInstant && slider.IsStartTimePinned != isEndTimePinned)
                slider.IsStartTimePinned = isEndTimePinned;

            // Enable or disable the middle thumb based on whether both the start and end thumbs are pinned
            slider.HorizontalTrackThumb.IsEnabled = slider.MaximumThumb.IsEnabled && slider.MinimumThumb.IsEnabled;
        }

        /// <summary>
        /// Identifies the <see cref="IsPlaying"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(TimeSlider), new PropertyMetadata(false, OnIsPlayingPropertyChanged));

        private static void OnIsPlayingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            var isPlaying = (bool)e.NewValue;


            // Start or stop playback
            if (isPlaying && slider.TimeSteps != null && slider.TimeSteps.GetEnumerator().MoveNext())
            {
                if ((slider.IsStartTimePinned && slider.IsEndTimePinned) ||
                (slider.IsCurrentExtentTimeInstant && slider.IsEndTimePinned))
                {
                    // Can't start playback because current time extent is pinned
                    slider.IsPlaying = false;
                    return;
                }
                else
                {
                    slider._playTimer.Start();
                }
            }
            else
            {
                slider._playTimer.Stop();
            }

            // Update the state of the play/pause button
            if (slider.PlayPauseButton != null)
                slider.PlayPauseButton.IsChecked = isPlaying;
        }

#region Appearance Properties

        /// <summary>
        /// Gets or sets the border color of the thumbs
        /// </summary>
		public Brush ThumbStroke
        {
            get { return (Brush)GetValue(ThumbStrokeProperty); }
            set { SetValue(ThumbStrokeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ThumbStroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbStrokeProperty =
            DependencyProperty.Register(nameof(ThumbStroke), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the fill color of the thumbs
        /// </summary>
		public Brush ThumbFill
        {
            get { return (Brush)GetValue(ThumbFillProperty); }
            set { SetValue(ThumbFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ThumbFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbFillProperty =
            DependencyProperty.Register(nameof(ThumbFill), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="CurrentExtent"/>
        /// </summary>
		public Brush CurrentExtentFill
        {
            get { return (Brush)GetValue(CurrentExtentFillProperty); }
            set { SetValue(CurrentExtentFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtentFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentExtentFillProperty =
            DependencyProperty.Register(nameof(CurrentExtentFill), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="FullExtent"/>
        /// </summary>
		public Brush FullExtentFill
        {
            get { return (Brush)GetValue(FullExtentFillProperty); }
            set { SetValue(FullExtentFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtentFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FullExtentFillProperty =
            DependencyProperty.Register(nameof(FullExtentFill), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the border color of the area on the slider track that indicates the <see cref="FullExtent"/>
        /// </summary>
		public Brush FullExtentStroke
        {
            get { return (Brush)GetValue(FullExtentStrokeProperty); }
            set { SetValue(FullExtentStrokeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtentStroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FullExtentStrokeProperty =
            DependencyProperty.Register(nameof(FullExtentStroke), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the color of the slider's tickmarks
        /// </summary>
		public Brush TimeStepIntervalTickFill
        {
            get { return (Brush)GetValue(TimeStepIntervalTickFillProperty); }
            set { SetValue(TimeStepIntervalTickFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepIntervalTickFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeStepIntervalTickFillProperty =
            DependencyProperty.Register(nameof(TimeStepIntervalTickFill), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the fill color of the playback buttons
        /// </summary>
		public Brush PlaybackButtonsFill
        {
            get { return (Brush)GetValue(PlaybackButtonsFillProperty); }
            set { SetValue(PlaybackButtonsFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackButtonsFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaybackButtonsFillProperty =
            DependencyProperty.Register(nameof(PlaybackButtonsFill), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the border color of the playback buttons
        /// </summary>
		public Brush PlaybackButtonsStroke
        {
            get { return (Brush)GetValue(PlaybackButtonsStrokeProperty); }
            set { SetValue(PlaybackButtonsStrokeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackButtonsStroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaybackButtonsStrokeProperty =
            DependencyProperty.Register(nameof(PlaybackButtonsStroke), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the color of the full extent labels
        /// </summary>
		public Brush FullExtentLabelColor
        {
            get { return (Brush)GetValue(FullExtentLabelColorProperty); }
            set { SetValue(FullExtentLabelColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtentLabelColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FullExtentLabelColorProperty =
            DependencyProperty.Register(nameof(FullExtentLabelColor), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the color of the current extent labels
        /// </summary>
		public Brush CurrentExtentLabelColor
        {
            get { return (Brush)GetValue(CurrentExtentLabelColorProperty); }
            set { SetValue(CurrentExtentLabelColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtentLabelColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentExtentLabelColorProperty =
            DependencyProperty.Register(nameof(CurrentExtentLabelColor), typeof(Brush), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the color of the time step interval labels
        /// </summary>
		public Brush TimeStepIntervalLabelColor
        {
            get { return (Brush)GetValue(TimeStepIntervalLabelColorProperty); }
            set { SetValue(TimeStepIntervalLabelColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepIntervalLabelColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeStepIntervalLabelColorProperty =
            DependencyProperty.Register(nameof(TimeStepIntervalLabelColor), typeof(Brush), typeof(TimeSlider), null);

#endregion // Appearance Properties

#endregion // Properties

#region Initialization Helper Methods

        /// <summary>
        /// Updates the time slider to have the specified number of time steps
        /// </summary>
        /// <param name="count">The number of time steps</param>
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
                return;

            TimeStepInterval = FullExtent.Divide(count);
        }

        /// <summary>
        /// Initializes the time slider's temporal properties based on the specified GeoView. Specifically,
        /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>
        /// </summary>
        /// <param name="geoView">The GeoView to use to initialize the time-slider's properties</param>
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
                    canUseInstantaneousTime = false;
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
        /// <param name="timeAwareLayer">The layer to use to initialize the time slider</param>
        public async Task InitializeTimePropertiesAsync(ITimeAware timeAwareLayer)
        {
            FullExtent = timeAwareLayer.FullTimeExtent;
            TimeStepInterval = await GetTimeStepIntervalAsync(timeAwareLayer);
            // TODO: Double-check whether we can choose a better default for current extent - does not seem to be exposed
            // at all in service metadata
            CurrentExtent = await CanUseInstantaneousTimeAsync(timeAwareLayer) ?
                new TimeExtent(FullExtent.StartTime) : new TimeExtent(FullExtent.StartTime, TimeSteps.ElementAt(1));

            // TODO: Initialize time-zone (will require converting time zone string to strong type)
        }

        /// <summary>
        /// Gets the default time-step interval for the specified time-aware layer
        /// </summary>
        /// <param name="timeAwareLayer">The time-aware layer to retrieve the interval for</param>
        /// <returns>The interval, represented as a <see cref="TimeValue"/> instance</returns>
        private static async Task<TimeValue> GetTimeStepIntervalAsync(ITimeAware timeAwareLayer)
        {
            var timeStepInterval = timeAwareLayer.TimeInterval;

            // For map image layers, if the map service does not have a time step interval, check the time step intervals
            // of the service's sub-layers
            if (timeStepInterval == null && timeAwareLayer is ArcGISMapImageLayer mapImageLayer)
            {
                // Get the largest time-step interval defined by the service's sub-layers
                foreach (var sublayer in mapImageLayer.Sublayers)
                {
                    if (sublayer.IsVisible) // Only use visible sub-layers
                    {
                        var timeInfo = await GetTimeInfoAsync(sublayer);
                        if (timeInfo == null)
                            continue;

                        if (timeInfo != null && (timeStepInterval == null || timeInfo.Interval.IsGreaterThan(timeStepInterval)))
                            timeStepInterval = timeInfo.Interval;
                    }
                }
            }

            return timeStepInterval;
        }

        /// <summary>
        /// Gets whether an instantaneous time filter can be applied to the specified time-aware layer 
        /// </summary>
        /// <param name="timeAwareLayer">The time-aware layer to check</param>
        /// <returns><c>true</c> if the layer can be filtered based on a time instant, otherwise <c>false</c></returns>
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
                            continue;

                        if (string.IsNullOrEmpty(timeInfo.StartTimeField) || string.IsNullOrEmpty(timeInfo.EndTimeField))
                        {
                            canUseInstantaneousTime = false; // Instantaneous time filtering won't work for this sub-layer
                            break;
                        }
                    }
                }
            }
            else // !(timeAwareLayer is ArcGISMapImageLayer)
            {
                var timeInfo = await GetTimeInfoAsync(timeAwareLayer as ILoadable);
                canUseInstantaneousTime = !string.IsNullOrEmpty(timeInfo?.StartTimeField) && !string.IsNullOrEmpty(timeInfo?.EndTimeField);
            }

            return canUseInstantaneousTime;
        }

        /// <summary>
        /// Returns the layer's time-info, if applicable
        /// </summary>
        private static async Task<LayerTimeInfo> GetTimeInfoAsync(ILoadable layer) // Can't be of type Layer since ArcGISSublayer doesn't inherit from that
        {
            if (!(layer is ArcGISSublayer) && !(layer is FeatureLayer) && !(layer is RasterLayer))
                return null;

            try
            {
                await layer.LoadAsync();
                if (layer.LoadStatus != LoadStatus.Loaded)
                    return null;
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
        public event EventHandler<CurrentExtentChangedEventArgs> CurrentExtentChanged;

#endregion

#region Playback Methods

        /// <summary>
        /// Moves the slider position forward by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the position could not be moved as requested</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position</param>
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
                throw new ArgumentOutOfRangeException(nameof(timeSteps), $"{nameof(timeSteps)} must be greater than zero");

            var preserveSpan = !IsStartTimePinned && !IsEndTimePinned;
            return MoveTimeStep(timeSteps, preserveSpan);
        }

        /// <summary>
        /// Moves the slider position back by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the position could not be moved as requested</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position</param>
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
                throw new ArgumentOutOfRangeException(nameof(timeSteps), $"{nameof(timeSteps)} must be greater than zero");

            var preserveSpan = !IsStartTimePinned && !IsEndTimePinned;
            return MoveTimeStep(0 - timeSteps, preserveSpan);
        }

        /// <summary>
        /// Moves the current start and end times by the specified number of time steps
        /// </summary>
        /// <param name="timeSteps">The number of time steps by which to move the current time.  A positive number will advance the time step forward, while
        /// a negative value will move the current time step backward</param>
        /// <param name="preserveSpan">Whether to preserve the number of time steps between the current start and end time</param>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the time step could not be moved as requested</returns>
        private bool MoveTimeStep(int timeSteps, bool preserveSpan)
        {
            if (TimeSteps == null || CurrentValidExtent == null || (IsStartTimePinned && IsEndTimePinned))
                return false;

            // Always preserve the number of intervals between start and end when the current time is a time instant
            preserveSpan = IsCurrentExtentTimeInstant ? true : preserveSpan;

            // We want to rely on step indexes, so we use the known backing type here since that's most efficent.
            // If the backing type changes, or if the property is changed to be settable, the implemetation here
            // will need to be updated accordingly.
            var timeStepsList = (List<DateTimeOffset>)TimeSteps;

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
                validTimeStepDelta = startTimeStepIndex + timeSteps <= maxTimeStepIndex ? timeSteps : maxTimeStepIndex - startTimeStepIndex;
            else
                validTimeStepDelta = endTimeStepIndex + timeSteps >= minTimeStepIndex ? timeSteps : minTimeStepIndex - endTimeStepIndex;

            // Get the new start and end time step indexes
            var newStartTimeStepIndex = !IsStartTimePinned && (validTimeStepDelta > 0 || startTimeStepIndex + validTimeStepDelta >= minTimeStepIndex) ?
                startTimeStepIndex + validTimeStepDelta : startTimeStepIndex;
            var newEndTimeStepIndex = !IsEndTimePinned && (validTimeStepDelta < 0 || endTimeStepIndex + validTimeStepDelta <= maxTimeStepIndex) ?
                endTimeStepIndex + validTimeStepDelta : endTimeStepIndex;

            // Adjust the new index in the event that it's coincident with the max or min and the current time extent is a time range (i.e. not a
            // time instant.  In that case, we need to preserve at least one time step between the start and end times.
            if (newStartTimeStepIndex == maxTimeStepIndex && !IsCurrentExtentTimeInstant)
                newStartTimeStepIndex--;
            if (newEndTimeStepIndex == minTimeStepIndex && !IsCurrentExtentTimeInstant)
                newEndTimeStepIndex++;

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
                    CurrentExtent = new TimeExtent(newStartTime);
                else
                    CurrentExtent = new TimeExtent(newStartTime, newEndTime);
            }

            // Return whether or not the time extent was moved by the specified number of steps
            return isRequestedMoveValid; 
        }

        /// <summary>
        /// Gets or sets whether the time slider is animating playback
        /// </summary>
        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="FullExtent"/>
        /// </summary>
        public string FullExtentLabelFormat
        {
            get { return (string)GetValue(FullExtentLabelFormatProperty); }
            set { SetValue(FullExtentLabelFormatProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtentLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FullExtentLabelFormatProperty =
            DependencyProperty.Register(nameof(FullExtentLabelFormat), typeof(string), typeof(TimeSlider),
                new PropertyMetadata(default(string), OnFullExtentLabelFormatPropertyChanged));

        private static void OnFullExtentLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            var newLabelFormat = e.NewValue as string;

            // Apply the updated string format to the full extent label elements' bindings
            slider.FullExtentStartTimeLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: newLabelFormat,
                fallbackFormat: ref slider._originalFullExtentLabelFormat);
            slider.FullExtentEndTimeLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: newLabelFormat,
                fallbackFormat: ref slider._originalFullExtentLabelFormat);
        }

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="CurrentExtent"/>
        /// </summary>
        public string CurrentExtentLabelFormat
        {
            get { return (string)GetValue(CurrentExtentLabelFormatProperty); }
            set { SetValue(CurrentExtentLabelFormatProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtentLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentExtentLabelFormatProperty =
            DependencyProperty.Register(nameof(CurrentExtentLabelFormat), typeof(string), typeof(TimeSlider),
                new PropertyMetadata(default(string), OnCurrentExtentLabelFormatPropertyChanged));

        private static void OnCurrentExtentLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            var newLabelFormat = e.NewValue as string;

            // Apply the updated string format to the current extent label elements' bindings
            slider.MinimumThumbLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: newLabelFormat,
                fallbackFormat: ref slider._originalCurrentExtentLabelFormat);
            slider.MaximumThumbLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: newLabelFormat,
                fallbackFormat: ref slider._originalCurrentExtentLabelFormat);

            // Layout the slider to accommodate updated label text
            slider.UpdateTrackLayout(slider.CurrentExtent);
        }

        /// <summary>
        /// Gets or sets the string format to use for displaying the labels for the tick marks representing each time step interval
        /// </summary>
        public string TimeStepIntervalLabelFormat
        {
            get { return (string)GetValue(TimeStepIntervalLabelFormatProperty); }
            set { SetValue(TimeStepIntervalLabelFormatProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepIntervalLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeStepIntervalLabelFormatProperty =
            DependencyProperty.Register(nameof(TimeStepIntervalLabelFormat), typeof(string), typeof(TimeSlider),
                new PropertyMetadata(default(string), OnTimeStepIntervalLabelFormatPropertyChanged));

        private static void OnTimeStepIntervalLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            if (slider.Tickmarks != null)
            {
                slider.Tickmarks.TickLabelFormat = e.NewValue as string;
            }
        }

        /// <summary>
        /// Gets or sets the mode to use for labels along the time slider
        /// </summary>
        public TimeSliderLabelMode LabelMode
        {
            get { return (TimeSliderLabelMode)GetValue(LabelModeProperty); }
            set { SetValue(LabelModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LabelMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelModeProperty =
            DependencyProperty.Register(nameof(LabelMode), typeof(TimeSliderLabelMode), typeof(TimeSlider),
                new PropertyMetadata(default(TimeSliderLabelMode), OnLabelModePropertyChanged));

        private static void OnLabelModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            var labelMode = (TimeSliderLabelMode)e.NewValue;
            slider.ApplyLabelMode(labelMode);
        }

        /// <summary>
        /// Updates the slider for the specified label mode
        /// </summary>
        /// <param name="labelMode">The label mode to apply</param>
        private void ApplyLabelMode(TimeSliderLabelMode labelMode)
        {
            if (Tickmarks == null || MinimumThumbLabel == null || MaximumThumbLabel == null)
                return;

            switch (labelMode)
            {
                case TimeSliderLabelMode.None:
                    Tickmarks.ShowTickLabels = false;
                    MinimumThumbLabel.Visibility = Visibility.Collapsed;
                    MaximumThumbLabel.Visibility = Visibility.Collapsed;
                    break;
                case TimeSliderLabelMode.CurrentExtent:
                    Tickmarks.ShowTickLabels = false;
                    MinimumThumbLabel.Visibility = Visibility.Visible;
                    MaximumThumbLabel.Visibility = Visibility.Visible;
                    break;
                case TimeSliderLabelMode.TimeStepInterval:
                    Tickmarks.ShowTickLabels = true;
                    MinimumThumbLabel.Visibility = Visibility.Collapsed;
                    MaximumThumbLabel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// Moves the time slider's current extent upon expiration of the playback interval
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

                    // We want to rely on step indexes, so we use the known backing type here since that's most efficent.
                    // If the backing type changes, the implemetation here will need to be updated accordingly
                    var timeStepsList = (List<DateTimeOffset>)TimeSteps;

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
#endregion
    }
}

#endif