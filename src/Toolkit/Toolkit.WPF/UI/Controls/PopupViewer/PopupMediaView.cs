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
            else if (GeoElement is null)
            {
                // Chart but no attributes
                Content = null;
                return;
            }
            else // Chart
            {
                var fields = PopupMedia.Value.FieldNames;
                List<object?> data = new List<object?>(PopupMedia.Value.FieldNames.Count);
                foreach (var field in fields)
                {
                    if (GeoElement.Attributes.ContainsKey(field))
                    {
                        data.Add(GeoElement.Attributes[field]);
                    }
                }
                object? normalizeData = !string.IsNullOrEmpty(PopupMedia.Value.NormalizeFieldName) && GeoElement.Attributes.ContainsKey(PopupMedia.Value.NormalizeFieldName) ?
                     GeoElement.Attributes[PopupMedia.Value.NormalizeFieldName] : null;

                try
                {
                    Content = await GenerateChartAsync(PopupMedia.Type, data, normalizeData);
                }
                catch
                {
                    Content = null;
                }
            }
        }

        // TODO:
        internal virtual Task<UIElement> GenerateChartAsync(PopupMediaType type, IEnumerable<object?> data, object? normalizeValue)
        {
            return Task.FromResult<UIElement>(new TextBlock() { Text = type.ToString() + "\nData: " + string.Join(",", data.ToArray()) + "\n" + normalizeValue });
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

        /// <summary>
        /// Gets or sets the GeoElement who's attribute will be showed with the <see cref="FieldsPopupElement"/>.
        /// </summary>
        public GeoElement? GeoElement
        {
            get { return GetValue(GeoElementProperty) as GeoElement; }
            set { SetValue(GeoElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoElement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoElementProperty =
            DependencyProperty.Register(nameof(GeoElement), typeof(GeoElement), typeof(PopupMediaView), new PropertyMetadata(null, (s, e) => ((PopupMediaView)s).UpdateChart()));
    }
}