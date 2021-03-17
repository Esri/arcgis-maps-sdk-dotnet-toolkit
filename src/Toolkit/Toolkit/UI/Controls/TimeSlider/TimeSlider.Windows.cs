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

#if !XAMARIN

using System;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Key = Windows.System.VirtualKey;
#else
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "SliderTrack", Type = typeof(FrameworkElement))]
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
    public partial class TimeSlider : Control
    {
#pragma warning disable SX1309 // Names match elements in template
#pragma warning disable SA1306 // Names match elements in template
        private FrameworkElement SliderTrack;
        private Thumb MinimumThumb;
        private Thumb MaximumThumb;
        private Thumb HorizontalTrackThumb;
        private ButtonBase NextButton;
        private ButtonBase PreviousButton;
        private ToggleButton PlayPauseButton;
        private RepeatButton SliderTrackStepBackRepeater;
        private RepeatButton SliderTrackStepForwardRepeater;
#pragma warning restore SX1309
#pragma warning restore SA1306
        private string _originalFullExtentLabelFormat;
        private string _originalCurrentExtentLabelFormat;
        private bool _isFocused;
        private bool _isMouseOver;

        private void InitializeImpl()
        {
            DefaultStyleKey = typeof(TimeSlider);
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
            {
                SliderTrack.SizeChanged += TimeSlider_SizeChanged;
            }

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
                MinimumThumb.DragCompleted += (s, e) => OnDragCompleted();
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
                    OnMaximumThumbDrag(translateX);
                };
#else
                MaximumThumb.DragDelta += (s, e) => OnMaximumThumbDrag(e.HorizontalChange);
#endif
                MaximumThumb.DragCompleted += (s, e) => OnDragCompleted();
                MaximumThumb.DragStarted += (s, e) => SetFocus();
            }

            if (HorizontalTrackThumb != null)
            {
#if NETFX_CORE
                HorizontalTrackThumb.ManipulationMode = ManipulationModes.TranslateX;
                HorizontalTrackThumb.ManipulationDelta += (s, e) =>
                {
                    // Position is reported relative to the left edge of the thumb.  Adjust it so it is relative to the thumb's center.
                    var translateX = e.Position.X - (HorizontalTrackThumb.ActualWidth / 2);
                    OnCurrentExtentThumbDrag(translateX);
                };
#else
                HorizontalTrackThumb.DragDelta += (s, e) => OnCurrentExtentThumbDrag(e.HorizontalChange);
#endif
                HorizontalTrackThumb.DragCompleted += (s, e) => OnDragCompleted();
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
                NextButton.Click += (s, e) => OnNextButtonClick();
            }

            if (PreviousButton != null)
            {
                PreviousButton.Click += (s, e) => OnPreviousButtonClick();
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
            if ((MinimumThumb != null && !MinimumThumb.IsDragging) ||
                (MaximumThumb != null && !MaximumThumb.IsDragging) ||
                (MinimumThumb == null && MaximumThumb == null))
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
            if ((MinimumThumb != null && !MinimumThumb.IsDragging) ||
                (MaximumThumb != null && !MaximumThumb.IsDragging) ||
                (MinimumThumb == null && MaximumThumb == null))
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
                if (e.Key == Key.Left)
                {
                    StepBack();
                }
                else if (e.Key == Key.Right)
                {
                    StepForward();
                }
            }
        }

#endregion // Overrides

        private void ChangeVisualState(bool useTransitions)
        {
            // CommonStates
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

            // FocusStates
            if (_isFocused && IsEnabled)
            {
                VisualStateManager.GoToState(this, "Focused", useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, "Unfocused", useTransitions);
            }
        }

#region Properties

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> associated with the visual thumbs(s) displayed on the TimeSlider.
        /// </summary>
#if !NETFX_CORE
        [TypeConverter(typeof(TimeExtentConverter))]
#endif
        private TimeExtent CurrentExtentImpl
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

        private static void OnCurrentExtentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TimeSlider)d).OnCurrentExtentPropertyChanged(e.NewValue as TimeExtent, e.OldValue as TimeExtent);

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> that specifies the overall start and end time of the time slider instance.
        /// </summary>
#if !NETFX_CORE
        [TypeConverter(typeof(TimeExtentConverter))]
