﻿// /*******************************************************************************
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

#if WPF || MAUI
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if WPF
using Esri.ArcGISRuntime.UI;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xaml;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupMediaView"/> class.
        /// </summary>
        public PopupMediaView()
        {
#if !MAUI
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
#endif
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
#if MAUI
                Image img = this;
#else
                Image img = Content as Image ?? new Image();
#endif
                var sourceUrl = PopupMedia.Value?.SourceUrl;
                if (!string.IsNullOrEmpty(sourceUrl))
                {
#if MAUI
                    if (img.Source is not UriImageSource bmi || bmi.Uri?.OriginalString != sourceUrl)
#else
                    if (img.Source is not BitmapImage bmi || bmi.UriSource?.OriginalString != sourceUrl)
#endif
                    {
                        if (sourceUrl.StartsWith("data:image/"))
                        {
                            // might be base64
                            var idx = sourceUrl.IndexOf(";base64,");
                            if (idx > 11 && sourceUrl.Length > idx + 8)
                            {
                                try
                                {
                                    var base64data = sourceUrl.Substring(idx + 8);
                                    var data = Convert.FromBase64String(base64data);
#if MAUI
                                    var source = new StreamImageSource() { Stream = (token) => Task.FromResult<Stream>(new MemoryStream(data)) };
#else
                                    var source = new BitmapImage();
                                    source.BeginInit();
                                    source.StreamSource = new MemoryStream(data);
                                    source.EndInit();
#endif
                                    img.Source = source;
                                }
                                catch { }
                            }
                        }
                        else if (PopupMedia.Value != null && Uri.TryCreate(PopupMedia.Value.SourceUrl, UriKind.Absolute, out Uri? result))
                        {
#if MAUI
                            img.Source = new UriImageSource() { Uri = result };
#else
                            img.Source = new BitmapImage(result);
#endif
                        }
                    }
                }
                if (!string.IsNullOrEmpty(PopupMedia.Value?.LinkUrl) && Uri.TryCreate(PopupMedia.Value?.LinkUrl, UriKind.Absolute, out var linkUrl))
                {
#if MAUI
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) => _ = Launcher.LaunchUriAsync(linkUrl);
                    img.GestureRecognizers.Add(tapGesture);
#else
                    img.Cursor = Cursors.Hand;
                    img.MouseLeftButtonDown += (s, e) => _ = Launcher.LaunchUriAsync(linkUrl);
#endif
                }
#if !MAUI
                Content = img;
#endif
            }
        }


        private async void UpdateChart(Size desiredSize)
        {
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
#else
                        Content = await GenerateChartAsync(desiredWidth, desiredWidth, VisualTreeHelper.GetDpi(this).PixelsPerInchX);
#endif
                    }
                    catch
                    {
                        Content = null;
                    }
                }
            }
        }
#if MAUI
        private async Task<ImageSource?> GenerateChartAsync(double width, double height, double dpi)
#else
        private async Task<Image?> GenerateChartAsync(double width, double height, double dpi)
#endif
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
#endif
                var chart = await PopupMedia.GenerateChartAsync(new Mapping.ChartImageParameters((int)(width * scalefactor), (int)(height * scalefactor)) { Dpi = (float)dpi, Style = style });
                var source = await chart.Image.ToImageSourceAsync();
#if MAUI
                return source;
#else
                return new Image() { Source = source };
#endif
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
#if MAUI
        public static readonly BindableProperty PopupMediaProperty =
            BindableProperty.Create(nameof(PopupMedia), typeof(PopupMedia), typeof(PopupMediaView), null, propertyChanged: (s, o, n) => ((PopupMediaView)s).OnPopupMediaPropertyChanged());
#else
        public static readonly DependencyProperty PopupMediaProperty =
            DependencyProperty.Register(nameof(PopupMedia), typeof(PopupMedia), typeof(PopupMediaView), new PropertyMetadata(null, (s, e) => ((PopupMediaView)s).OnPopupMediaPropertyChanged()));
#endif

        private void OnPopupMediaPropertyChanged()
        {
            // TODO: Handle PopupMedia.ImageRefreshInterval - Start/Stop/Reset refresh timer if "interval > 0 && IsLoaded==true" + start/stop on load/unload
            _lastChartSize = 0;
            if (PopupMedia?.Type == PopupMediaType.Image)
            {
                UpdateImage();
            }
            else
            {
                InvalidateMeasure(); // Forces recalculation of available space for generating a new chart
            }
        }
    }
}
#endif