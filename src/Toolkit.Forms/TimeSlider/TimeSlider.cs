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
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
#if NETFX_CORE
using Windows.UI.Xaml.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The Compass Control showing the heading on the map when the rotation is not North up / 0.
    /// </summary>
    public class TimeSlider : View
    {
        private bool _propertyChangedByFormsSlider = false;

#if NETFX_CORE
        private TimeSliderBindingProxy _bindingProxy = new TimeSliderBindingProxy();
#endif

        internal UI.Controls.TimeSlider NativeSlider { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class.
        /// </summary>
        public TimeSlider()
#if __ANDROID__
            : this(new UI.Controls.TimeSlider(Android.App.Application.Context))
#else
            : this(new UI.Controls.TimeSlider())
#endif
        {
        }

        internal TimeSlider(UI.Controls.TimeSlider nativeSlider)
        {
            NativeSlider = nativeSlider;

#if XAMARIN
            // On Xamarin platforms, listen to the native time slider's PropertyChanged event to synchronize changes
            // between the native slider and the Xamarin Forms wrapper
            NativeSlider.PropertyChanged += NativeSlider_PropertyChanged;
#elif NETFX_CORE
            // On UWP, use a binding proxy to listen for property changes in the native time slider.  This is needed
            // because the UWP time slider relies on DependencyProperties to surface property changes and does not implement
            // INotifyPropertyChanged.
            _bindingProxy.PropertyChanged += NativeSlider_PropertyChanged;
            SetProxyBinding(nameof(CurrentExtent), TimeSliderBindingProxy.CurrentExtentProperty);
            SetProxyBinding(nameof(FullExtent), TimeSliderBindingProxy.FullExtentProperty);
            SetProxyBinding(nameof(TimeStepInterval), TimeSliderBindingProxy.TimeStepIntervalProperty);
            SetProxyBinding(nameof(TimeSteps), TimeSliderBindingProxy.TimeStepsProperty);
            SetProxyBinding(nameof(PlaybackInterval), TimeSliderBindingProxy.PlaybackIntervalProperty);
            SetProxyBinding(nameof(PlaybackDirection), TimeSliderBindingProxy.PlaybackDirectionProperty);
            SetProxyBinding(nameof(PlaybackLoopMode), TimeSliderBindingProxy.LoopModeProperty);
            SetProxyBinding(nameof(IsStartTimePinned), TimeSliderBindingProxy.IsStartTimePinnedProperty);
            SetProxyBinding(nameof(IsEndTimePinned), TimeSliderBindingProxy.IsEndTimePinnedProperty);
            SetProxyBinding(nameof(IsPlaying), TimeSliderBindingProxy.IsPlayingProperty);
            SetProxyBinding(nameof(FullExtentLabelFormat), TimeSliderBindingProxy.FullExtentLabelFormatProperty);
            SetProxyBinding(nameof(CurrentExtentLabelFormat), TimeSliderBindingProxy.CurrentExtentLabelFormatProperty);
            SetProxyBinding(nameof(TimeStepIntervalLabelFormat), TimeSliderBindingProxy.TimeStepIntervalLabelFormatProperty);
            SetProxyBinding(nameof(LabelMode), TimeSliderBindingProxy.LabelModeProperty);
            SetProxyBinding(nameof(ThumbStroke), TimeSliderBindingProxy.ThumbStrokeProperty);
            SetProxyBinding(nameof(ThumbFill), TimeSliderBindingProxy.ThumbFillProperty);
            SetProxyBinding(nameof(CurrentExtentFill), TimeSliderBindingProxy.CurrentExtentFillProperty);
            SetProxyBinding(nameof(FullExtentFill), TimeSliderBindingProxy.FullExtentFillProperty);
            SetProxyBinding(nameof(FullExtentStroke), TimeSliderBindingProxy.FullExtentStrokeProperty);
            SetProxyBinding(nameof(TimeStepIntervalTickFill), TimeSliderBindingProxy.TimeStepIntervalTickFillProperty);
            SetProxyBinding(nameof(PlaybackButtonsFill), TimeSliderBindingProxy.PlaybackButtonsFillProperty);
            SetProxyBinding(nameof(PlaybackButtonsStroke), TimeSliderBindingProxy.PlaybackButtonsStrokeProperty);
            SetProxyBinding(nameof(FullExtentLabelColor), TimeSliderBindingProxy.FullExtentLabelColorProperty);
            SetProxyBinding(nameof(CurrentExtentLabelColor), TimeSliderBindingProxy.CurrentExtentLabelColorProperty);
            SetProxyBinding(nameof(TimeStepIntervalLabelColor), TimeSliderBindingProxy.TimeStepIntervalLabelColorProperty);
            nativeSlider.SizeChanged += (o, e) => InvalidateMeasure();
#endif

            NativeSlider.CurrentExtentChanged += NativeSlider_CurrentExtentChanged;
        }

#if NETFX_CORE
        private void SetProxyBinding(string sourcePropertyName, Windows.UI.Xaml.DependencyProperty targetProperty)
        {
            var b = new Windows.UI.Xaml.Data.Binding()
            {
                Path = new Windows.UI.Xaml.PropertyPath(sourcePropertyName),
                Source = NativeSlider,
                Mode = Windows.UI.Xaml.Data.BindingMode.TwoWay,
            };
            Windows.UI.Xaml.Data.BindingOperations.SetBinding(_bindingProxy, targetProperty, b);
        }
#endif

        private void NativeSlider_CurrentExtentChanged(object sender, TimeExtentChangedEventArgs e)
        {
            if (_propertyChangedByFormsSlider)
            {
                return;
            }

            CurrentExtent = e.NewExtent;
            OnCurrentExtentChanged(e);
        }

        // Handles property changes coming from the native time slider
        private void NativeSlider_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check whether the property change originated from the Xamarin Forms wrapper
            if (_propertyChangedByFormsSlider)
            {
                // This change started with the Xamarin Forms implementation, so the Xamarin Forms wrapper is already synced
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(FullExtent):
                    FullExtent = NativeSlider.FullExtent;
                    break;
                case nameof(TimeStepInterval):
                    TimeStepInterval = NativeSlider.TimeStepInterval;
                    break;
                case nameof(TimeSteps):
                    SetValue(TimeStepsProperty, NativeSlider.TimeSteps);
                    break;
                case nameof(PlaybackInterval):
                    PlaybackInterval = NativeSlider.PlaybackInterval;
                    break;
                case nameof(PlaybackDirection):
                    PlaybackDirection = NativeSlider.PlaybackDirection;
                    break;
                case nameof(PlaybackLoopMode):
                    PlaybackLoopMode = NativeSlider.PlaybackLoopMode;
                    break;
                case nameof(IsStartTimePinned):
                    IsStartTimePinned = NativeSlider.IsStartTimePinned;
                    break;
                case nameof(IsEndTimePinned):
                    IsEndTimePinned = NativeSlider.IsEndTimePinned;
                    break;
                case nameof(IsPlaying):
                    IsPlaying = NativeSlider.IsPlaying;
                    break;
                case nameof(FullExtentLabelFormat):
                    FullExtentLabelFormat = NativeSlider.FullExtentLabelFormat;
                    break;
                case nameof(CurrentExtentLabelFormat):
                    CurrentExtentLabelFormat = NativeSlider.CurrentExtentLabelFormat;
                    break;
                case nameof(TimeStepIntervalLabelFormat):
                    TimeStepIntervalLabelFormat = NativeSlider.TimeStepIntervalLabelFormat;
                    break;
                case nameof(LabelMode):
                    LabelMode = NativeSlider.LabelMode;
                    break;

                // Note that appearance properties need not be synced, as changes in these should never originate
                // at the native control level
            }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtent"/> bindable property.
        /// </summary>
        public static readonly BindableProperty CurrentExtentProperty =
            BindableProperty.Create(nameof(CurrentExtent), typeof(TimeExtent), typeof(TimeSlider), null, BindingMode.OneWay, null, OnCurrentExtentPropertyChanged);

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> associated with the visual thumbs(s) displayed on the TimeSlider.
        /// </summary>
        public TimeExtent? CurrentExtent
        {
            get => GetValue(CurrentExtentProperty) as TimeExtent;
            set => SetValue(CurrentExtentProperty, value);
        }

        private static void OnCurrentExtentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;

                if (!newValue.Equals(slider.NativeSlider.CurrentExtent))
                {
                    slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                    slider.NativeSlider.CurrentExtent = newValue as TimeExtent;
#elif NETFX_CORE
                    slider._bindingProxy.SetValue(TimeSliderBindingProxy.CurrentExtentProperty, newValue);
#endif
                    slider._propertyChangedByFormsSlider = false;
                    slider.InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtent"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FullExtentProperty =
            BindableProperty.Create(nameof(FullExtent), typeof(TimeExtent), typeof(TimeSlider), null, BindingMode.OneWay, null, OnFullExtentPropertyChanged);

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> associated with the visual thumbs(s) displayed on the TimeSlider.
        /// </summary>
        public TimeExtent? FullExtent
        {
            get => GetValue(FullExtentProperty) as TimeExtent;
            set => SetValue(FullExtentProperty, value);
        }

        private static void OnFullExtentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.FullExtent = newValue as TimeExtent;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.FullExtentFillProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepInterval"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TimeStepIntervalProperty =
            BindableProperty.Create(nameof(TimeStepInterval), typeof(TimeValue), typeof(TimeSlider), null, BindingMode.OneWay, null, OnTimeStepIntervalPropertyChanged);

        /// <summary>
        /// Gets or sets the <see cref="TimeValue" /> associated with the visual thumbs(s) displayed on the TimeSlider.
        /// </summary>
        public TimeValue? TimeStepInterval
        {
            get => GetValue(TimeStepIntervalProperty) as TimeValue;
            set => SetValue(TimeStepIntervalProperty, value);
        }

        private static void OnTimeStepIntervalPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.TimeStepInterval = newValue as TimeValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.TimeStepIntervalProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeSteps"/> bindable property.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)] // TimeSteps are not meant to be set explicitly
        public static readonly BindableProperty TimeStepsProperty =
            BindableProperty.Create(nameof(TimeSteps), typeof(IReadOnlyList<DateTimeOffset>), typeof(TimeSlider), null, BindingMode.OneWay, null, OnTimeStepsPropertyChanged);

        /// <summary>
        /// Gets the time steps that can be used to set the slider instance's current extent.
        /// </summary>
        public IReadOnlyList<DateTimeOffset>? TimeSteps
        {
            get => NativeSlider.TimeSteps;
        }

        private static void OnTimeStepsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // No-op on Xamarin platforms, as time steps are strictly read-only
#if NETFX_CORE
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
                slider.NativeSlider.SetValue(UI.Controls.TimeSlider.TimeStepsProperty, newValue);
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
#endif
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackInterval"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PlaybackIntervalProperty =
            BindableProperty.Create(nameof(PlaybackInterval), typeof(TimeSpan), typeof(TimeSlider), null, BindingMode.OneWay, null, OnPlaybackIntervalPropertyChanged);

        /// <summary>
        /// Gets or sets the interval at which the time slider's current extent will move to the next or previous time step.
        /// </summary>
        public TimeSpan PlaybackInterval
        {
            get => (TimeSpan)GetValue(PlaybackIntervalProperty);
            set => SetValue(PlaybackIntervalProperty, value);
        }

        private static void OnPlaybackIntervalPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.PlaybackInterval = (TimeSpan)newValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.PlaybackIntervalProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackDirection"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PlaybackDirectionProperty =
            BindableProperty.Create(nameof(PlaybackDirection), typeof(PlaybackDirection), typeof(TimeSlider), null, BindingMode.OneWay, null, OnPlaybackDirectionPropertyChanged);

        /// <summary>
        /// Gets or sets whether the current extent will move to the next or the previous time step during playback.
        /// </summary>
        public PlaybackDirection PlaybackDirection
        {
            get => (PlaybackDirection)GetValue(PlaybackDirectionProperty);
            set => SetValue(PlaybackDirectionProperty, value);
        }

        private static void OnPlaybackDirectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.PlaybackDirection = (PlaybackDirection)newValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.PlaybackDirectionProperty, (PlaybackDirection)newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackLoopMode"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PlaybackLoopModeProperty =
            BindableProperty.Create(nameof(PlaybackLoopMode), typeof(LoopMode), typeof(TimeSlider), null, BindingMode.OneWay, null, OnPlaybackLoopModePropertyChanged);

        /// <summary>
        /// Gets or sets the behavior when the current extent reaches the end of the slider during playback.
        /// </summary>
        public LoopMode PlaybackLoopMode
        {
            get => (LoopMode)GetValue(PlaybackLoopModeProperty);
            set => SetValue(PlaybackLoopModeProperty, value);
        }

        private static void OnPlaybackLoopModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.PlaybackLoopMode = (LoopMode)newValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.LoopModeProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsStartTimePinned"/> bindable property.
        /// </summary>
        public static readonly BindableProperty IsStartTimePinnedProperty =
            BindableProperty.Create(nameof(IsStartTimePinned), typeof(bool), typeof(TimeSlider), null, BindingMode.OneWay, null, OnIsStartTimePinnedPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the start time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        public bool IsStartTimePinned
        {
            get => (bool)GetValue(IsStartTimePinnedProperty);
            set => SetValue(IsStartTimePinnedProperty, value);
        }

        private static void OnIsStartTimePinnedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.IsStartTimePinned = (bool)newValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.IsStartTimePinnedProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsEndTimePinned"/> bindable property.
        /// </summary>
        public static readonly BindableProperty IsEndTimePinnedProperty =
            BindableProperty.Create(nameof(IsEndTimePinned), typeof(bool), typeof(TimeSlider), null, BindingMode.OneWay, null, OnIsEndTimePinnedPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the end time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        public bool IsEndTimePinned
        {
            get => (bool)GetValue(IsEndTimePinnedProperty);
            set => SetValue(IsEndTimePinnedProperty, value);
        }

        private static void OnIsEndTimePinnedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.IsEndTimePinned = (bool)newValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.IsEndTimePinnedProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsPlaying"/> bindable property.
        /// </summary>
        public static readonly BindableProperty IsPlayingProperty =
            BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(TimeSlider), null, BindingMode.OneWay, null, OnIsPlayingPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the time slider is animating playback.
        /// </summary>
        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }

        private static void OnIsPlayingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.IsPlaying = (bool)newValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.IsPlayingProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

#region Appearance Properties

        /// <summary>
        /// Identifies the <see cref="FullExtentLabelFormat"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FullExtentLabelFormatProperty =
            BindableProperty.Create(nameof(FullExtentLabelFormat), typeof(string), typeof(TimeSlider), null, BindingMode.OneWay, null, OnFullExtentLabelFormatPropertyChanged);

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="FullExtent"/>.
        /// </summary>
        public string? FullExtentLabelFormat
        {
            get => GetValue(FullExtentLabelFormatProperty) as string;
            set => SetValue(FullExtentLabelFormatProperty, value);
        }

        private static void OnFullExtentLabelFormatPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.FullExtentLabelFormat = newValue as string;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.FullExtentLabelFormatProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtentLabelFormat"/> bindable property.
        /// </summary>
        public static readonly BindableProperty CurrentExtentLabelFormatProperty =
            BindableProperty.Create(nameof(CurrentExtentLabelFormat), typeof(string), typeof(TimeSlider), null, BindingMode.OneWay, null, OnCurrentExtentLabelFormatPropertyChanged);

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="CurrentExtent"/>.
        /// </summary>
        public string? CurrentExtentLabelFormat
        {
            get => GetValue(CurrentExtentLabelFormatProperty) as string;
            set => SetValue(CurrentExtentLabelFormatProperty, value);
        }

        private static void OnCurrentExtentLabelFormatPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.CurrentExtentLabelFormat = newValue as string;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.CurrentExtentLabelFormatProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepIntervalLabelFormat"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TimeStepIntervalLabelFormatProperty =
            BindableProperty.Create(nameof(TimeStepIntervalLabelFormat), typeof(string), typeof(TimeSlider), null, BindingMode.OneWay, null, OnTimeStepIntervalLabelFormatPropertyChanged);

        /// <summary>
        /// Gets or sets the string format to use for displaying the labels for the tick marks representing each time step interval.
        /// </summary>
        public string? TimeStepIntervalLabelFormat
        {
            get => GetValue(TimeStepIntervalLabelFormatProperty) as string;
            set => SetValue(TimeStepIntervalLabelFormatProperty, value);
        }

        private static void OnTimeStepIntervalLabelFormatPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.TimeStepIntervalLabelFormat = newValue as string;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.TimeStepIntervalLabelFormatProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="LabelMode"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LabelModeProperty =
            BindableProperty.Create(nameof(LabelMode), typeof(TimeSliderLabelMode), typeof(TimeSlider), null, BindingMode.OneWay, null, OnLabelModePropertyChanged);

        /// <summary>
        /// Gets or sets the mode to use for labels along the time slider.
        /// </summary>
        public TimeSliderLabelMode LabelMode
        {
            get => (TimeSliderLabelMode)GetValue(LabelModeProperty);
            set => SetValue(LabelModeProperty, value);
        }

        private static void OnLabelModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
                slider._propertyChangedByFormsSlider = true;
#if XAMARIN
                slider.NativeSlider.LabelMode = (TimeSliderLabelMode)newValue;
#elif NETFX_CORE
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.LabelModeProperty, newValue);
#endif
                slider._propertyChangedByFormsSlider = false;
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="ThumbStroke"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ThumbStrokeProperty =
            BindableProperty.Create(nameof(ThumbStroke), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnThumbStrokePropertyChanged);

        /// <summary>
        /// Gets or sets the border color of the thumbs.
        /// </summary>
        public Color ThumbStroke
        {
            get => (Color)GetValue(ThumbStrokeProperty);
            set => SetValue(ThumbStrokeProperty, value);
        }

        private static void OnThumbStrokePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.ThumbStrokeProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.ThumbStroke = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="ThumbFill"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ThumbFillProperty =
            BindableProperty.Create(nameof(ThumbFill), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnThumbFillPropertyChanged);

        /// <summary>
        /// Gets or sets the fill color of the thumbs.
        /// </summary>
        public Color ThumbFill
        {
            get => (Color)GetValue(ThumbFillProperty);
            set => SetValue(ThumbFillProperty, value);
        }

        private static void OnThumbFillPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.ThumbFillProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.ThumbFill = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtentFill"/> bindable property.
        /// </summary>
        public static readonly BindableProperty CurrentExtentFillProperty =
            BindableProperty.Create(nameof(CurrentExtentFill), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnCurrentExtentFillPropertyChanged);

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="CurrentExtent"/>.
        /// </summary>
        public Color CurrentExtentFill
        {
            get => (Color)GetValue(CurrentExtentFillProperty);
            set => SetValue(CurrentExtentFillProperty, value);
        }

        private static void OnCurrentExtentFillPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.CurrentExtentFillProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.CurrentExtentFill = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtentFill"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FullExtentFillProperty =
            BindableProperty.Create(nameof(FullExtentFill), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnFullExtentFillPropertyChanged);

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        public Color FullExtentFill
        {
            get => (Color)GetValue(FullExtentFillProperty);
            set => SetValue(FullExtentFillProperty, value);
        }

        private static void OnFullExtentFillPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.FullExtentFillProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.FullExtentFill = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtentStroke"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FullExtentStrokeProperty =
            BindableProperty.Create(nameof(FullExtentStroke), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnFullExtentStrokePropertyChanged);

        /// <summary>
        /// Gets or sets the border color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        public Color FullExtentStroke
        {
            get => (Color)GetValue(FullExtentStrokeProperty);
            set => SetValue(FullExtentStrokeProperty, value);
        }

        private static void OnFullExtentStrokePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.FullExtentStrokeProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.FullExtentStroke = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepIntervalTickFill"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TimeStepIntervalTickFillProperty =
            BindableProperty.Create(nameof(TimeStepIntervalTickFill), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnTimeStepIntervalTickFillPropertyChanged);

        /// <summary>
        /// Gets or sets the color of the slider's tickmarks.
        /// </summary>
        public Color TimeStepIntervalTickFill
        {
            get => (Color)GetValue(TimeStepIntervalTickFillProperty);
            set => SetValue(TimeStepIntervalTickFillProperty, value);
        }

        private static void OnTimeStepIntervalTickFillPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.TimeStepIntervalTickFillProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.TimeStepIntervalTickFill = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackButtonsFill"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PlaybackButtonsFillProperty =
            BindableProperty.Create(nameof(PlaybackButtonsFill), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnPlaybackButtonsFillPropertyChanged);

        /// <summary>
        /// Gets or sets the fill color of the playback buttons.
        /// </summary>
        public Color PlaybackButtonsFill
        {
            get => (Color)GetValue(PlaybackButtonsFillProperty);
            set => SetValue(PlaybackButtonsFillProperty, value);
        }

        private static void OnPlaybackButtonsFillPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.PlaybackButtonsFillProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.PlaybackButtonsFill = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackButtonsStroke"/> bindable property.
        /// </summary>
        public static readonly BindableProperty PlaybackButtonsStrokeProperty =
            BindableProperty.Create(nameof(PlaybackButtonsStroke), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnPlaybackButtonsStrokePropertyChanged);

        /// <summary>
        /// Gets or sets the border color of the playback buttons.
        /// </summary>
        public Color PlaybackButtonsStroke
        {
            get => (Color)GetValue(PlaybackButtonsStrokeProperty);
            set => SetValue(PlaybackButtonsStrokeProperty, value);
        }

        private static void OnPlaybackButtonsStrokePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.PlaybackButtonsStrokeProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.PlaybackButtonsStroke = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="FullExtentLabelColor"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FullExtentLabelColorProperty =
            BindableProperty.Create(nameof(FullExtentLabelColor), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnFullExtentLabelColorPropertyChanged);

        /// <summary>
        /// Gets or sets the color of the full extent labels.
        /// </summary>
        public Color FullExtentLabelColor
        {
            get => (Color)GetValue(FullExtentLabelColorProperty);
            set => SetValue(FullExtentLabelColorProperty, value);
        }

        private static void OnFullExtentLabelColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.FullExtentLabelColorProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.FullExtentLabelColor = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtentLabelColor"/> bindable property.
        /// </summary>
        public static readonly BindableProperty CurrentExtentLabelColorProperty =
            BindableProperty.Create(nameof(CurrentExtentLabelColor), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnCurrentExtentLabelColorPropertyChanged);

        /// <summary>
        /// Gets or sets the color of the current extent labels.
        /// </summary>
        public Color CurrentExtentLabelColor
        {
            get => (Color)GetValue(CurrentExtentLabelColorProperty);
            set => SetValue(CurrentExtentLabelColorProperty, value);
        }

        private static void OnCurrentExtentLabelColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.CurrentExtentLabelColorProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.CurrentExtentLabelColor = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepIntervalLabelColor"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TimeStepIntervalLabelColorProperty =
            BindableProperty.Create(nameof(TimeStepIntervalLabelColor), typeof(Color), typeof(TimeSlider), null, BindingMode.OneWay, null, OnTimeStepIntervalLabelColorPropertyChanged);

        /// <summary>
        /// Gets or sets the color of the time step interval labels.
        /// </summary>
        public Color TimeStepIntervalLabelColor
        {
            get => (Color)GetValue(TimeStepIntervalLabelColorProperty);
            set => SetValue(TimeStepIntervalLabelColorProperty, value);
        }

        private static void OnTimeStepIntervalLabelColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var slider = (TimeSlider)bindable;
#if NETFX_CORE
                var nativeColor = new Windows.UI.Xaml.Media.SolidColorBrush(((Color)newValue).ToNativeColor());
                slider._bindingProxy.SetValue(TimeSliderBindingProxy.TimeStepIntervalLabelColorProperty, nativeColor);
#else
                var nativeColor = ((Color)newValue).ToNativeColor();
                slider.NativeSlider.TimeStepIntervalLabelColor = nativeColor;
#endif
                slider.InvalidateMeasure();
            }
        }

#endregion // Appearance Properties

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
        public void InitializeTimeSteps(int count) => NativeSlider.InitializeTimeSteps(count);

        /// <summary>
        /// Initializes the time slider's temporal properties based on the specified GeoView. Specifically,
        /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>.
        /// </summary>
        /// <param name="geoView">The GeoView to use to initialize the time-slider's properties.</param>
        /// <returns>Task.</returns>
        public Task InitializeTimePropertiesAsync(GeoView geoView)
        {
            if (geoView is null)
            {
                throw new ArgumentNullException(nameof(geoView));
            }
#if NETSTANDARD2_0
            throw new NotImplementedException();
#else
            return NativeSlider.InitializeTimePropertiesAsync(geoView.GetNativeGeoView());
#endif
        }

        /// <summary>
        /// Initializes the time slider's temporal properties based on the specified time-aware layer. Specifically,
        /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>.
        /// </summary>
        /// <param name="timeAwareLayer">The layer to use to initialize the time slider.</param>
        /// <returns>Task.</returns>
        public Task InitializeTimePropertiesAsync(ITimeAware timeAwareLayer) => NativeSlider.InitializeTimePropertiesAsync(timeAwareLayer);

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
        public bool StepForward(int timeSteps = 1) => NativeSlider.StepForward(timeSteps);

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
        public bool StepBack(int timeSteps = 1) => NativeSlider.StepBack(timeSteps);

#endregion

        /// <summary>
        /// Occurs when the selected time extent has changed.
        /// </summary>
        public event EventHandler<TimeExtentChangedEventArgs>? CurrentExtentChanged;

        private void OnCurrentExtentChanged(TimeExtentChangedEventArgs e)
        {
            CurrentExtentChanged?.Invoke(this, e);
        }
    }
}