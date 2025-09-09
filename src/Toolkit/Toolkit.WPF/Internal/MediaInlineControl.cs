using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
    /// Private control for displaying inline media with play/pause overlay.
    /// </summary>
    internal class MediaInlineControl : UserControl
    {
        private readonly MediaElement _mediaElement;
        private readonly Grid _overlay;
        private bool _isPlaying;

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
                Width = mediaType is MediaType.Audio ? double.NaN : 300,
                Height = mediaType is MediaType.Audio ? 50 : 200,
            };

            UIElement mediaVisual;
            if (mediaType is MediaType.Audio)
            {
                mediaVisual = new TextBlock
                {
                    Text = "🔊",
                    FontSize = 32,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else
            {
                mediaVisual = _mediaElement;
            }

            var panel = new Grid();
            panel.Children.Add(mediaVisual);

            _overlay = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Visible
            };
            var playIcon = new TextBlock
            {
                Text = "▶",
                FontSize = 32,
                Foreground = Brushes.LightGray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _overlay.Children.Add(playIcon);

            panel.Children.Add(_overlay);

            var button = new Button
            {
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Focusable = false,
                Content = panel
            };

            button.Click += (s, e) =>
            {
                if (_isPlaying)
                {
                    _mediaElement.Pause();
                }
                else
                {
                    _isPlaying = true;
                    _mediaElement.Play();
                }
                UpdateOverlay();
            };

            _mediaElement.MediaEnded += (s, e) =>
            {
                _mediaElement.Stop();
                _isPlaying = false;
                UpdateOverlay();
            };
            _mediaElement.MediaFailed += (s, e) =>
            {
                _isPlaying = false;
                UpdateOverlay();
            };

            UpdateOverlay();

            Content = button;
        }

        private void UpdateOverlay()
        {
            _overlay.Visibility = (!_isPlaying) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}