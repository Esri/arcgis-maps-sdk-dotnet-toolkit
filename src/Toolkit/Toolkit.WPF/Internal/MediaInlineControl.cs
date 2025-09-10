using System.Windows.Threading;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Specifies the type of media content.
    /// </summary>
    internal enum MediaType
    {
        Audio,
        Video
    }

    /// <summary>
    /// Control for displaying inline media with playback controls.
    /// </summary>
    internal class MediaInlineControl : UserControl
    {
        private const string PlayButtonGlyph = "\u25B6";
        private const string PauseButtonGlyph = "\u23F8";
        private readonly MediaElement _mediaElement;
        private readonly Button _playPauseButton;
        private readonly Slider _progressSlider;
        private readonly TextBlock _timestampText;
        private readonly DispatcherTimer _timer;
        private bool _isPlaying;
        private bool _isSliderDragging;
        private TimeSpan _duration;

        public MediaInlineControl(Uri source, MediaType mediaType)
        {
            Padding = BorderThickness = new Thickness(0);
            Background = Brushes.Transparent;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            Focusable = false;

            _mediaElement = new MediaElement
            {
                Source = source,
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Stretch = Stretch.Uniform,
                Width = 300,
                Height = mediaType is MediaType.Audio ? double.NaN : 200,
                Margin = new Thickness(5),
            };

            var controlsGridMargin = new Thickness(4, 0, 4, 0);

            _playPauseButton = new Button
            {
                Content = PlayButtonGlyph,
                Width = 32,
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = controlsGridMargin,
            };

            _progressSlider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = 0,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = controlsGridMargin,
            };

            _timestampText = new TextBlock
            {
                Text = "00:00 / 00:00",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = controlsGridMargin,
            };

            var controlsGrid = new Grid();
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            controlsGrid.Children.Add(_playPauseButton);
            Grid.SetColumn(_playPauseButton, 0);

            controlsGrid.Children.Add(_progressSlider);
            Grid.SetColumn(_progressSlider, 1);

            controlsGrid.Children.Add(_timestampText);
            Grid.SetColumn(_timestampText, 2);

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            mainGrid.Children.Add(_mediaElement);
            Grid.SetRow(_mediaElement, 0);

            mainGrid.Children.Add(controlsGrid);
            Grid.SetRow(controlsGrid, 1);

            Content = mainGrid;

            // Timer for updating slider and timestamp
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (s, e) =>
            {
                if (!_isSliderDragging && _duration.TotalSeconds > 0)
                {
                    _progressSlider.Value = _mediaElement.Position.TotalSeconds;
                    UpdateTimestamp();
                }
            };

            _playPauseButton.Click += (s, e) =>
            {
                if (_isPlaying)
                {
                    _mediaElement.Pause();
                    _isPlaying = false;
                    _playPauseButton.Content = PlayButtonGlyph;
                    _timer.Stop();
                }
                else
                {
                    _mediaElement.Play();
                    _isPlaying = true;
                    _playPauseButton.Content = PauseButtonGlyph;
                    _timer.Start();
                }
            };

            _progressSlider.PreviewMouseDown += (s, e) => _isSliderDragging = true;
            _progressSlider.PreviewMouseUp += (s, e) =>
            {
                if (_duration.TotalSeconds > 0)
                {
                    _mediaElement.Position = TimeSpan.FromSeconds(_progressSlider.Value);
                }
                _isSliderDragging = false;
            };

            _progressSlider.ValueChanged += (s, e) =>
            {
                if (_isSliderDragging && _duration.TotalSeconds > 0)
                {
                    _timestampText.Text = $"{FormatTime(TimeSpan.FromSeconds(_progressSlider.Value))} / {FormatTime(_duration)}";
                }
            };

            _mediaElement.MediaOpened += (s, e) =>
            {
                _duration = _mediaElement.NaturalDuration.HasTimeSpan ? _mediaElement.NaturalDuration.TimeSpan : TimeSpan.Zero;
                _progressSlider.Maximum = _duration.TotalSeconds;
                UpdateTimestamp();
            };

            _mediaElement.MediaEnded += (s, e) =>
            {
                _mediaElement.Stop();
                _isPlaying = false;
                _playPauseButton.Content = PlayButtonGlyph;
                _timer.Stop();
                _progressSlider.Value = 0;
                UpdateTimestamp();
            };
            _mediaElement.MediaFailed += (s, e) =>
            {
                _isPlaying = false;
                _playPauseButton.Content = PlayButtonGlyph;
                _timer.Stop();
                UpdateTimestamp();
            };
        }

        private void UpdateTimestamp()
        {
            var current = _mediaElement.Position;
            var total = _duration;
            _timestampText.Text = $"{FormatTime(current)} / {FormatTime(total)}";
        }

        private static string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}