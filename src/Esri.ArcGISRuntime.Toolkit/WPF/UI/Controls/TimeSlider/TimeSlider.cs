// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

// Implementation ported from https://github.com/Esri/arcgis-toolkit-sl-wpf

using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The TimeSlider is a utility Control that emits TimeExtent values typically for use with the Map Control 
    /// to enhance the viewing of geographic features that have attributes based upon Date/Time information.
    /// </summary>
    [TemplatePart(Name = "HorizontalTrack", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "HorizontalTrackThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "MinimumThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "MaximumThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "TickMarks", Type = typeof(Primitives.TickBar))]
    [TemplatePart(Name = "HorizontalTrackLargeChangeDecreaseRepeatButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "HorizontalTrackLargeChangeIncreaseRepeatButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PlayPauseButton", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "NextButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PreviousButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "FullExtentStartTimeTextBlock", Type = typeof(TextBlock))]
    [TemplatePart(Name = "FullExtentEndTimeTextBlock", Type = typeof(TextBlock))]
    [TemplateVisualState(GroupName = "CommonStates", Name = "Normal")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "MouseOver")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "Disabled")]
    [TemplateVisualState(GroupName = "FocusStates", Name = "Focused")]
    [TemplateVisualState(GroupName = "FocusStates", Name = "Unfocused")]
    public class TimeSlider : Control
    {
        #region Fields
        FrameworkElement SliderTrack;
        Thumb MinimumThumb;
        Thumb MaximumThumb;
        Thumb HorizontalTrackThumb;
        RepeatButton ElementHorizontalLargeDecrease;
        RepeatButton ElementHorizontalLargeIncrease;
        ToggleButton PlayPauseButton;
        ButtonBase NextButton;
        ButtonBase PreviousButton;
        TextBlock _fullExtentStartTimeTextBlock;
        TextBlock _fullExtentEndTimeTextBlock;
        Primitives.TickBar TickMarks;
        DispatcherTimer playTimer;
        private TimeExtent currentValue;
        private bool isFocused;
        private bool isMouseOver;
        private double totalHorizontalChange;
        private TimeExtent HorizontalChangeExtent;
        private bool _valueInitialized = false;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
        public TimeSlider()
        {
            //TimeSteps = new ObservableCollection<DateTime>();
            playTimer = new DispatcherTimer() { Interval = this.PlaybackInterval };
            playTimer.Tick += playTimer_Tick;
            SizeChanged += TimeSlider_SizeChanged;
        }

        private async void TimeSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTrackLayout(CurrentValidExtent);

            if (!_valueInitialized && CurrentExtent == null && CurrentValidExtent != null)
            {
                await System.Threading.Tasks.Task.Delay(2000);
                CurrentExtent = CurrentValidExtent;
                _valueInitialized = true;
            }
        }
        /// <summary>
        /// Static initialization for the <see cref="TimeSlider"/> control.
        /// </summary>
        static TimeSlider()
        {
#if !SILVERLIGHT
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeSlider),
                new FrameworkPropertyMetadata(typeof(TimeSlider)));
