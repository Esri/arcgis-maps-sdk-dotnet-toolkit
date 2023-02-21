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
using Esri.ArcGISRuntime.UI;
using System.Windows.Media.Imaging;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="PopupMedia"/>.
    /// </summary>
    public class PopupMediaView : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopupMediaView"/> class.
        /// </summary>
        public PopupMediaView()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
        }

        private async void UpdateChart()
        {
            if (PopupMedia is null || PopupMedia.Value is null)
            {
                Content = null;
                return;
            }
            if (PopupMedia.Type == PopupMediaType.Image)
            {
                Image img = Content as Image ?? new Image();
                if (PopupMedia.Value.SourceUrl != null)
                {
                    if (img.Source is not BitmapImage bmi || bmi.UriSource?.OriginalString != PopupMedia.Value.SourceUrl)
                    {
                        img.Source = new BitmapImage(new Uri(PopupMedia.Value.SourceUrl));
                    }
                }
                Content = img;
            }
            else // Chart
            {
                try
                {
                    Content = await GenerateChartAsync();
                }
                catch
                {
                    Content = null;
                }
            }
        }
        
        internal virtual async Task<UIElement?> GenerateChartAsync()
        {
            if (PopupMedia is null)
                return null;
            var width = (int)MaxWidth;
            if (width < 1) width = 600;
            var scalefactor = 1f; //TODO: get scale factor
            width = (int)(width * scalefactor);
            try
            {
                var chart = await PopupMedia.GenerateChartAsync(new Mapping.ChartImageParameters(width, width) { Dpi = scalefactor * 96 }); // TODO: Image scale
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
            UpdateChart();
        }
    }
}