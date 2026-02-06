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

using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Timer = System.Timers.Timer;

#if WPF
using Esri.ArcGISRuntime.UI;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xaml;
#elif MAUI
using DispatcherTimer = Microsoft.Maui.Dispatching.IDispatcherTimer;
#elif WINDOWS_XAML
using Esri.ArcGISRuntime.UI;
using Windows.Foundation;
#if WINUI
using Microsoft.UI.Xaml.Media.Imaging;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Media.Imaging;
#endif
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    public partial class PopupMediaView
    {
        private double _lastChartSize = 0;
        private const double MaxChartSize = 1024;
        private DispatcherTimer? _refreshTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupMediaView"/> class.
        /// </summary>
        public PopupMediaView()
        {
#if !MAUI
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
#else
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
#endif
            Loaded += OnViewLoaded;
            Unloaded += OnViewUnloaded;
        }

        private void UpdateMedia(Size desiredSize)
        {
            if (PopupMedia is null || PopupMedia.Value is null)
            {
                Content = null;
                return;
            }
            if (PopupMedia.Type == PopupMediaType.Image)
            {
                UpdateImage();
            }
            else
            {
                UpdateChart(desiredSize);
            }
        }

        private void UpdateImage()
        {
            if (PopupMedia is null)
            {
                Content = null;
            }
            else if (PopupMedia.Type == PopupMediaType.Image)
            {
                _lastChartSize = 0;
                Image img = Content as Image ?? new Image();
                var sourceUrl = PopupMedia.Value?.SourceUrl;
                if (!string.IsNullOrEmpty(sourceUrl))
                {
#if MAUI
                    if (img.Source is not RuntimeStreamImageSource rsis || rsis.Source?.OriginalString != sourceUrl || _refreshTimer?.IsRunning == true)
                    {
                        if (TryCreateImageSource(sourceUrl, out var source, _refreshTimer?.IsRunning == true))
                        {
#else
                    if (img.Source is not BitmapImage bmi || bmi.UriSource?.OriginalString != sourceUrl || _refreshTimer?.IsEnabled == true)
                    {
                        if (TryCreateImageSource(sourceUrl, out var source, _refreshTimer?.IsEnabled == true))
                        {
#endif
#if WPF
                            // This code ensures that the height of the MediaView in the Popup Viewer for WPF is maintained
                            // during refreshes of a dynamic image source for a smooth visual experience. It temporarily sets the height to the current
                            // actual height of the image while the new image is being downloaded, and resets it to auto once the download is complete.
                            if (double.IsNaN(img.Height))
                                img.Height = img.ActualHeight;
                            if (source is BitmapImage bitmapImage)
                            {
                                bitmapImage.DownloadCompleted += (s, e) =>
                                {
                                    img.Height = double.NaN;
                                };
                            }
#endif
                            img.Source = source;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(PopupMedia.Value?.LinkUrl) && Uri.TryCreate(PopupMedia.Value?.LinkUrl, UriKind.Absolute, out var linkUrl))
                {
#if MAUI
                    img.GestureRecognizers.Clear();
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) => _ = Launcher.LaunchUriAsync(linkUrl);
                    img.GestureRecognizers.Add(tapGesture);
#else
                    img.Tag = linkUrl;
#if WPF
                    if (img.Cursor != Cursors.Hand)
                    {
                        img.Cursor = Cursors.Hand;
                        img.MouseLeftButtonDown += (s, e) => _ = Launcher.LaunchUriAsync((s as Image)?.Tag as Uri);
                    }
#elif WINDOWS_XAML
                    img.Tapped += (s, e) => _ = Launcher.LaunchUriAsync((s as Image)?.Tag as Uri);
#endif
#endif
                }
                Content = img;
            }
        }
        private async void UpdateChart(Size desiredSize)
        {
            var oldContent = base.Content;
            if (PopupMedia is null || PopupMedia.Value is null)
            {
                Content = null;
                return;
            }
            if (PopupMedia.Type != PopupMediaType.Image) // Chart
            {
                var desiredWidth = desiredSize.Width;
                if (desiredWidth > MaxChartSize)
                    desiredWidth = MaxChartSize;
                else if (desiredWidth < 1)
                    desiredWidth = 0;
                if (desiredWidth == 0)
                {
                    _lastChartSize = 0;
                    Content = null;
                }
                else if (_lastChartSize != desiredWidth)
                {
                    _lastChartSize = desiredWidth;
                    try
                    {
#if MAUI
                        Content = await GenerateChartAsync(desiredWidth, desiredWidth, DeviceDisplay.Current.MainDisplayInfo.Density * 96);
#elif WPF
                        Content = await GenerateChartAsync(desiredWidth, desiredWidth, VisualTreeHelper.GetDpi(this).PixelsPerInchX);
#elif WINUI
                        Content = await GenerateChartAsync(desiredWidth, desiredWidth, (XamlRoot?.RasterizationScale ?? 1) * 96);
#elif WINDOWS_UWP
                        Content = await GenerateChartAsync(desiredWidth, desiredWidth, Windows.Graphics.Display.DisplayInformation.GetForCurrentView()?.LogicalDpi ?? 96);
#endif
                    }
                    catch
                    {
                        Content = null;
                    }
                }
            }
        }

        private async Task<Image?> GenerateChartAsync(double width, double height, double dpi)
        {
            if (PopupMedia is null || width < 1 || height < 1)
                return null;
            var scalefactor = dpi / 96;
            try
            {
                Mapping.ChartImageStyle style = Mapping.ChartImageStyle.Neutral;
#if MAUI
                switch (Application.Current?.RequestedTheme)
                {
                    case Microsoft.Maui.ApplicationModel.AppTheme.Dark: style = Mapping.ChartImageStyle.Dark; break;
                    case Microsoft.Maui.ApplicationModel.AppTheme.Light: style = Mapping.ChartImageStyle.Light; break;
                    default: style = Mapping.ChartImageStyle.Neutral; break;
                }
#elif WINDOWS_XAML
                switch (ActualTheme)
                {
                    case ElementTheme.Dark: style = Mapping.ChartImageStyle.Dark; break;
                    case ElementTheme.Light: style = Mapping.ChartImageStyle.Light; break;
                    default: style = Mapping.ChartImageStyle.Neutral; break;
                }
#endif
                var chart = await PopupMedia.GenerateChartAsync(new Mapping.ChartImageParameters((int)(width * scalefactor), (int)(height * scalefactor)) { Dpi = (float)dpi, Style = style });
                var source = await chart.Image.ToImageSourceAsync();
                return new Image() { Source = source };
            }
            catch { return null; }
        }

        /// <summary>
        /// Gets or sets the PopupMedia to be displayed.
        /// </summary>
        public PopupMedia? PopupMedia
        {
            get => (PopupMedia)GetValue(PopupMediaProperty);
            set => SetValue(PopupMediaProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PopupMedia"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PopupMediaProperty =
            PropertyHelper.CreateProperty<PopupMedia, PopupMediaView>(nameof(PopupMedia), null, (s, oldValue, newValue) => s.OnPopupMediaPropertyChanged());

        private void OnPopupMediaPropertyChanged()
        {
            // Stop any existing timer
            StopRefreshTimer();
            _lastChartSize = 0;
            if (PopupMedia?.Type == PopupMediaType.Image)
            {
                UpdateImage();
                if (IsLoaded)
                    StartRefreshTimer(PopupMedia.ImageRefreshInterval);
            }
            else
            {
                InvalidateMeasure(); // Forces recalculation of available space for generating a new chart
            }
        }

        // Also used for embedded images in TextPopupElement views
        internal static bool TryCreateImageSource(string? sourceUri, out ImageSource? source, bool ignoreImageCache = false)
        {
            if (sourceUri != null && sourceUri.StartsWith("data:image/"))
            {
                // might be base64
                var idx = sourceUri.IndexOf(";base64,");
                if (idx > 11 && sourceUri.Length > idx + 8)
                {
                    try
                    {
                        var base64data = sourceUri.Substring(idx + 8);
                        var data = Convert.FromBase64String(base64data);
#if MAUI
                        var newSource = new StreamImageSource { Stream = (token) => Task.FromResult<Stream>(new MemoryStream(data)) };
#elif WPF
                        var newSource = new BitmapImage();
                        newSource.BeginInit();
                        newSource.StreamSource = new MemoryStream(data);
                        newSource.EndInit();
#else
                        var newSource = new BitmapImage();
                        newSource.SetSource(new MemoryStream(data).AsRandomAccessStream());
#endif
                        source = newSource;
                        return true;
                    }
                    catch { }
                }
            }
            else if (Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? result))
            {
#if MAUI
                source = new RuntimeStreamImageSource(result);
#else
                if (ignoreImageCache)
                {
                    var newSource = new BitmapImage();
#if WPF
                    newSource.BeginInit();
                    newSource.CacheOption = BitmapCacheOption.OnLoad; // Load the image into memory
#endif
                    newSource.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Disable caching
                    newSource.UriSource = result;
#if WPF
                    newSource.EndInit();
#endif
                    source = newSource;
                }
                else
                {
                    source = new BitmapImage(result);
                }
#endif
                return true;
            }
            source = null;
            return false;
        }

        private void OnViewLoaded(object? sender,
#if WINDOWS_XAML
            RoutedEventArgs
#else
            EventArgs
#endif
            e)
        {
            if (PopupMedia?.Type == PopupMediaType.Image)
            {
                StartRefreshTimer(PopupMedia.ImageRefreshInterval);
            }
        }

        private void OnViewUnloaded(object? sender,
#if WINDOWS_XAML
            RoutedEventArgs
#else
            EventArgs
#endif
            e)
        {
            StopRefreshTimer();
        }

        private void StartRefreshTimer(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                return;

            // Stop any existing timer to avoid double-instantiation
            StopRefreshTimer();

            // Initialize and start a new timer
#if MAUI
            _refreshTimer = Dispatcher.CreateTimer();
            _refreshTimer.IsRepeating = true;
#else
            _refreshTimer = new DispatcherTimer();
#endif
            _refreshTimer.Tick += OnRefreshTimerElapsed;
            _refreshTimer.Interval = interval;
            _refreshTimer.Start();
        }

        private void StopRefreshTimer()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnRefreshTimerElapsed;
                _refreshTimer = null;
            }
        }

        private void OnRefreshTimerElapsed(object? sender, object e)
        {
            UpdateImage();
        }
    }
}