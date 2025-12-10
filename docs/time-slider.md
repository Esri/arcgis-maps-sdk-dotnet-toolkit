# TimeSlider 

TimeSlider allows you to animate or manually step through a time range, with options to configure based on a time-aware layer or a GeoView.

![TimeSlider on WinUI](https://user-images.githubusercontent.com/29742178/147712751-6d6db182-3e72-4dfc-ba23-3fbe97b1f934.png)

## Features

- Supports stepping through the time extent with pre-defined steps. Steps can be customized.
- Supports animating, with play and pause functionality, the time extent
- Raises events when the time slider's time extent changes
- Supports initialization of the slider from a layer or a GeoView.

## Key API

The following API methods, events, and properties are available to configure, interact, and respond to the time slider.

See the API reference for full details.

### Time control

- `CurrentExtentChanged` - Raised when the current time extent in the slider changes. Listen to this if you want to update a GeoView's time extent.
- `InitializeTimePropertiesAsync` - Initializes various properties, including full extent and time intervals, based on the GeoModel displayed by a GeoView, or a time-aware layer.
- `InitializeTimeSteps` - Divides the slider into a specific number of steps
- `FullExtent` - Full time range for the slider
- `CurrentExtent` - Currently selected time range within the slider
- `TimeStepInterval` - Duration of a time step
- `TimeSteps` - Quantity of time steps
- `IsStartTimePinned` - Controls whether selected start time can be changed
- `IsEndTimePinned` - Controls whether selected end time can be changed

### Playback

- `IsPlaying` - Gets or sets a value indicating whether the time slider is animating
- `PlaybackInterval` - Controls how long (in user time) the time slider waits before stepping the current extent when `IsPlaying` is true.
- `PlaybackDirection` - Controls whether playback is moving forward or backward
- `PlaybackLoopMode` - Controls the behavior when the time slider reaches the end of the animation

### Labeling

- `LabelMode` - Controls how intervals and extents are labeled
- `FullExtentLabelFormat` - Controls the format for the extent labels
- `CurrentExtentLabelFormat` - Controls the format for the current extent labels
- `TimeStepIntervalLabelFormat` - Controls the format for the interval labels

## Usage

Note: following code assumes that `TimeSlider` has already been added to the view alongside a `MapView`.

```cs
private async Task InitializeSliderAsync()
{
    timeSlider.CurrentExtentChanged += Slider_CurrentExtentChanged;
    await timeSlider.InitializeTimePropertiesAsync(mapView);
}

private void Slider_CurrentExtentChanged(object sender, UI.TimeExtentChangedEventArgs e)
{
    mapView.TimeExtent = e.NewExtent;
}
```