#endif
        private TimeExtent FullExtentImpl
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

        private static void OnFullExtentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimeSlider)d).OnFullExtentPropertyChanged();

        /// <summary>
        /// Gets or sets the time step intervals for the time slider.  The slider thumbs will snap to and tick marks will be shown at this interval.
        /// </summary>
        private TimeValue TimeStepIntervalImpl
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

        private static void OnTimeStepIntervalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimeSlider)d).OnTimeStepIntervalPropertyChanged();

        /// <summary>
        /// Gets or sets gets the time steps that can be used to set the slider instance's current extent.
        /// </summary>
        private IReadOnlyList<DateTimeOffset> TimeStepsImpl
        {
            get { return (IReadOnlyList<DateTimeOffset>)GetValue(TimeStepsProperty); }
            set { SetValue(TimeStepsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TimeSteps"/> dependency property.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly DependencyProperty TimeStepsProperty =
            DependencyProperty.Register(nameof(TimeSteps), typeof(IReadOnlyList<DateTimeOffset>), typeof(TimeSlider),
                new PropertyMetadata(default(IReadOnlyList<DateTimeOffset>), OnTimeStepsPropertyChanged));

        private static void OnTimeStepsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimeSlider)d).OnTimeStepsPropertyChanged();

        /// <summary>
        /// Gets or sets the interval at which the time slider's current extent will move to the next or previous time step.
        /// </summary>
        private TimeSpan PlaybackIntervalImpl
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

        private static void OnPlaybackIntervalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TimeSlider)d).OnPlaybackIntervalPropertyChanged((TimeSpan)e.NewValue);

        /// <summary>
        /// Gets or sets whether the current extent will move to the next or the previous time step during playback.
        /// </summary>
        private PlaybackDirection PlaybackDirectionImpl
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
        /// Gets or sets the behavior when the current extent reaches the end of the slider during playback.
        /// </summary>
        private LoopMode PlaybackLoopModeImpl
        {
            get { return (LoopMode)GetValue(LoopModeProperty); }
            set { SetValue(LoopModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackLoopMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LoopModeProperty =
            DependencyProperty.Register(nameof(PlaybackLoopMode), typeof(LoopMode), typeof(TimeSlider), new PropertyMetadata(LoopMode.None));

        /// <summary>
        /// Gets or sets a value indicating whether the start time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        private bool IsStartTimePinnedImpl
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

        private static void OnIsStartTimePinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TimeSlider)d).OnIsStartTimePinnedChanged((bool)e.NewValue);

        /// <summary>
        /// Gets or sets a value indicating whether the end time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        private bool IsEndTimePinnedImpl
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

        private static void OnIsEndTimePinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TimeSlider)d).OnIsEndTimePinnedChanged((bool)e.NewValue);

        /// <summary>
        /// Gets or sets a value indicating whether the time slider is animating playback.
        /// </summary>
        private bool IsPlayingImpl
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsPlaying"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(TimeSlider), new PropertyMetadata(false, OnIsPlayingPropertyChanged));

        private static void OnIsPlayingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TimeSlider)d).OnIsPlayingPropertyChanged((bool)e.NewValue);

#region Appearance Properties

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="FullExtent"/>.
        /// </summary>
        private string FullExtentLabelFormatImpl
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
            var labelFormat = e.NewValue as string;
            var slider = (TimeSlider)d;

            // Apply the updated string format to the full extent label elements' bindings
            slider.FullExtentStartTimeLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: labelFormat,
                fallbackFormat: ref slider._originalFullExtentLabelFormat);
            slider.FullExtentEndTimeLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: labelFormat,
                fallbackFormat: ref slider._originalFullExtentLabelFormat);
        }

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="CurrentExtent"/>.
        /// </summary>
        private string CurrentExtentLabelFormatImpl
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
            var labelFormat = e.NewValue as string;
            var slider = (TimeSlider)d;

            // Apply the updated string format to the current extent label elements' bindings
            slider.MinimumThumbLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: labelFormat,
                fallbackFormat: ref slider._originalCurrentExtentLabelFormat);
            slider.MaximumThumbLabel?.UpdateStringFormat(
                targetProperty: TextBlock.TextProperty,
                stringFormat: labelFormat,
                fallbackFormat: ref slider._originalCurrentExtentLabelFormat);
            slider.OnCurrentExtentLabelFormatPropertyChanged(labelFormat);
        }

        /// <summary>
        /// Gets or sets the string format to use for displaying the labels for the tick marks representing each time step interval.
        /// </summary>
        private string TimeStepIntervalLabelFormatImpl
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

        private static void OnTimeStepIntervalLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TimeSlider)d).OnTimeStepIntervalLabelFormatPropertyChanged(e.NewValue as string);

        /// <summary>
        /// Gets or sets the mode to use for labels along the time slider.
        /// </summary>
        private TimeSliderLabelMode LabelModeImpl
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

        private static void OnLabelModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TimeSlider)d).OnLabelModePropertyChanged((TimeSliderLabelMode)e.NewValue);

        /// <summary>
        /// Gets or sets the border color of the thumbs.
        /// </summary>
        private Brush ThumbStrokeImpl
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
        /// Gets or sets the fill color of the thumbs.
        /// </summary>
        private Brush ThumbFillImpl
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
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="CurrentExtent"/>.
        /// </summary>
        private Brush CurrentExtentFillImpl
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
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        private Brush FullExtentFillImpl
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
        /// Gets or sets the border color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        private Brush FullExtentStrokeImpl
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
        /// Gets or sets the color of the slider's tickmarks.
        /// </summary>
        private Brush TimeStepIntervalTickFillImpl
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
        /// Gets or sets the fill color of the playback buttons.
        /// </summary>
        private Brush PlaybackButtonsFillImpl
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
        /// Gets or sets the border color of the playback buttons.
        /// </summary>
        private Brush PlaybackButtonsStrokeImpl
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
        /// Gets or sets the color of the full extent labels.
        /// </summary>
        private Brush FullExtentLabelColorImpl
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
        /// Gets or sets the color of the current extent labels.
        /// </summary>
        private Brush CurrentExtentLabelColorImpl
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
        /// Gets or sets the color of the time step interval labels.
        /// </summary>
        private Brush TimeStepIntervalLabelColorImpl
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
    }
}

#endif