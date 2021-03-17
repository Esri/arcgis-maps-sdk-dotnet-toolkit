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

#if XAMARIN

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if __IOS__
using Color = UIKit.UIColor;
#elif __ANDROID__
using Android.Graphics;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class TimeSlider : INotifyPropertyChanged
    {
        private string _defaultFullExtentLabelFormat = "M/d/yyyy";
        private string _defaultCurrentExtentLabelFormat = "M/d/yyyy";

        #region Properties

        private TimeExtent _currentExtent;

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> associated with the visual thumbs(s) displayed on the TimeSlider.
        /// </summary>
        private TimeExtent CurrentExtentImpl
        {
            get => _currentExtent;
            set
            {
                if (_currentExtent != value)
                {
                    var oldValue = value;
                    _currentExtent = value;
                    ApplyCurrentExtentLabelFormat();
                    OnCurrentExtentPropertyChanged(oldValue, _currentExtent);
                    OnPropertyChanged(nameof(CurrentExtent));
                }
            }
        }

        private TimeExtent _fullExtent;

        /// <summary>
        /// Gets or sets the <see cref="TimeExtent" /> that specifies the overall start and end time of the time slider instance.
        /// </summary>
        private TimeExtent FullExtentImpl
        {
            get => _fullExtent;
            set
            {
                if (_fullExtent != value)
                {
                    _fullExtent = value;
                    ApplyFullExtentLabelFormat();
                    OnFullExtentPropertyChanged();
                    OnPropertyChanged(nameof(FullExtent));
                }
            }
        }

        private TimeValue _timeStepInterval;

        /// <summary>
        /// Gets or sets the time step intervals for the time slider.  The slider thumbs will snap to and tick marks will be shown at this interval.
        /// </summary>
        private TimeValue TimeStepIntervalImpl
        {
            get => _timeStepInterval;
            set
            {
                if (_timeStepInterval != value)
                {
                    _timeStepInterval = value;
                    OnTimeStepIntervalPropertyChanged();
                    OnPropertyChanged(nameof(TimeStepInterval));
                }
            }
        }

        private IReadOnlyList<DateTimeOffset> _timeSteps;

        /// <summary>
        /// Gets or sets the time steps that can be used to set the slider instance's current extent.
        /// </summary>
        private IReadOnlyList<DateTimeOffset> TimeStepsImpl
        {
            get => _timeSteps;
            set
            {
                if (_timeSteps != value)
                {
                    _timeSteps = value;
                    OnTimeStepsPropertyChanged();
                    OnPropertyChanged(nameof(TimeSteps));
                }
            }
        }

        private TimeSpan _playbackInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the interval at which the time slider's current extent will move to the next or previous time step.
        /// </summary>
        private TimeSpan PlaybackIntervalImpl
        {
            get => _playbackInterval;
            set
            {
                if (_playbackInterval != value)
                {
                    _playbackInterval = value;
                    OnPlaybackIntervalPropertyChanged(_playbackInterval);
                    OnPropertyChanged(nameof(PlaybackInterval));
                }
            }
        }

        private PlaybackDirection _playbackDirection = PlaybackDirection.Forward;

        /// <summary>
        /// Gets or sets whether the current extent will move to the next or the previous time step during playback.
        /// </summary>
        private PlaybackDirection PlaybackDirectionImpl
        {
            get => _playbackDirection;
            set
            {
                if (_playbackDirection != value)
                {
                    _playbackDirection = value;
                    OnPropertyChanged(nameof(PlaybackDirection));
                }
            }
        }

        private LoopMode _playbackLoopMode = LoopMode.None;

        /// <summary>
        /// Gets or sets the behavior when the current extent reaches the end of the slider during playback.
        /// </summary>
        private LoopMode PlaybackLoopModeImpl
        {
            get => _playbackLoopMode;
            set
            {
                if (_playbackLoopMode != value)
                {
                    _playbackLoopMode = value;
                    OnPropertyChanged(nameof(PlaybackLoopMode));
                }
            }
        }

        private bool _isStartTimePinned = false;

        /// <summary>
        /// Gets or sets a value indicating whether the start time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        private bool IsStartTimePinnedImpl
        {
            get => _isStartTimePinned;
            set
            {
                if (_isStartTimePinned != value)
                {
                    _isStartTimePinned = value;
                    OnIsStartTimePinnedChanged(_isStartTimePinned);
                    OnPropertyChanged(nameof(IsStartTimePinned));
                }
            }
        }

        private bool _isEndTimePinned = false;

        /// <summary>
        /// Gets or sets a value indicating whether the end time of the <see cref="CurrentExtent"/> is locked into place.
        /// </summary>
        private bool IsEndTimePinnedImpl
        {
            get => _isEndTimePinned;
            set
            {
                if (_isEndTimePinned != value)
                {
                    _isEndTimePinned = value;
                    OnIsEndTimePinnedChanged(_isEndTimePinned);
                    OnPropertyChanged(nameof(IsEndTimePinned));
                }
            }
        }

        private bool _isPlaying = false;

        /// <summary>
        /// Gets or sets a value indicating whether the time slider is animating playback.
        /// </summary>
        private bool IsPlayingImpl
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnIsPlayingPropertyChanged(_isPlaying);
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        #region Appearance Properties

        private string _fullExtentLabelFormat;

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="FullExtent"/>.
        /// </summary>
        private string FullExtentLabelFormatImpl
        {
            get => _fullExtentLabelFormat;
            set
            {
                if (_fullExtentLabelFormat != value)
                {
                    _fullExtentLabelFormat = value;
                    ApplyFullExtentLabelFormat();
                    OnPropertyChanged(nameof(FullExtentLabelFormat));
                }
            }
        }

        private string _currentExtentLabelFormat;

        /// <summary>
        /// Gets or sets the string format to use for displaying the start and end labels for the <see cref="CurrentExtent"/>.
        /// </summary>
        private string CurrentExtentLabelFormatImpl
        {
            get => _currentExtentLabelFormat;
            set
            {
                if (_currentExtentLabelFormat != value)
                {
                    _currentExtentLabelFormat = value;
                    ApplyCurrentExtentLabelFormat();
                    OnCurrentExtentLabelFormatPropertyChanged(_currentExtentLabelFormat);
                    OnPropertyChanged(nameof(CurrentExtentLabelFormat));
                }
            }
        }

        private string _timeStepIntervalLabelFormat;

        /// <summary>
        /// Gets or sets the string format to use for displaying the labels for the tick marks representing each time step interval.
        /// </summary>
        private string TimeStepIntervalLabelFormatImpl
        {
            get => _timeStepIntervalLabelFormat;
            set
            {
                if (_timeStepIntervalLabelFormat != value)
                {
                    _timeStepIntervalLabelFormat = value;
                    OnTimeStepIntervalLabelFormatPropertyChanged(TimeStepIntervalLabelFormat);
                    OnPropertyChanged(nameof(TimeStepIntervalLabelFormat));
                }
            }
        }

        private TimeSliderLabelMode _labelMode = TimeSliderLabelMode.CurrentExtent;

        /// <summary>
        /// Gets or sets the mode to use for labels along the time slider.
        /// </summary>
        private TimeSliderLabelMode LabelModeImpl
        {
            get => _labelMode;
            set
            {
                if (_labelMode != value)
                {
                    _labelMode = value;
                    OnLabelModePropertyChanged(_labelMode);
                    OnPropertyChanged(nameof(LabelMode));
                }
            }
        }

        private Color _thumbStroke =
#if __IOS__
            Color.LightGray;
#elif __ANDROID__
            Color.Rgb(94, 151, 246);
#endif

        /// <summary>
        /// Gets or sets the border color of the thumbs.
        /// </summary>
        private Color ThumbStrokeImpl
        {
            get => _thumbStroke;
            set
            {
                if (_thumbStroke != value)
                {
                    _thumbStroke = value;
                    MinimumThumb?.SetBorderColor(value);
                    MaximumThumb?.SetBorderColor(value);
                    OnPropertyChanged(nameof(ThumbStroke));
                }
            }
        }

        private Color _thumbFill =
#if __IOS__
            Color.White;
#elif __ANDROID__
            Color.Rgb(94, 151, 246);
#endif

        /// <summary>
        /// Gets or sets the fill color of the thumbs.
        /// </summary>
        private Color ThumbFillImpl
        {
            get => _thumbFill;
            set
            {
                if (_thumbFill != value)
                {
                    _thumbFill = value;
                    MinimumThumb?.SetBackgroundFill(value);
                    MaximumThumb?.SetBackgroundFill(value);
#if __ANDROID__
                    PinnedMinimumThumb?.SetBackgroundFill(value);
                    PinnedMaximumThumb?.SetBackgroundFill(value);
#endif
                    OnPropertyChanged(nameof(ThumbFill));
                }
            }
        }

        private Color _currentExtentFill =
#if __IOS__
            Color.FromRGB(0, 111, 255);
#elif __ANDROID__
            Color.Rgb(94, 151, 246);
#endif

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="CurrentExtent"/>.
        /// </summary>
        private Color CurrentExtentFillImpl
        {
            get => _currentExtentFill;
            set
            {
                if (_currentExtentFill != value)
                {
                    _currentExtentFill = value;
                    HorizontalTrackThumb?.SetBackgroundFill(value);
                    OnPropertyChanged(nameof(CurrentExtentFill));
                }
            }
        }

        private Color _fullExtentFill =
#if __IOS__
            Color.FromRGB(170, 169, 170);
#elif __ANDROID__
            Color.Rgb(92, 92, 92);
#endif

        /// <summary>
        /// Gets or sets the fill color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        private Color FullExtentFillImpl
        {
            get => _fullExtentFill;
            set
            {
                if (_fullExtentFill != value)
                {
                    _fullExtentFill = value;
                    SliderTrack?.SetBackgroundFill(value);
                    OnPropertyChanged(nameof(FullExtentFill));
                }
            }
        }

        private Color _fullExtentStroke =
#if __IOS__
            Color.FromRGB(170, 169, 170);
#elif __ANDROID__
            Color.Rgb(92, 92, 92);
#endif

        /// <summary>
        /// Gets or sets the border color of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        private Color FullExtentStrokeImpl
        {
            get => _fullExtentStroke;
            set
            {
                if (_fullExtentStroke != value)
                {
                    _fullExtentStroke = value;
#if __IOS__
                    SliderTrack?.SetBorderColor(value);
#elif __ANDROID__
                    SliderTrackOutline?.SetBackgroundFill(value);
#endif
                    OnPropertyChanged(nameof(FullExtentStroke));
                }
            }
        }

        private double _fullExtentBorderWidth = 0;

        /// <summary>
        /// Gets or sets the border width of the area on the slider track that indicates the <see cref="FullExtent"/>.
        /// </summary>
        /// <value>The full width of the extent border.</value>
        public double FullExtentBorderWidth
        {
            get => _fullExtentBorderWidth;
            set
            {
                if (_fullExtentBorderWidth != value)
                {
                    _fullExtentBorderWidth = value;
                    SliderTrack?.SetBorderWidth(value);
                    OnPropertyChanged();
                }
            }
        }

        private Color _timeStepIntervalTickFill =
#if __IOS__
            Color.FromRGB(170, 169, 170);
#elif __ANDROID__
            Color.Rgb(170, 169, 170);
#endif

        /// <summary>
        /// Gets or sets the color of the slider's tickmarks.
        /// </summary>
        private Color TimeStepIntervalTickFillImpl
        {
            get => _timeStepIntervalTickFill;
            set
            {
                if (_timeStepIntervalTickFill != value)
                {
                    _timeStepIntervalTickFill = value;
                    if (Tickmarks != null)
                    {
                        Tickmarks.TickFill = value;
                    }

                    _startTimeTickmark?.SetBackgroundFill(value);
                    _endTimeTickmark?.SetBackgroundFill(value);

                    OnPropertyChanged(nameof(TimeStepIntervalTickFill));
                }
            }
        }

        private Color _playbackButtonsFill =
#if __IOS__
            Color.FromRGB(230, 230, 230);
#elif __ANDROID__
            Color.Rgb(94, 151, 246);
#endif

        /// <summary>
        /// Gets or sets the fill color of the playback buttons.
        /// </summary>
        private Color PlaybackButtonsFillImpl
        {
            get => _playbackButtonsFill;
            set
            {
                if (_playbackButtonsFill != value)
                {
                    _playbackButtonsFill = value;
                    PreviousButton?.SetBackgroundFill(value);
                    NextButton?.SetBackgroundFill(value);
                    PlayPauseButton.SetBackgroundFill(value);
                    OnPropertyChanged(nameof(PlaybackButtonsFill));
                }
            }
        }

        private Color _playbackButtonsStroke =
#if __IOS__
            Color.FromRGB(170, 169, 170);
#elif __ANDROID__
            Color.Rgb(94, 151, 246);
#endif

        /// <summary>
        /// Gets or sets the border color of the playback buttons.
        /// </summary>
        private Color PlaybackButtonsStrokeImpl
        {
            get => _playbackButtonsStroke;
            set
            {
                if (_playbackButtonsStroke != value)
                {
                    _playbackButtonsStroke = value;
#if __IOS__
                    PreviousButton?.SetBorderColor(value);
                    NextButton?.SetBorderColor(value);
                    PlayPauseButton.SetBorderColor(value);
#elif __ANDROID__
                    PreviousButtonOutline.SetBackgroundFill(value);
                    NextButtonOutline.SetBackgroundFill(value);
                    PlayButtonOutline.SetBackgroundFill(value);
                    PauseButtonOutline.SetBackgroundFill(value);
#endif
                    OnPropertyChanged(nameof(PlaybackButtonsStroke));
                }
            }
        }

#if __IOS__
        private Color _fullExtentLabelColor;
#elif __ANDROID__
        private Color _fullExtentLabelColor = Color.Rgb(184, 184, 184);
#endif

        /// <summary>
        /// Gets or sets the color of the full extent labels.
        /// </summary>
        private Color FullExtentLabelColorImpl
        {
            get => _fullExtentLabelColor;
            set
            {
                if (_fullExtentLabelColor != value)
                {
                    _fullExtentLabelColor = value;
                    FullExtentStartTimeLabel?.SetTextColor(value);
                    FullExtentEndTimeLabel?.SetTextColor(value);
                    OnPropertyChanged(nameof(FullExtentLabelColor));
                }
            }
        }

#if __IOS__
        private Color _currentExtentLabelColor;
#elif __ANDROID__
        private Color _currentExtentLabelColor = Color.Rgb(184, 184, 184);
#endif

        /// <summary>
        /// Gets or sets the color of the current extent labels.
        /// </summary>
        private Color CurrentExtentLabelColorImpl
        {
            get => _currentExtentLabelColor;
            set
            {
                if (_currentExtentLabelColor != value)
                {
                    _currentExtentLabelColor = value;
                    MinimumThumbLabel?.SetTextColor(value);
                    MaximumThumbLabel?.SetTextColor(value);
                    OnPropertyChanged(nameof(CurrentExtentLabelColor));
                }
            }
        }

#if __IOS__
        private Color _timeStepIntervalLabelColor;
#elif __ANDROID__
        private Color _timeStepIntervalLabelColor = Color.Rgb(184, 184, 184);
#endif

        /// <summary>
        /// Gets or sets the color of the time step interval labels.
        /// </summary>
        private Color TimeStepIntervalLabelColorImpl
        {
            get => _timeStepIntervalLabelColor;
            set
            {
                if (_timeStepIntervalLabelColor != value)
                {
                    _timeStepIntervalLabelColor = value;
                    if (Tickmarks != null)
                    {
                        Tickmarks.TickLabelColor = value;
                    }

                    OnPropertyChanged(nameof(TimeStepIntervalLabelColor));
                }
            }
        }

        #endregion // Appearance Properties

        #endregion // Properties

        private void ApplyFullExtentLabelFormat()
        {
            var fullExtentLabelFormat = string.IsNullOrEmpty(FullExtentLabelFormat) ? _defaultFullExtentLabelFormat : FullExtentLabelFormat;
            FullExtentStartTimeLabel.Text = FullExtent?.StartTime.ToString(fullExtentLabelFormat) ?? string.Empty;
            FullExtentEndTimeLabel.Text = FullExtent?.EndTime.ToString(fullExtentLabelFormat) ?? string.Empty;

            InvalidateMeasureAndArrange();
        }

        private void ApplyCurrentExtentLabelFormat()
        {
            var currentExtentLabelFormat = string.IsNullOrEmpty(CurrentExtentLabelFormat) ? _defaultCurrentExtentLabelFormat : CurrentExtentLabelFormat;
            MinimumThumbLabel.Text = CurrentExtent?.StartTime.ToString(currentExtentLabelFormat) ?? string.Empty;
            MaximumThumbLabel.Text = CurrentExtent?.EndTime.ToString(currentExtentLabelFormat) ?? string.Empty;

            InvalidateMeasureAndArrange();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#endif