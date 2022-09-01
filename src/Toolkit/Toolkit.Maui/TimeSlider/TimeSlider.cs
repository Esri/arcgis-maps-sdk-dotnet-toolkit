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
#if WINDOWS || __IOS__ || __ANDROID__
using Esri.ArcGISRuntime.Maui.Handlers;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI;
#if WINDOWS
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.TimeExtentChangedEventArgs;
#elif __IOS__
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.Maui.TimeExtentChangedEventArgs;
using Esri.ArcGISRuntime.Toolkit.Maui;
#elif __ANDROID__
using NativeViewType = Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider;
using TimeExtentChangedEventArg = Esri.ArcGISRuntime.Toolkit.Maui.TimeExtentChangedEventArgs;
using Esri.ArcGISRuntime.Toolkit.Maui;
#endif
namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// The TimeSlider is a utility Control that emits TimeExtent values typically for use with the Map Control
/// to enhance the viewing of geographic features that have attributes based upon Date/Time information.
/// </summary>
public class TimeSlider : View, ITimeSlider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSlider"/> class.
    /// </summary>
    public TimeSlider()
    {
    }

    /// <summary>
    /// Identifies the <see cref="CurrentExtent"/> bindable property.
    /// </summary>
    public static readonly BindableProperty CurrentExtentProperty =
        BindableProperty.Create(nameof(CurrentExtent), typeof(TimeExtent), typeof(TimeSlider), null, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the CurrentExtent for the compass.
    /// </summary>
    public TimeExtent CurrentExtent
    {
        get { return (TimeExtent)GetValue(CurrentExtentProperty); }
        set { SetValue(CurrentExtentProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="FullExtent"/> bindable property.
    /// </summary>
    public static readonly BindableProperty FullExtentProperty =
        BindableProperty.Create(nameof(FullExtent), typeof(TimeExtent), typeof(TimeSlider), null, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the FullExtent for the compass.
    /// </summary>
    public TimeExtent FullExtent
    {
        get { return (TimeExtent)GetValue(FullExtentProperty); }
        set { SetValue(FullExtentProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="TimeStepInterval"/> bindable property.
    /// </summary>
    public static readonly BindableProperty TimeStepIntervalProperty =
        BindableProperty.Create(nameof(TimeStepInterval), typeof(TimeValue), typeof(TimeSlider), null, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the TimeStepInterval for the compass.
    /// </summary>
    public TimeValue TimeStepInterval
    {
        get { return (TimeValue)GetValue(TimeStepIntervalProperty); }
        set { SetValue(TimeStepIntervalProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="TimeSteps"/> bindable property.
    /// </summary>
    public static readonly BindableProperty TimeStepsProperty =
        BindableProperty.Create(nameof(TimeSteps), typeof(IReadOnlyList<DateTimeOffset>), typeof(TimeSlider), null, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the TimeSteps for the compass.
    /// </summary>
    public IReadOnlyList<DateTimeOffset> TimeSteps
    {
        get { return (IReadOnlyList<DateTimeOffset>)GetValue(TimeStepsProperty); }
        set { SetValue(TimeStepsProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="PlaybackInterval"/> bindable property.
    /// </summary>
    public static readonly BindableProperty PlaybackIntervalProperty =
        BindableProperty.Create(nameof(PlaybackInterval), typeof(TimeSpan), typeof(TimeSlider), null, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the PlaybackInterval for the compass.
    /// </summary>
    public TimeSpan PlaybackInterval
    {
        get { return (TimeSpan)GetValue(PlaybackIntervalProperty); }
        set { SetValue(PlaybackIntervalProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="PlaybackDirection"/> bindable property.
    /// </summary>
    public static readonly BindableProperty PlaybackDirectionProperty =
        BindableProperty.Create(nameof(PlaybackDirection), typeof(PlaybackDirection), typeof(TimeSlider), null, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the PlaybackDirection for the compass.
    /// </summary>
    public PlaybackDirection PlaybackDirection
    {
        get { return (PlaybackDirection)GetValue(PlaybackDirectionProperty); }
        set { SetValue(PlaybackDirectionProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="PlaybackLoopMode"/> bindable property.
    /// </summary>
    public static readonly BindableProperty PlaybackLoopModeProperty =
        BindableProperty.Create(nameof(PlaybackLoopMode), typeof(LoopMode), typeof(TimeSlider), null, BindingMode.OneWay, null);

    /// <summary>
    /// Gets or sets the PlaybackLoopMode for the compass.
    /// </summary>
    public LoopMode PlaybackLoopMode
    {
        get { return (LoopMode)GetValue(PlaybackLoopModeProperty); }
        set { SetValue(PlaybackLoopModeProperty, value); }
    }

    public static readonly BindableProperty IsStartTimePinnedProperty =
        BindableProperty.Create(nameof(IsStartTimePinned), typeof(bool), typeof(TimeSlider), false);

    public bool IsStartTimePinned
    {
        get => (bool)GetValue(IsStartTimePinnedProperty);
        set => SetValue(IsStartTimePinnedProperty, value);
    }

    public static readonly BindableProperty IsEndTimePinnedProperty =
        BindableProperty.Create(nameof(IsEndTimePinned), typeof(bool), typeof(TimeSlider), false);

    public bool IsEndTimePinned
    {
        get => (bool)GetValue(IsEndTimePinnedProperty);
        set => SetValue(IsEndTimePinnedProperty, value);
    }
    public static readonly BindableProperty IsPlayingProperty =
        BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(TimeSlider), false);

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public static readonly BindableProperty FullExtentLabelFormatProperty =
        BindableProperty.Create(nameof(FullExtentLabelFormat), typeof(string), typeof(TimeSlider), null);

    public string? FullExtentLabelFormat
    {
        get => GetValue(FullExtentLabelFormatProperty) as string;
        set => SetValue(FullExtentLabelFormatProperty, value);
    }

    public static readonly BindableProperty CurrentExtentLabelFormatProperty =
        BindableProperty.Create(nameof(CurrentExtentLabelFormat), typeof(string), typeof(TimeSlider), null);

    public string? CurrentExtentLabelFormat
    {
        get => GetValue(CurrentExtentLabelFormatProperty) as string;
        set => SetValue(CurrentExtentLabelFormatProperty, value);
    }

    public static readonly BindableProperty TimeStepIntervalLabelFormatProperty =
        BindableProperty.Create(nameof(TimeStepIntervalLabelFormat), typeof(string), typeof(TimeSlider), null);

    public string? TimeStepIntervalLabelFormat
    {
        get => GetValue(TimeStepIntervalLabelFormatProperty) as string;
        set => SetValue(TimeStepIntervalLabelFormatProperty, value);
    }

    public static readonly BindableProperty LabelModeProperty =
        BindableProperty.Create(nameof(LabelMode), typeof(TimeSliderLabelMode), typeof(TimeSlider));

    public TimeSliderLabelMode LabelMode
    {
        get => (TimeSliderLabelMode)GetValue(LabelModeProperty);
        set => SetValue(LabelModeProperty, value);
    }

    public static readonly BindableProperty ThumbStrokeProperty =
        BindableProperty.Create(nameof(ThumbStroke), typeof(Color), typeof(TimeSlider));

    public Color ThumbStroke
    {
        get => GetValue(ThumbStrokeProperty) as Color;
        set => SetValue(ThumbStrokeProperty, value);
    }

    public static readonly BindableProperty ThumbFillProperty =
        BindableProperty.Create(nameof(ThumbFill), typeof(Color), typeof(TimeSlider));

    public Color ThumbFill
    {
        get => GetValue(ThumbFillProperty) as Color;
        set => SetValue(ThumbFillProperty, value);
    }

    public static readonly BindableProperty CurrentExtentFillProperty =
        BindableProperty.Create(nameof(CurrentExtentFill), typeof(Color), typeof(TimeSlider));

    public Color CurrentExtentFill
    {
        get => GetValue(CurrentExtentFillProperty) as Color;
        set => SetValue(CurrentExtentFillProperty, value);
    }

    public static readonly BindableProperty FullExtentFillProperty =
        BindableProperty.Create(nameof(FullExtentFill), typeof(Color), typeof(TimeSlider));

    public Color FullExtentFill
    {
        get => GetValue(FullExtentFillProperty) as Color;
        set => SetValue(FullExtentFillProperty, value);
    }

    public static readonly BindableProperty FullExtentStrokeProperty =
        BindableProperty.Create(nameof(FullExtentStroke), typeof(Color), typeof(TimeSlider));

    public Color FullExtentStroke
    {
        get => GetValue(FullExtentStrokeProperty) as Color;
        set => SetValue(FullExtentStrokeProperty, value);
    }

    public static readonly BindableProperty TimeStepIntervalTickFillProperty =
        BindableProperty.Create(nameof(TimeStepIntervalTickFill), typeof(Color), typeof(TimeSlider));

    public Color TimeStepIntervalTickFill
    {
        get => GetValue(TimeStepIntervalTickFillProperty) as Color;
        set => SetValue(TimeStepIntervalTickFillProperty, value);
    }

    public static readonly BindableProperty PlaybackButtonsFillProperty =
        BindableProperty.Create(nameof(PlaybackButtonsFill), typeof(Color), typeof(TimeSlider));

    public Color PlaybackButtonsFill
    {
        get => GetValue(PlaybackButtonsFillProperty) as Color;
        set => SetValue(PlaybackButtonsFillProperty, value);
    }

    public static readonly BindableProperty PlaybackButtonsStrokeProperty =
        BindableProperty.Create(nameof(PlaybackButtonsStroke), typeof(Color), typeof(TimeSlider));

    public Color PlaybackButtonsStroke
    {
        get => GetValue(PlaybackButtonsStrokeProperty) as Color;
        set => SetValue(PlaybackButtonsStrokeProperty, value);
    }

    public static readonly BindableProperty FullExtentLabelColorProperty =
        BindableProperty.Create(nameof(FullExtentLabelColor), typeof(Color), typeof(TimeSlider));

    public Color FullExtentLabelColor
    {
        get => GetValue(FullExtentLabelColorProperty) as Color;
        set => SetValue(FullExtentLabelColorProperty, value);
    }

    public static readonly BindableProperty CurrentExtentLabelColorProperty =
        BindableProperty.Create(nameof(CurrentExtentLabelColor), typeof(Color), typeof(TimeSlider));

    public Color CurrentExtentLabelColor
    {
        get => GetValue(CurrentExtentLabelColorProperty) as Color;
        set => SetValue(CurrentExtentLabelColorProperty, value);
    }

    public static readonly BindableProperty TimeStepIntervalLabelColorProperty =
        BindableProperty.Create(nameof(TimeStepIntervalLabelColor), typeof(Color), typeof(TimeSlider));

    public Color TimeStepIntervalLabelColor
    {
        get => GetValue(TimeStepIntervalLabelColorProperty) as Color;
        set => SetValue(TimeStepIntervalLabelColorProperty, value);
    }

    public bool StepForward(int timeSteps)
    {
        return (Handler?.PlatformView as NativeViewType)?.StepForward(timeSteps) ?? false;
    }

    public bool StepBack(int timeSteps)
    {
        return (Handler?.PlatformView as NativeViewType)?.StepBack(timeSteps) ?? false;
    }
    public void InitializeTimeSteps(int count)
    {
        (Handler?.PlatformView as NativeViewType)?.InitializeTimeSteps(count);
    }

    /// <summary>
    /// Initializes the time slider's temporal properties based on the specified time-aware layer. Specifically,
    /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>.
    /// </summary>
    /// <param name="timeAwareLayer">The layer to use to initialize the time slider.</param>
    /// <returns>Task.</returns>
    public Task InitializeTimePropertiesAsync(ITimeAware timeAwareLayer)
    {
        return (Handler?.PlatformView as NativeViewType)?.InitializeTimePropertiesAsync(timeAwareLayer) ?? Task.CompletedTask;
    }
    /// <summary>
    /// Initializes the time slider's temporal properties based on the specified GeoView. Specifically,
    /// this will initialize <see cref="FullExtent"/>, <see cref="TimeStepInterval"/>, and <see cref="CurrentExtent"/>.
    /// </summary>
    /// <param name="geoView">The GeoView to use to initialize the time-slider's properties.</param>
    /// <returns>Task.</returns>
    public Task InitializeTimePropertiesAsync(GeoView? geoView)
    {
#if WINDOWS || __IOS__ || __ANDROID__
        if (geoView?.Handler is MapViewHandler mvh)
            return (Handler.PlatformView as NativeViewType)?.InitializeTimePropertiesAsync(mvh.PlatformView);
        else if (geoView?.Handler is SceneViewHandler svh)
            return (Handler.PlatformView as NativeViewType)?.InitializeTimePropertiesAsync(svh.PlatformView);
#endif
        return Task.CompletedTask;
    }
    public void InvokeEventChanged(object? sender, TimeExtentChangedEventArg e)
    {
        CurrentExtentChanged?.Invoke(sender, e);
    }
    public event EventHandler<TimeExtentChangedEventArg> CurrentExtentChanged;
}
#endif