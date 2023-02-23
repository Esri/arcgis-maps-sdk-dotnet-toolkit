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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="PopupMedia"/>.
    /// </summary>
    public class PopupMediaView : ContentControl
    {
        private double _lastChartSize = 0;
        private const double MaxChartSize = 1024;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupMediaView"/> class.
        /// </summary>
        public PopupMediaView()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
        }

        /// <inheritdoc />
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            _lastChartSize = 0;
            base.OnDpiChanged(oldDpi, newDpi);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            if (PopupMedia.Type != PopupMediaType.Image)
            {
                UpdateChart(constraint);
            }
            return base.MeasureOverride(constraint);
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
            if (PopupMedia.Type == PopupMediaType.Image)
            {
                _lastChartSize = 0;
                Image img = Content as Image ?? new Image();
                var sourceUrl = PopupMedia.Value.SourceUrl;
                if (!string.IsNullOrEmpty(sourceUrl))
                {
                    if (img.Source is not BitmapImage bmi || bmi.UriSource?.OriginalString != sourceUrl)
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
                                    var source = new BitmapImage();
                                    source.BeginInit();
                                    source.StreamSource = new MemoryStream(data);
                                    source.EndInit();
                                    img.Source = source;
                                }
                                catch { }
                            }
                        }
                        else if (Uri.TryCreate(PopupMedia.Value.SourceUrl, UriKind.Absolute, out Uri result))
                            img.Source = new BitmapImage(result);
                    }
                }
                if (!string.IsNullOrEmpty(PopupMedia.Value.LinkUrl) && Uri.TryCreate(PopupMedia.Value.LinkUrl, UriKind.Absolute, out var linkUrl))
                {
                    img.Cursor = Cursors.Hand;
                    img.MouseLeftButtonDown += (s, e) => _ = Launcher.LaunchUriAsync(linkUrl);
                }
                Content = img;
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
                        Content = await GenerateChartAsync(desiredWidth, desiredWidth, VisualTreeHelper.GetDpi(this).PixelsPerInchX);
                    }
                    catch
                    {
                        Content = null;
                    }
                }
            }
        }
        
        internal async Task<UIElement?> GenerateChartAsync(double width, double height, double dpi)
        {
            if (PopupMedia is null || width < 1 || height < 1)
                return null;
            var scalefactor = dpi / 96;
            try
            {
                const double oversizingFactor = 2; // Charting API currently generates very large text, so we generate the image larger and scale it back down again.
                var chart = await PopupMedia.GenerateChartAsync(new Mapping.ChartImageParameters((int)(width * scalefactor * oversizingFactor), (int)(height * scalefactor * oversizingFactor)) { Dpi = (float)dpi }); // TODO: Image scale
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
            DependencyProperty.Register(nameof(PopupMedia), typeof(PopupMedia), typeof(PopupMediaView), new PropertyMetadata(null, (s, e) => ((PopupMediaView)s).OnPopupMediaPropertyChanged()));

        private void OnPopupMediaPropertyChanged()
        {
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