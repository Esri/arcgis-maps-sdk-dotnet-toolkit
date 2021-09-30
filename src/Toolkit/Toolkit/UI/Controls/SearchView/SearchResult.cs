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

using System.ComponentModel;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
#if !XAMARIN && !NETFX_CORE
using System.Windows.Media;
#elif NETFX_CORE
using Windows.UI.Xaml.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Wraps a search result for display.
    /// </summary>
    public class SearchResult : INotifyPropertyChanged
        //TODO - implement INPC for other properties
    {
        #if !XAMARIN
        private ImageSource _uiImage;
        #endif

        private RuntimeImage? _rtImage;
        private CalloutDefinition? _calloutDefinition;

        /// <summary>
        /// Gets or sets the title that should be shown whenever the result is displayed.
        /// </summary>
        public string DisplayTitle { get; set; }

        /// <summary>
        /// Gets or sets the subtitle that should be shown whenever the result is displayed.
        /// </summary>
        public string? DisplaySubtitle { get; set; }

        /// <summary>
        /// Gets the search source that created this result.
        /// </summary>
        public ISearchSource OwningSource { get; private set; }

        /// <summary>
        /// Gets or sets the optional GeoElement for the result. This could be a graphic for a locator search, a feature for a feature layer search, or null if there is nothing to display on the map.
        /// </summary>
        public GeoElement? GeoElement { get; set; }

        /// <summary>
        /// Gets or sets the selection viewpoint for the result. Some search sources will return a viewpoint that should be used when a result is selected.
        /// </summary>
        public Viewpoint? SelectionViewpoint { get; set; }

        /// <summary>
        /// Gets or sets the CalloutDefinition used to display the result.
        /// </summary>
        public CalloutDefinition? CalloutDefinition { get; set; }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// </summary>
        /// <param name="title">Sets the <see cref="DisplayTitle"/>.</param>
        /// <param name="subtitle">Sets the <see cref="DisplaySubtitle"/>.</param>
        /// <param name="owner">Sets the <see cref="OwningSource"/>.</param>
        /// <param name="geoElement">Sets the <see cref="GeoElement"/>.</param>
        /// <param name="viewpoint">Sets the <see cref="SelectionViewpoint"/>.</param>
        public SearchResult(string title, string? subtitle, ISearchSource owner, GeoElement? geoElement, Viewpoint? viewpoint)
        {
            DisplayTitle = title;
            DisplaySubtitle = subtitle;
            OwningSource = owner;
            GeoElement = geoElement;
            SelectionViewpoint = viewpoint;
        }

        #if !XAMARIN
        // TODO - cache these operations; multiple roundtrips to core for images is not fast
        public ImageSource MarkerImage
        {
            get
            {
                if (_uiImage != null)
                {
                    return _uiImage;
                }
                else if (GeoElement is Graphic graphic && graphic.Symbol != null)
                {
                    _ = GetImage(graphic.Symbol);
                }
                else if (GeoElement is Feature feature && feature.FeatureTable?.Layer is FeatureLayer featureLayer
                    && featureLayer.Renderer?.GetSymbol(GeoElement) is Symbol symbol)
                {
                    _ = GetImage(symbol);
                }
                return null;
            }
        }
        private bool _imageRequestFlag;

        private async Task GetImage(Symbol symbol)
        {
            if (_imageRequestFlag) return;
            _imageRequestFlag = true;
            var swatch = await symbol.CreateSwatchAsync();
            _uiImage = await swatch.ToImageSourceAsync();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MarkerImage)));
        }
        #endif
    }
}
