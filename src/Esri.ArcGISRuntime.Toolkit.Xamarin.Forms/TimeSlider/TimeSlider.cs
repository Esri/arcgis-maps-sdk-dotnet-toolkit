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

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The TimeSlider is a utility Control that emits TimeExtent values typically for use with the Map Control
    /// to enhance the viewing of geographic features that have attributes based upon Date/Time information.
    /// </summary>
    public class TimeSlider : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlider"/> class
        /// </summary>
        public TimeSlider()
        {
        }

        /// <summary>
        /// Identifies the <see cref="CurrentExtent"/> bindable property.
        /// </summary>
        public static readonly BindableProperty CurrentExtentProperty = BindableProperty.Create(nameof(CurrentExtent), typeof(TimeExtent), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> associated with the visual thumbs(s) displayed on the TimeSlider.
        /// </summary>
        public TimeExtent CurrentExtent
        {
            get => (TimeExtent)GetValue(CurrentExtentProperty);
            set => SetValue(CurrentExtentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FullExtent"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FullExtentProperty = BindableProperty.Create(nameof(FullExtent), typeof(TimeExtent), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> that specifies the overall start and end time of the time slider instance
        /// </summary>
        public TimeExtent FullExtent
        {
            get => (TimeExtent)GetValue(FullExtentProperty);
            set => SetValue(FullExtentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TimeStepInterval"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TimeStepIntervalProperty = BindableProperty.Create(nameof(TimeStepInterval), typeof(TimeValue), typeof(TimeSlider), null);

        /// <summary>
        /// Gets or sets the time step intervals for the time slider.  The slider thumbs will snap to and tick marks will be shown at this interval.
        /// </summary>
        public TimeValue TimeStepInterval
        {
            get => (TimeValue)GetValue(TimeStepIntervalProperty);
            set => SetValue(TimeStepIntervalProperty, value);
        }

        /*
        public IReadOnlyList<DateTimeOffset> TimeSteps { get; set; }
        public TimeSpan PlaybackInterval { get; set; }
        public PlaybackDirection PlaybackDirection { get; set; }
        public LoopMode PlaybackLoopMode { get; set; }
        public bool IsStartTimePinned { get; set; }
        public bool IsEndTimePinned { get; set; }
        public bool IsPlaying { get; set; }
        public string FullExtentLabelFormat { get; set; }
        public string CurrentExtentLabelFormat { get; set; }
        public string TimeStepIntervalLabelFormat { get; set; }
        public TimeSliderLabelMode LabelMode { get; set; }
        public Color ThumbStroke { get; set; }
        public Color ThumbFill { get; set; }
        public Color CurrentExtentFill { get; set; }
        public Color FullExtentFill { get; set; }
        public Color FullExtentStroke { get; set; }
        public Color TimeStepIntervalTickFill { get; set; }
        public Color PlaybackButtonsFill { get; set; }
        public Color PlaybackButtonsStroke { get; set; }
        public Color FullExtentLabelColor { get; set; }
        public Color CurrentExtentLabelColor { get; set; }
        public Color TimeStepIntervalLabelColor { get; set; }
        */

        /// <summary>
        /// Updates the time slider to have the specified number of time steps
        /// </summary>
        /// <param name="count">The number of time steps</param>
        public void InitializeTimeSteps(int count) => Renderer.InitializeTimeSteps(count);

        /// <summary>
        /// Moves the slider position forward by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the position could not be moved as requested</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position</param>
        public bool StepForward(int timeSteps = 1) => Renderer.StepForward(timeSteps);

        /// <summary>
        /// Moves the slider position back by the specified number of time steps.
        /// </summary>
        /// <returns><c>true</c> if succeeded, <c>false</c> if the position could not be moved as requested</returns>
        /// <param name="timeSteps">The number of steps to advance the slider's position</param>
        public bool StepBack(int timeSteps = 1) => Renderer.StepBack(timeSteps);

        /// <summary>
        /// Initializes the time slider's temporal properties based on the specified GeoView. Specifically,
        /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>
        /// </summary>
        /// <param name="geoView">The GeoView to use to initialize the time-slider's properties</param>
        /// <returns>Task</returns>
        public Task InitializeTimePropertiesAsync(GeoView geoView)
        {
#if NETSTANDARD2_0
            throw new NotSupportedException();
#else
            return Renderer.InitializeTimePropertiesAsync(geoView.GetNativeGeoView());
#endif
        }

        /// <summary>
        /// Initializes the time slider's temporal properties based on the specified time-aware layer. Specifically,
        /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>.
        /// </summary>
        /// <param name="timeAwareLayer">The layer to use to initialize the time slider</param>
        /// <returns>Task</returns>
        public Task InitializeTimePropertiesAsync(ITimeAware timeAwareLayer) => Renderer.InitializeTimePropertiesAsync(timeAwareLayer);

        private TimeSliderRenderer Renderer
        {
            get
            {
#if NETSTANDARD2_0
                throw new NotSupportedException("Platform not supported");
#else
#if __IOS__
                var renderer = global::Xamarin.Forms.Platform.iOS.Platform.GetRenderer(this) as TimeSliderRenderer;
#elif __ANDROID__
                var renderer = global::Xamarin.Forms.Platform.Android.Platform.GetRenderer(this) as TimeSliderRenderer;
#elif NETFX_CORE
                var renderer = global::Xamarin.Forms.Platform.UWP.Platform.GetRenderer(this) as TimeSliderRenderer;
#endif
                if (renderer == null)
                {
                    throw new InvalidOperationException("Control not loaded");
                }

                return renderer;
#endif
            }
        }

        /// <summary>
        /// Occurs when the selected time extent has changed.
        /// </summary>
        public event EventHandler<TimeExtentChangedEventArgs> CurrentExtentChanged;

        internal void RaiseCurrentExtentChanged(TimeExtentChangedEventArgs args)
        {
            CurrentExtentChanged?.Invoke(this, args);
        }
    }
}
