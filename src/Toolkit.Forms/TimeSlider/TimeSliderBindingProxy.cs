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

#if NETFX_CORE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Windows.UI.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// Provides property changed notifications for UWP TimeSlider DependencyProperties.
    /// </summary>
    internal class TimeSliderBindingProxy : DependencyObject, INotifyPropertyChanged
    {
        public TimeSliderBindingProxy()
        {
        }

        public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
            nameof(TimeSlider.IsPlaying), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.IsPlaying), OnPropertyChanged));

        public static readonly DependencyProperty CurrentExtentProperty = DependencyProperty.Register(
            nameof(TimeSlider.CurrentExtent), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.CurrentExtent), OnPropertyChanged));

        public static readonly DependencyProperty FullExtentLabelColorProperty = DependencyProperty.Register(
            nameof(TimeSlider.FullExtentLabelColor), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.FullExtentLabelColor), OnPropertyChanged));

        public static readonly DependencyProperty PlaybackButtonsStrokeProperty = DependencyProperty.Register(
            nameof(TimeSlider.PlaybackButtonsStroke), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.PlaybackButtonsStroke), OnPropertyChanged));

        public static readonly DependencyProperty PlaybackButtonsFillProperty = DependencyProperty.Register(
            nameof(TimeSlider.PlaybackButtonsFill), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.PlaybackButtonsFill), OnPropertyChanged));

        public static readonly DependencyProperty TimeStepIntervalTickFillProperty = DependencyProperty.Register(
            nameof(TimeSlider.TimeStepIntervalTickFill), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.TimeStepIntervalTickFill), OnPropertyChanged));

        public static readonly DependencyProperty FullExtentStrokeProperty = DependencyProperty.Register(
            nameof(TimeSlider.FullExtentStroke), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.FullExtentStroke), OnPropertyChanged));

        public static readonly DependencyProperty FullExtentFillProperty = DependencyProperty.Register(
            nameof(TimeSlider.FullExtentFill), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.FullExtentFill), OnPropertyChanged));

        public static readonly DependencyProperty CurrentExtentFillProperty = DependencyProperty.Register(
            nameof(TimeSlider.CurrentExtentFill), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.CurrentExtentFill), OnPropertyChanged));

        public static readonly DependencyProperty ThumbFillProperty = DependencyProperty.Register(
            nameof(TimeSlider.ThumbFill), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.ThumbFill), OnPropertyChanged));

        public static readonly DependencyProperty ThumbStrokeProperty = DependencyProperty.Register(
            nameof(TimeSlider.ThumbStroke), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.ThumbStroke), OnPropertyChanged));

        public static readonly DependencyProperty LabelModeProperty = DependencyProperty.Register(
            nameof(TimeSlider.LabelMode), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.LabelMode), OnPropertyChanged));

        public static readonly DependencyProperty CurrentExtentLabelColorProperty = DependencyProperty.Register(
            nameof(TimeSlider.CurrentExtentLabelColor), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.CurrentExtentLabelColor), OnPropertyChanged));

        public static readonly DependencyProperty TimeStepIntervalLabelFormatProperty = DependencyProperty.Register(
            nameof(TimeSlider.TimeStepIntervalLabelFormat), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.TimeStepIntervalLabelFormat), OnPropertyChanged));

        public static readonly DependencyProperty FullExtentLabelFormatProperty = DependencyProperty.Register(
            nameof(TimeSlider.FullExtentLabelFormat), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.FullExtentLabelFormat), OnPropertyChanged));

        public static readonly DependencyProperty IsEndTimePinnedProperty = DependencyProperty.Register(
            nameof(TimeSlider.IsEndTimePinned), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.IsEndTimePinned), OnPropertyChanged));

        public static readonly DependencyProperty IsStartTimePinnedProperty = DependencyProperty.Register(
            nameof(TimeSlider.IsStartTimePinned), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.IsStartTimePinned), OnPropertyChanged));

        public static readonly DependencyProperty LoopModeProperty = DependencyProperty.Register(
            nameof(TimeSlider.PlaybackLoopMode), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.PlaybackLoopMode), OnPropertyChanged));

        public static readonly DependencyProperty PlaybackDirectionProperty = DependencyProperty.Register(
            nameof(TimeSlider.PlaybackDirection), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.PlaybackDirection), OnPropertyChanged));

        public static readonly DependencyProperty PlaybackIntervalProperty = DependencyProperty.Register(
            nameof(TimeSlider.PlaybackInterval), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.PlaybackInterval), OnPropertyChanged));

        public static readonly DependencyProperty TimeStepsProperty = DependencyProperty.Register(
            nameof(TimeSlider.TimeSteps), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.TimeSteps), OnPropertyChanged));

        public static readonly DependencyProperty TimeStepIntervalProperty = DependencyProperty.Register(
            nameof(TimeSlider.TimeStepInterval), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.TimeStepInterval), OnPropertyChanged));

        public static readonly DependencyProperty FullExtentProperty = DependencyProperty.Register(
            nameof(TimeSlider.FullExtent), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.FullExtent), OnPropertyChanged));

        public static readonly DependencyProperty CurrentExtentLabelFormatProperty = DependencyProperty.Register(
            nameof(TimeSlider.CurrentExtentLabelFormat), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.CurrentExtentLabelFormat), OnPropertyChanged));

        public static readonly DependencyProperty TimeStepIntervalLabelColorProperty = DependencyProperty.Register(
            nameof(TimeSlider.TimeStepIntervalLabelColor), typeof(object), typeof(TimeSliderBindingProxy),
            new PropertyMetadata(nameof(TimeSlider.TimeStepIntervalLabelColor), OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var propertyName = e.Property.GetMetadata(typeof(TimeSliderBindingProxy)).DefaultValue as string;
            ((TimeSliderBindingProxy)d).OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
#endif