#endif
        }
        #region Overrides

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application
        /// code or internal processes (such as a rebuilding layout pass) call
        /// <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SliderTrack = GetTemplateChild("HorizontalTrack") as FrameworkElement;
            if (SliderTrack != null)
                SliderTrack.SizeChanged += TimeSlider_SizeChanged;
            HorizontalTrackThumb = GetTemplateChild("HorizontalTrackThumb") as Thumb;
            MinimumThumb = GetTemplateChild("MinimumThumb") as Thumb;
            MaximumThumb = GetTemplateChild("MaximumThumb") as Thumb;
            ElementHorizontalLargeDecrease = GetTemplateChild("HorizontalTrackLargeChangeDecreaseRepeatButton") as RepeatButton;
            ElementHorizontalLargeIncrease = GetTemplateChild("HorizontalTrackLargeChangeIncreaseRepeatButton") as RepeatButton;
            PlayPauseButton = GetTemplateChild("PlayPauseButton") as ToggleButton;
            NextButton = GetTemplateChild("NextButton") as ButtonBase;
            PreviousButton = GetTemplateChild("PreviousButton") as ButtonBase;
            TickMarks = GetTemplateChild("TickMarks") as Primitives.TickBar;
            _fullExtentStartTimeTextBlock = GetTemplateChild("FullExtentStartTimeTextBlock") as TextBlock;
            _fullExtentEndTimeTextBlock = GetTemplateChild("FullExtentEndTimeTextBlock") as TextBlock;

            if (MinimumThumb != null)
            {
                MinimumThumb.DragDelta += MinimumThumb_DragDelta;
                MinimumThumb.DragCompleted += DragCompleted;
                MinimumThumb.DragStarted += (s, e) => { Focus(); };
            }
            if (MaximumThumb != null)
            {
                MaximumThumb.DragDelta += MaximumThumb_DragDelta;
                MaximumThumb.DragCompleted += DragCompleted;
                MaximumThumb.DragStarted += (s, e) => { Focus(); };
            }
            if (HorizontalTrackThumb != null)
            {
                HorizontalTrackThumb.DragDelta += HorizontalTrackThumb_DragDelta;
                HorizontalTrackThumb.DragCompleted += DragCompleted;
                HorizontalTrackThumb.DragStarted += (s, e) => { Focus(); };
            }
            if (ElementHorizontalLargeDecrease != null)
            {
                ElementHorizontalLargeDecrease.Click += (s, e) => { Focus(); if (IsPlaying) { IsPlaying = false; } StepBack(); };
            }
            if (ElementHorizontalLargeIncrease != null)
            {
                ElementHorizontalLargeIncrease.Click += (s, e) => { Focus(); if (IsPlaying) { IsPlaying = false; } StepForward(); };
            }
            if (PlayPauseButton != null)
            {
                this.IsPlaying = PlayPauseButton.IsChecked.Value;
                PlayPauseButton.Checked += (s, e) => { this.IsPlaying = true; };
                PlayPauseButton.Unchecked += (s, e) => { this.IsPlaying = false; };
            }
            if (NextButton != null)
            {
                NextButton.Click += (s, e) => { this.StepForward(); };
            }
            if (PreviousButton != null)
            {
                PreviousButton.Click += (s, e) => { this.StepBack(); };
            }
            CreateTickmarks();
            SetButtonVisibility();
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.LostFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            isFocused = false;
            ChangeVisualState(true);
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.GotFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            isFocused = true;
            ChangeVisualState(true);
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.MouseEnter"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            isMouseOver = true;
            if (this.MinimumThumb != null && !this.MinimumThumb.IsDragging ||
                this.MaximumThumb != null && !this.MaximumThumb.IsDragging ||
                this.MinimumThumb == null && this.MaximumThumb == null)
            {
                ChangeVisualState(true);
            }
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.MouseLeave"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            isMouseOver = false;
            if (this.MinimumThumb != null && !this.MinimumThumb.IsDragging ||
                this.MaximumThumb != null && !this.MaximumThumb.IsDragging ||
                this.MinimumThumb == null && this.MaximumThumb == null)
            {
                ChangeVisualState(true);
            }
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.MouseLeftButtonDown"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!e.Handled && IsEnabled)
            {
                Focus();
            }
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.KeyDown"/> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnKeyDown(KeyEventArgs e)
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

        #endregion

        private void ChangeVisualState(bool useTransitions)
        {
            //CommonStates
            if (!base.IsEnabled)
            {
                VisualStateManager.GoToState(this, "Disabled", useTransitions);
            }
            else if (this.isMouseOver)
            {
                VisualStateManager.GoToState(this, "MouseOver", useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal", useTransitions);
            }
            //FocusStates
            if (this.isFocused && base.IsEnabled)
            {
                VisualStateManager.GoToState(this, "Focused", useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, "Unfocused", useTransitions);
            }
        }

        private void UpdateTrackLayout(TimeExtent extent)
        {
            if (extent == null || extent.StartTime < FullExtentStartTime || extent.EndTime > FullExtentEndTime ||
                MinimumThumb == null || MaximumThumb == null || FullExtentEndTime <=
                FullExtentStartTime || SliderTrack == null)
                return;

            double sliderWidth = SliderTrack.ActualWidth;
            double minimum = FullExtentStartTime.Ticks;
            double maximum = FullExtentEndTime.Ticks;

            // time			
            TimeExtent snapped = Snap(extent, false);
            double start = snapped.StartTime.DateTime.Ticks;
            double end = snapped.EndTime.DateTime.Ticks;

            //margins 
            double left = 0;
            double right = 0;
            bool hasIntervals = (TimeSteps == null) ? false : TimeSteps.GetEnumerator().MoveNext();

            // rate = (distance) / (time)				
            double rate = GetTrackWidth() / (maximum - minimum);

            if (!IsCurrentExtentTimeInstant && !hasIntervals)
            {
                //left repeater	
                right = Math.Min(sliderWidth, ((maximum - start) * rate) + MinimumThumb.ActualWidth + MaximumThumb.ActualWidth);
                ElementHorizontalLargeDecrease.Margin = new Thickness(0, 0, right, 0);

                //minimum thumb
                left = Math.Min(sliderWidth, (start - minimum) * rate);
                right = Math.Min(sliderWidth, ((maximum - start) * rate) + MaximumThumb.ActualWidth);
                MinimumThumb.Margin = new Thickness(left, 0, right, 0);

                //middle thumb
                left = Math.Min(sliderWidth, ((start - minimum) * rate) + (MinimumThumb.ActualWidth) / 2);
                right = Math.Min(sliderWidth, (maximum - end) * rate + (MaximumThumb.ActualWidth) / 2);
                HorizontalTrackThumb.Margin = new Thickness(left, 0, right, 0);
                HorizontalTrackThumb.Width = Math.Max(0, (sliderWidth - right - left));

                //maximum thumb
                left = Math.Min(sliderWidth, (end - minimum) * rate + MinimumThumb.ActualWidth);
                right = Math.Min(sliderWidth, ((maximum - end) * rate));
                MaximumThumb.Margin = new Thickness(left, 0, right, 0);

                //right repeater
                left = Math.Min(sliderWidth, ((end - minimum) * rate) + MinimumThumb.ActualWidth + MaximumThumb.ActualWidth);
                ElementHorizontalLargeIncrease.Margin = new Thickness(left, 0, 0, 0);
            }
            else if (hasIntervals) //one or two thumbs
            {
                //left repeater								
                right = Math.Min(sliderWidth, ((maximum - start) * rate) + MaximumThumb.ActualWidth);
                ElementHorizontalLargeDecrease.Margin = new Thickness(0, 0, right, 0);

                //minimum thumb
                if (!IsCurrentExtentTimeInstant)
                {
                    left = Math.Min(sliderWidth, (start - minimum) * rate);
                    right = Math.Min(sliderWidth, ((maximum - start) * rate));
                    MinimumThumb.Margin = new Thickness(left, 0, right, 0);
                }
                else
                {
                    MinimumThumb.Margin = new Thickness(0, 0, sliderWidth, 0);
                }

                //middle thumb
                if (IsCurrentExtentTimeInstant)
                {
                    HorizontalTrackThumb.Margin = new Thickness(0, 0, sliderWidth, 0);
                    HorizontalTrackThumb.Width = 0;
                }
                else if (!IsCurrentExtentTimeInstant) //&& !IsStartTimePinned && !IsEndTimePinned)
                {
                    // TODO: validate this (sizing of middle thumb) works as expected when start and/or end times are pinned

                    left = Math.Min(sliderWidth, ((start - minimum) * rate) + (MinimumThumb.ActualWidth / 2));
                    right = Math.Min(sliderWidth, (maximum - end) * rate + (MaximumThumb.ActualWidth / 2));
                    HorizontalTrackThumb.Margin = new Thickness(left, 0, right, 0);
                    HorizontalTrackThumb.Width = Math.Max(0, (sliderWidth - right - left));
                    HorizontalTrackThumb.HorizontalAlignment = HorizontalAlignment.Left;
                }
                //else
                //{
                //	right = Math.Min(sliderWidth, ((maximum - end) * rate) + MaximumThumb.ActualWidth);
                //	HorizontalTrackThumb.Margin = new Thickness(0, 0, right, 0);
                //	HorizontalTrackThumb.Width = (sliderWidth - right);
                //	HorizontalTrackThumb.HorizontalAlignment = HorizontalAlignment.Left;
                //}

                //maximum thumb
                left = Math.Min(sliderWidth, (end - minimum) * rate);
                right = Math.Min(sliderWidth, ((maximum - end) * rate));
                MaximumThumb.Margin = new Thickness(left, 0, right, 0);

                //right repeater
                left = Math.Min(sliderWidth, ((end - minimum) * rate) + MaximumThumb.ActualWidth);
                ElementHorizontalLargeIncrease.Margin = new Thickness(left, 0, 0, 0);
            }
            else //no intervals, one thumb or two thumbs where start==end
            {
                //left repeater				
                right = Math.Min(sliderWidth, ((maximum - end) * rate) + MaximumThumb.ActualWidth);
                ElementHorizontalLargeDecrease.Margin = new Thickness(0, 0, right, 0);

                //minimum thumb
                MinimumThumb.Margin = new Thickness(0, 0, sliderWidth, 0);

                //middle thumb
                if (IsCurrentExtentTimeInstant)
                {
                    HorizontalTrackThumb.Margin = new Thickness(0, 0, sliderWidth, 0);
                    HorizontalTrackThumb.Width = 0;
                }
                else
                {
                    HorizontalTrackThumb.Margin = new Thickness(0, 0, right, 0);
                    HorizontalTrackThumb.Width = (sliderWidth - right);
                    HorizontalTrackThumb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                }

                //maximum thumb
                left = Math.Min(sliderWidth, (end - minimum) * rate);
                right = Math.Min(sliderWidth, ((maximum - end) * rate));
                MaximumThumb.Margin = new Thickness(left, 0, right, 0);

                //right repeater
                left = Math.Min(sliderWidth, ((end - minimum) * rate) + MaximumThumb.ActualWidth);
                ElementHorizontalLargeIncrease.Margin = new Thickness(left, 0, 0, 0);
            }
        }

        #region Drag event handlers

        private void HorizontalTrackThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (IsPlaying) IsPlaying = false;
            if (e.HorizontalChange == 0 || IsStartTimePinned || IsEndTimePinned)
                return;
            if (currentValue == null) currentValue = CurrentValidExtent;
            totalHorizontalChange = e.HorizontalChange;

            HorizontalChangeExtent = new TimeExtent(currentValue.StartTime, currentValue.EndTime);
            // time ratio 
            long TimeRate = (FullExtentEndTime.Ticks - FullExtentStartTime.Ticks) / (long)GetTrackWidth();

            // time change
            long TimeChange = (long)(TimeRate * totalHorizontalChange);

            TimeSpan difference = new TimeSpan(currentValue.EndTime.DateTime.Ticks - currentValue.StartTime.DateTime.Ticks);

            TimeExtent tempChange = null;
            try
            {
                tempChange = new TimeExtent(HorizontalChangeExtent.StartTime.DateTime.AddTicks(TimeChange),
                    HorizontalChangeExtent.EndTime.DateTime.AddTicks(TimeChange));
            }
            catch (ArgumentOutOfRangeException)
            {
                if (totalHorizontalChange < 0)
                    tempChange = new TimeExtent(FullExtentStartTime, FullExtentStartTime.Add(difference));
                else if (totalHorizontalChange > 0)
                    tempChange = new TimeExtent(FullExtentEndTime.Subtract(difference), FullExtentEndTime);
            }

            if (tempChange.StartTime.DateTime.Ticks < FullExtentStartTime.Ticks)
                currentValue = Snap(new TimeExtent(FullExtentStartTime, FullExtentStartTime.Add(difference)), true);
            else if (tempChange.EndTime.DateTime.Ticks > FullExtentEndTime.Ticks)
                currentValue = Snap(new TimeExtent(FullExtentEndTime.Subtract(difference), FullExtentEndTime), true);
            else
                currentValue = Snap(new TimeExtent(tempChange.StartTime, tempChange.EndTime), true);

            UpdateTrackLayout(currentValue);
        }

        private void MinimumThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (IsPlaying) IsPlaying = false;
            if (e.HorizontalChange == 0) return;
            if (currentValue == null) currentValue = CurrentValidExtent;
            totalHorizontalChange = e.HorizontalChange;
            HorizontalChangeExtent = new TimeExtent(currentValue.StartTime, currentValue.EndTime);
            // time ratio 
            long TimeRate = (FullExtentEndTime.Ticks - FullExtentStartTime.Ticks) / (long)GetTrackWidth();

            // time change
            long TimeChange = (long)(TimeRate * totalHorizontalChange);

            TimeExtent tempChange = null;
            try
            {
                tempChange = new TimeExtent(HorizontalChangeExtent.StartTime.DateTime.AddTicks(TimeChange), HorizontalChangeExtent.EndTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                if (totalHorizontalChange < 0)
                    tempChange = new TimeExtent(FullExtentStartTime, currentValue.EndTime);
                else if (totalHorizontalChange > 0)
                    tempChange = new TimeExtent(currentValue.EndTime);
            }

            if (tempChange.StartTime.DateTime.Ticks < FullExtentStartTime.Ticks)
                currentValue = Snap(new TimeExtent(FullExtentStartTime, currentValue.EndTime), false);
            else if (tempChange.StartTime >= currentValue.EndTime)
                currentValue = Snap(new TimeExtent(currentValue.EndTime), false);
            else
                currentValue = Snap(new TimeExtent(tempChange.StartTime, tempChange.EndTime), false);

            UpdateTrackLayout(currentValue);
        }

        private void MaximumThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (IsPlaying) IsPlaying = false;
            if (e.HorizontalChange == 0) return;
            if (currentValue == null) currentValue = CurrentValidExtent;
            totalHorizontalChange = e.HorizontalChange;
            HorizontalChangeExtent = new TimeExtent(currentValue.StartTime, currentValue.EndTime);
            // time ratio 
            long TimeRate = (FullExtentEndTime.Ticks - FullExtentStartTime.Ticks) / (long)GetTrackWidth();

            // time change
            long TimeChange = (long)(TimeRate * totalHorizontalChange);

            TimeExtent tempChange = null;
            if (IsCurrentExtentTimeInstant)
            {
                try
                {
                    // If the mouse drag creates a date thats year is before 
                    // 1/1/0001 or after 12/31/9999 then an out of renge 
                    // exception will be trown.
                    tempChange = new TimeExtent(HorizontalChangeExtent.EndTime.DateTime.AddTicks(TimeChange));
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (totalHorizontalChange > 0) // date is after 12/31/9999
                        tempChange = new TimeExtent(FullExtentEndTime);
                    else if (totalHorizontalChange < 0) // date is before 1/1/0001
                        tempChange = new TimeExtent(FullExtentStartTime);
                }
            }
            else
            {
                try
                {
                    // TODO: Revisit this limitation

                    // If the mouse drag creates a date thats year is before 
                    // 1/1/0001 or after 12/31/9999 then an out of renge 
                    // exception will be trown.
                    tempChange = new TimeExtent(HorizontalChangeExtent.StartTime, HorizontalChangeExtent.EndTime.DateTime.AddTicks(TimeChange));
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (totalHorizontalChange > 0) // date is after 12/31/9999
                        tempChange = new TimeExtent(currentValue.StartTime, FullExtentEndTime);
                    else if (totalHorizontalChange < 0) // date is before 1/1/0001
                        tempChange = new TimeExtent(currentValue.StartTime);
                }
            }

            // validate change
            if (IsCurrentExtentTimeInstant)
            {
                if (tempChange.EndTime.DateTime.Ticks > FullExtentEndTime.Ticks)
                    currentValue = Snap(new TimeExtent(FullExtentEndTime), false);
                else if (tempChange.EndTime.DateTime.Ticks < FullExtentStartTime.Ticks)
                    currentValue = Snap(new TimeExtent(FullExtentStartTime), false);
                else
                    currentValue = Snap(new TimeExtent(tempChange.EndTime), false);
            }
            else
            {
                if (tempChange.EndTime.DateTime.Ticks > FullExtentEndTime.Ticks)
                    currentValue = Snap(new TimeExtent(currentValue.StartTime, FullExtentEndTime), false);
                else if (tempChange.EndTime <= currentValue.StartTime && !IsCurrentExtentTimeInstant) // TODO: Preserve one time step between min and max thumbs
                    currentValue = Snap(new TimeExtent(currentValue.StartTime, currentValue.StartTime.DateTime.AddMilliseconds(1)), false);
                else if (tempChange.EndTime.DateTime.Ticks < FullExtentStartTime.Ticks) // TODO: Preserve one time step between min and max thumbs
                    currentValue = Snap(new TimeExtent(FullExtentStartTime), false);
                else
                    currentValue = Snap(new TimeExtent(currentValue.StartTime, tempChange.EndTime), false);
            }

            UpdateTrackLayout(currentValue);
        }

        private double GetTrackWidth()
        {
            if (SliderTrack == null) return 0;
            bool hasIntervals = (TimeSteps == null) ? false : TimeSteps.GetEnumerator().MoveNext();
            double trackWidth;
            if (!IsCurrentExtentTimeInstant && !hasIntervals)
                trackWidth = SliderTrack.ActualWidth - (MinimumThumb == null ? 0 : MinimumThumb.ActualWidth) - (MaximumThumb == null ? 0 : MaximumThumb.ActualWidth);
            else
                trackWidth = SliderTrack.ActualWidth - (MaximumThumb == null ? 0 : MaximumThumb.ActualWidth);
            return trackWidth;
        }

        private void DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (currentValue == null) return;
            TimeExtent newTimeExtent = null;
            if ((sender as Thumb).Name == nameof(HorizontalTrackThumb))
            {
                newTimeExtent = Snap(new TimeExtent(currentValue.StartTime, currentValue.EndTime), true);
            }
            else
            {
                var newStartTime = IsStartTimePinned ? CurrentValidExtent.StartTime : currentValue.StartTime;
                var newEndTime = IsEndTimePinned ? CurrentValidExtent.EndTime : currentValue.EndTime;
                newTimeExtent = Snap(new TimeExtent(newStartTime, newEndTime), false);
            }

            CurrentExtent = newTimeExtent;
        }

        #endregion

        private TimeExtent Snap(TimeExtent extent, bool preserveSpan)
        {
            if (extent == null) return null;
            if (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext())
            {
                DateTimeOffset start = extent.StartTime < FullExtentStartTime ? FullExtentStartTime : extent.StartTime;
                DateTimeOffset end = extent.EndTime > FullExtentEndTime ? FullExtentEndTime : extent.EndTime;
                start = (start > end ? end : start);
                end = (end < start ? start : end);
                TimeExtent result = new TimeExtent(start, end);

                //snap min thumb.								
                long d0 = long.MaxValue;
                foreach (DateTimeOffset d in TimeSteps)
                {
                    long delta = Math.Abs((d - start).Ticks);
                    if (delta < d0)
                    {
                        // TODO: update to use validated extent without circular reference
                        if (CurrentExtent?.StartTime == CurrentExtent?.EndTime)
                        {
                            d0 = delta;
                            result = new TimeExtent(d);
                        }
                        else if (d < end)
                        {
                            d0 = delta;
                            result = new TimeExtent(d, result.EndTime);
                        }
                    }
                }

                if (preserveSpan)
                {
                    //check interval difference between min and max.
                    int intervalDifference = 0;
                    bool count = false;
                    // TODO: update to use validated extent without circular reference
                    if (CurrentExtent?.StartTime != CurrentExtent?.EndTime)
                    {
                        foreach (DateTimeOffset d in TimeSteps)
                        {
                            count = (d >= CurrentValidExtent.StartTime && d < CurrentValidExtent.EndTime) ? true : false;
                            if (count) intervalDifference++;
                        }
                    }

                    //snap max thumb.
                    long d1 = long.MaxValue;
                    count = false;
                    int diff = 0;
                    foreach (DateTimeOffset d in TimeSteps)
                    {
                        long delta = Math.Abs((d - end).Ticks);
                        if (delta < d1)
                        {
                            // TODO: update to use validated extent without circular reference
                            if (CurrentExtent?.StartTime == currentValue?.EndTime || d > result.StartTime)
                            {
                                if (intervalDifference != 0)
                                {
                                    count = (d >= result.StartTime) ? true : false;
                                    if (count) diff++;
                                    if (diff == intervalDifference)
                                    {
                                        result = new TimeExtent(result.StartTime, d);
                                        return result;
                                    }
                                }
                                else
                                {
                                    d1 = delta;
                                    result = new TimeExtent(result.StartTime, d);
                                }
                            }
                        }
                    }
                }
                else
                {
                    long d1 = long.MaxValue;
                    foreach (DateTimeOffset d in TimeSteps)
                    {
                        long delta = Math.Abs((d - end).Ticks);
                        if (delta < d1)
                        {
                            // TODO: update to use validated extent without circular reference
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
                }
                //return snapped extent
                return result;
            }
            else return extent;

        }

        //private void SnapFullExtent()
        //{
        //    if (FullExtent == null || TimeStepInterval == null)
        //        return;

        //    var newStartTime = FullExtent.StartTime;

        //    var newEndTime = FullExtent.StartTime;
        //    for (var time = FullExtent.StartTime; time <= FullExtent.EndTime; time + TimeStepInterval)
        //    {

        //    }
        //}

        private void CreateTickmarks()
        {
            if (TickMarks == null || FullExtentStartTime >= FullExtentEndTime) return;

            TickMarks.TickMarkPositions = null;
            if (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext())
            {
                long span = FullExtentEndTime.Ticks - FullExtentStartTime.Ticks;
                List<double> intervals = new List<double>();

                // Create a tick mark for every time step from the 2nd to the 2nd to last.  We don't create ticks
                // here for the first and last time step  because those are explicitly placed in the control template.
                for (int i = 1; i < TimeSteps.Count() - 1; i++)
                {
                    var d = TimeSteps.ElementAt(i);
                    intervals.Add((d.Ticks - FullExtentStartTime.Ticks) / (double)span);
                }
                TickMarks.TickMarkPositions = intervals.ToArray();
            }
        }

        #region Properties

        // Validates the current time extent and, in the case of it being invalid, returns a valid substitute
        private TimeExtent CurrentValidExtent
        {
            get
            {
                TimeExtent value = null;
                if (CurrentExtent == null)
                {
                    value = FullExtent == null ? new TimeExtent(FullExtentStartTime, FullExtentEndTime) : FullExtent;
                }
                else
                {
                    if (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext())
                        value = Snap(CurrentExtent, false);
                    else if (CurrentExtent.StartTime.DateTime < FullExtentStartTime && CurrentExtent.EndTime > FullExtentEndTime)
                        value = new TimeExtent(FullExtentStartTime, FullExtentEndTime);
                    else if (CurrentExtent.StartTime.DateTime < FullExtentStartTime)
                        value = new TimeExtent(FullExtentStartTime, CurrentExtent.EndTime);
                    else if (CurrentExtent.EndTime.DateTime > FullExtentEndTime)
                        value = new TimeExtent(CurrentExtent.StartTime, FullExtentEndTime);
                }
                //if (TimeMode == Toolkit.TimeMode.TimeInstant)
                //	value = new TimeExtent(value.EndTime);
                //else if(TimeMode == Toolkit.TimeMode.CumulativeFromStart)
                //	value = new TimeExtent(MinimumValue, value.EndTime);				
                return value;
            }

        }

        private bool IsCurrentExtentTimeInstant => CurrentValidExtent?.IsTimeInstant() ?? false;
        private DateTimeOffset FullExtentStartTime => FullExtent != null ? FullExtent.StartTime : DateTimeOffset.MinValue;
        private DateTimeOffset FullExtentEndTime => FullExtent != null ? FullExtent.EndTime : DateTimeOffset.MaxValue;

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> value(s) associated with the 
        /// visual thumbs(s) displayed on the TimeSlider. 
        /// </summary>
		[TypeConverter(typeof(TimeExtentConverter))]
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
                new PropertyMetadata(OnCurrentExtentPropertyChanged));

        private static void OnCurrentExtentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeSlider obj = (TimeSlider)d;
            TimeExtent newValue = e.NewValue as TimeExtent;

            obj.currentValue = newValue;
            obj.UpdateTrackLayout(obj.CurrentValidExtent);
            obj.ValueChanged?.Invoke(obj, new ValueChangedEventArgs(newValue, e.OldValue as TimeExtent));

        }

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> that specifies of the overall start and end time of the TimeSlider
        /// </summary>
		[TypeConverter(typeof(TimeExtentConverter))]
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
                new PropertyMetadata(OnFullExtentPropertyChanged));

        private async static void OnFullExtentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeSlider obj = (TimeSlider)d;
            await obj.CalculateTimeSteps();
            obj.CreateTickmarks();
            obj.UpdateTrackLayout(obj.CurrentValidExtent);
        }

        /// <summary>
        /// Gets or sets the time intervals for the tickmarks.
        /// </summary>
        /// <value>The intervals.</value>
        public TimeValue TimeStepInterval
        {
            get { return (TimeValue)GetValue(TimeStepIntervalProperty); }
            set { SetValue(TimeStepIntervalProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepInterval"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeStepIntervalProperty =
            DependencyProperty.Register(nameof(TimeStepInterval), typeof(TimeValue), typeof(TimeSlider), new PropertyMetadata(OnTimeStepIntervalPropertyChanged));

        private static void OnTimeStepIntervalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimeSlider)d).CalculateTimeSteps();
        }

        ThrottleTimer _calculateTimeStepsThrottler;
        TaskCompletionSource<bool> _calculateTimeStepsTcs = new TaskCompletionSource<bool>();
        // Updates time steps based on the current TimeStepInterval and FullExtent
        private Task CalculateTimeSteps()
        {
            if (_calculateTimeStepsTcs == null || _calculateTimeStepsTcs.Task.IsCompleted || _calculateTimeStepsTcs.Task.IsFaulted)
                _calculateTimeStepsTcs = new TaskCompletionSource<bool>();

            if (_calculateTimeStepsThrottler == null)
            {
                _calculateTimeStepsThrottler = new ThrottleTimer(1, () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (TimeStepInterval == null || FullExtent == null)
                            return;

                        var timeStep = TimeStepInterval;
                        var startTime = FullExtent.StartTime;
                        var endTime = FullExtent.EndTime;

                        var steps = new List<DateTimeOffset> { startTime };
                        //var nStep = startTime.AddTimeValue(timeStep);
                        for (var nextStep = startTime.AddTimeValue(timeStep); nextStep <= endTime; nextStep = nextStep.AddTimeValue(timeStep))
                            steps.Add(nextStep);

                        TimeSteps = steps;
                        _calculateTimeStepsTcs.TrySetResult(true);
                    });
                });
            }
            _calculateTimeStepsThrottler.Invoke();
            return _calculateTimeStepsTcs.Task;
        }

        /// <summary>
        /// Gets or sets the time intervals for the tickmarks.
        /// </summary>
        /// <value>The intervals.</value>
        public IEnumerable<DateTimeOffset> TimeSteps
        {
            get { return (IEnumerable<DateTimeOffset>)GetValue(TimeStepsProperty); }
            private set { SetValue(TimeStepsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeSteps"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeStepsProperty =
            DependencyProperty.Register(nameof(TimeSteps), typeof(IEnumerable<DateTimeOffset>), typeof(TimeSlider), new PropertyMetadata(OnTimeStepsPropertyChanged));

        private static void OnTimeStepsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeSlider obj = (TimeSlider)d;
            if (obj.IsPlaying && (obj.TimeSteps == null || !obj.TimeSteps.GetEnumerator().MoveNext()))
                obj.IsPlaying = false;
            obj.CreateTickmarks();
            if (e.OldValue is ObservableCollection<DateTimeOffset>)
            {
                (e.OldValue as ObservableCollection<DateTimeOffset>).CollectionChanged -= obj.TimeSlider_CollectionChanged;
            }
            if (e.NewValue is ObservableCollection<DateTimeOffset>)
            {
                (e.NewValue as ObservableCollection<DateTimeOffset>).CollectionChanged += obj.TimeSlider_CollectionChanged;
            }
            obj.SetButtonVisibility();
            obj.UpdateTrackLayout(obj.CurrentValidExtent);
        }

        /// <summary>
        /// Gets or sets the how fast the thumb(s) moves across the Tick Marks of the TimeSlider.
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
            TimeSlider obj = (TimeSlider)d;
            TimeSpan newValue = (TimeSpan)e.NewValue;
            obj.playTimer.Interval = newValue;
        }

        /// <summary>
        /// Gets or sets the how fast the thumb(s) moves across the Tick Marks of the TimeSlider.
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
        /// Gets or sets a value indicating whether the animating of the TimeSlider thumb(s) will restart playing 
        /// when the end of the TickBar is reached.
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
            Visibility viz = (TimeSteps != null && TimeSteps.GetEnumerator().MoveNext()) ? Visibility.Visible : Visibility.Collapsed;

            //Play Button
            if (PlayPauseButton != null) PlayPauseButton.Visibility = viz;

            //Next Button
            if (NextButton != null) NextButton.Visibility = viz;

            //Previous Button
            if (PreviousButton != null) PreviousButton.Visibility = viz;
        }

        private void TimeSlider_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CreateTickmarks();
            UpdateTrackLayout(CurrentValidExtent);
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
            DependencyProperty.Register(nameof(IsStartTimePinned), typeof(bool), typeof(TimeSlider), new PropertyMetadata(OnIsStartTimePinnedChanged));

        private static void OnIsStartTimePinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            slider.MinimumThumb.IsEnabled = !(bool)e.NewValue;
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
            DependencyProperty.Register(nameof(IsEndTimePinned), typeof(bool), typeof(TimeSlider), new PropertyMetadata(OnIsEndTimePinnedChanged));

        private static void OnIsEndTimePinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            slider.MaximumThumb.IsEnabled = !(bool)e.NewValue;
            slider.HorizontalTrackThumb.IsEnabled = slider.MaximumThumb.IsEnabled && slider.MinimumThumb.IsEnabled;
        }

        /// <summary>
        /// Identifies the <see cref="IsPlaying"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(TimeSlider), new PropertyMetadata(false, OnIsPlayingPropertyChanged));

        private static void OnIsPlayingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeSlider obj = (TimeSlider)d;
            bool newValue = (bool)e.NewValue;
            if (newValue && obj.TimeSteps != null && obj.TimeSteps.GetEnumerator().MoveNext())
                obj.playTimer.Start();
            else
                obj.playTimer.Stop();
            if (obj.PlayPauseButton != null)
                obj.PlayPauseButton.IsChecked = newValue;
        }

        #region Appearance Properties

        /// <summary>
        /// Gets or sets the border color of the TimeSlider's thumbs
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
        /// Gets or sets the fill color of the TimeSlider's thumbs
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
        /// Gets or sets the fill color of the area on the TimeSlider's track that indicates the <see cref="CurrentExtent"/>
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
        /// Gets or sets the fill color of the area on the TimeSlider's track that indicates the <see cref="FullExtent"/>
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
        /// Gets or sets the border color of the area on the TimeSlider's track that indicates the <see cref="FullExtent"/>
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
        /// Gets or sets the color of the ticks on the TimeSlider that mark each time step interval
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
        /// Gets or sets the fill color of the area on the TimeSlider's track that indicates the <see cref="PlaybackButtons"/>
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
        /// Gets or sets the border color of the area on the TimeSlider's track that indicates the <see cref="PlaybackButtons"/>
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

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the selected time extent has changed.
        /// </summary>
        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// <see cref="RoutedEventArgs"/> used when raising the <see cref="ValueChanged"/> event.
        /// </summary>
        public sealed class ValueChangedEventArgs : RoutedEventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ValueChangedEventArgs"/> class.
            /// </summary>
            /// <param name="newValue">The new <see cref="TimeExtent"/> value.</param>
            /// <param name="oldValue">The old <see cref="TimeExtent"/> value.</param>
            internal ValueChangedEventArgs(TimeExtent newValue, TimeExtent oldValue)
            {
                NewValue = newValue;
                OldValue = oldValue;
            }
            /// <summary>
            /// Gets the new <see cref="TimeExtent"/> value.
            /// </summary>
            /// <value>The new value.</value>
            public TimeExtent NewValue { get; private set; }
            /// <summary>
            /// Gets the old <see cref="TimeExtent"/> value.
            /// </summary>
            /// <value>The old value.</value>
            public TimeExtent OldValue { get; private set; }
        }

        #endregion

        #region Static helper methods

        /// <summary>
        /// Creates the specified number of time stops evenly distributed in the time extent.
        /// </summary>
        /// <param name="extent">The time extent.</param>
        /// <param name="count">Number of stops.</param>
        /// <returns>IEnumerable of time stops.</returns>
        public static IEnumerable<DateTime> CreateTimeStopsByCount(TimeExtent extent, int count)
        {
            long span = (extent.EndTime - extent.StartTime).Ticks / (count - 1);
            DateTime d = extent.StartTime.DateTime;
            for (int i = 0; i < count - 1; i++)
            {
                yield return d;
                try { d = d.AddTicks(span); }
                catch (ArgumentOutOfRangeException) { }
            }
            yield return extent.EndTime.DateTime;
        }

        /// <summary>
        /// Creates time stops within an interval dispersed with by specified <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="extent">The time extent.</param>
        /// <param name="interval">Interval between each time stop.</param>
        /// <returns>IEnumerable of time stops.</returns>
        public static IEnumerable<DateTime> CreateTimeStopsByTimeInterval(TimeExtent extent, TimeSpan interval)
        {
            DateTime d = extent.StartTime.DateTime;
            while (d <= extent.EndTime)
            {
                yield return d;
                try { d = d.Add(interval); }
                catch (ArgumentOutOfRangeException) { }
            }
        }

        #endregion

        #region Play methods

        /// <summary>
        /// Moves the slider position forward by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the <see cref="TimeExtent.EndTime"/> of the slider's
        /// <see cref="FullExtent"/> was reached before the slider position was moved by the specified number of steps</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position</param>
        public bool StepForward(int timeSteps = 1)
        {
            if (timeSteps < 1)
                throw new ArgumentOutOfRangeException(nameof(timeSteps), $"{nameof(timeSteps)} must be greater than zero");

            return MoveTimeStep(timeSteps);
        }

        /// <summary>
        /// Moves the slider position back by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the <see cref="TimeExtent.StartTime"/> of the slider's
        /// <see cref="FullExtent"/> was reached before the slider position was moved by the specified number of steps</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position</param>
        public bool StepBack(int timeSteps = 1)
        {
            if (timeSteps < 1)
                throw new ArgumentOutOfRangeException(nameof(timeSteps), $"{nameof(timeSteps)} must be greater than zero");

            return MoveTimeStep(0 - timeSteps);
        }

        /// <summary>
        /// Moves the current start and end times by the specified number of time steps
        /// </summary>
        /// <param name="timeSteps">The number of time steps by which to move the current time.  A positive number will advance the time step forward, while
        /// a negative value will move the current time step backward</param>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the <see cref="TimeExtent.StartTime"/> of the slider's
        /// <see cref="FullExtent"/> was reached before the slider position was moved by the specified number of steps</returns>
        private bool MoveTimeStep(int timeSteps)
        {
            if (TimeSteps == null || CurrentValidExtent == null) return false;

            // TODO: Implement forward/back behavior when start or end time is pinned - what is expected behavior here?
            if (IsStartTimePinned || IsEndTimePinned) return false;

            // We want to rely on step indexes, so we use the known backing type here since that's most efficent.
            // If the backing type changes, the implemetation here will need to be updated accordingly
            var timeStepsList = (List<DateTimeOffset>)TimeSteps;

            // Get the current start and end time step indexes
            var startTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.StartTime);
            var endTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.EndTime);

            // Get the number of steps by which to move the current time.  If the number specified in the method call would move the current time extent
            // beyond the valid range, clamp the number of steps to the maximum number that extent can move in the specified direction.
            var validTimeStepDelta = 0;
            if (timeSteps > 0)
                validTimeStepDelta = endTimeStepIndex + timeSteps < timeStepsList.Count ? timeSteps : timeStepsList.Count - 1 - endTimeStepIndex;
            else
                validTimeStepDelta = startTimeStepIndex + timeSteps >= 0 ? timeSteps : 0 - startTimeStepIndex;

            // Get the new start and end times
            var newStartTimeStepIndex = startTimeStepIndex + validTimeStepDelta;
            var newEndTimeStepIndex = endTimeStepIndex + validTimeStepDelta;
            var newStartTime = timeStepsList[newStartTimeStepIndex];
            var newEndTime = timeStepsList[newEndTimeStepIndex];

            // Update the current time extent
            if (newStartTimeStepIndex == newEndTimeStepIndex)
                CurrentExtent = new TimeExtent(newStartTime);
            else
                CurrentExtent = new TimeExtent(newStartTime, newEndTime);

            // Return whether or not the time extent was moved by the specified number of steps
            return validTimeStepDelta == timeSteps;
        }

        /// <summary>
        /// Gets or set as Boolean indicating whether the TimeSlider is playing through the 
        /// <see cref="ESRI.ArcGIS.Client.Toolkit.TimeSlider.Intervals">Intervals</see> of the TickBar.
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
                new PropertyMetadata(OnFullExtentLabelFormatPropertyChanged));

        private static void OnFullExtentLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (TimeSlider)d;
            if (slider._fullExtentStartTimeTextBlock != null)
            {
                slider._fullExtentStartTimeTextBlock.SetBinding(TextBlock.TextProperty,
                    new Binding("FullExtent.StartTime")
                    {
                        Source = slider,
                        StringFormat = e.NewValue as string
                    });
            }

            if (slider._fullExtentEndTimeTextBlock != null)
            {
                slider._fullExtentEndTimeTextBlock.SetBinding(TextBlock.TextProperty,
                    new Binding("FullExtent.EndTime")
                    {
                        Source = slider,
                        StringFormat = e.NewValue as string
                    });
            }
        }

        private void playTimer_Tick(object sender, EventArgs e)
        {
            var isFinished = PlaybackDirection == PlaybackDirection.Forward ? !StepForward() : !StepBack();
            if (isFinished)
            {
                if (PlaybackLoopMode == LoopMode.None)
                {
                    IsPlaying = false;
                }
                else
                {
                    // We want to rely on step indexes, so we use the known backing type here since that's most efficent.
                    // If the backing type changes, the implemetation here will need to be updated accordingly
                    var timeStepsList = (List<DateTimeOffset>)TimeSteps;

                    // Get the current start and end time step indexes
                    var startTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.StartTime);
                    var endTimeStepIndex = timeStepsList.IndexOf(CurrentValidExtent.EndTime);

                    if (PlaybackLoopMode == LoopMode.Repeat)
                    {
                        if (PlaybackDirection == PlaybackDirection.Forward)
                        {
                            StepBack(startTimeStepIndex);
                        }
                        else
                        {
                            StepForward(timeStepsList.Count - (endTimeStepIndex - startTimeStepIndex));
                        }
                    }
                    else if (PlaybackLoopMode == LoopMode.Reverse)
                    {
                        if (PlaybackDirection == PlaybackDirection.Forward)
                        {
                            PlaybackDirection = PlaybackDirection.Backward;
                            StepBack();
                        }
                        else
                        {
                            PlaybackDirection = PlaybackDirection.Forward;
                            StepForward();
                        }
                    }
                }
            }
        }

        private void Rewind()
        {
            if (CurrentValidExtent == null) return;
            if (!IsStartTimePinned && !IsEndTimePinned)
            {
                IEnumerator<DateTimeOffset> enumerator = TimeSteps.GetEnumerator();
                // Get the first valid time step
                while (enumerator.MoveNext())
                {
                    if (IsCurrentExtentTimeInstant)
                        break;
                    if (enumerator.Current == CurrentValidExtent.StartTime)
                        break;
                }
                if (IsCurrentExtentTimeInstant)
                    CurrentExtent = new TimeExtent(enumerator.Current, enumerator.Current);
                else
                {
                    int i = 0;
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == CurrentValidExtent.EndTime)
                            break;
                        i++;
                    }
                    enumerator = TimeSteps.GetEnumerator();
                    enumerator.MoveNext();
                    DateTimeOffset start = enumerator.Current;
                    for (int j = 0; j <= i; j++) enumerator.MoveNext();
                    DateTimeOffset end = enumerator.Current;
                    CurrentExtent = new TimeExtent(start, end);
                }
            }
            else
            {
                // TODO: Implement rewind (i.e. returning to start) for cases where start/end time is pinned
            }
        }
        #endregion


        /// <summary>
        /// Gets or sets the time zone to use for the TimeSlider
        /// </summary>
		public TimeZoneInfo TimeZone
        {
            get { return (TimeZoneInfo)GetValue(TimeZoneProperty); }
            set { SetValue(TimeZoneProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeZone"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeZoneProperty =
            DependencyProperty.Register(nameof(TimeZone), typeof(TimeZoneInfo), typeof(TimeSlider), null);
    }
}
