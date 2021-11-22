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

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
#if WINDOWS_UWP
using Windows.UI.Xaml.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Wraps a search result for display.
    /// </summary>
    public class SearchResult : INotifyPropertyChanged
    {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly - catch-22
        private byte[]? _markerData;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
        private RuntimeImage? _markerImage;
        #if WINDOWS_UWP
        private ImageSource _markerImageSource;
        #endif
        private bool _imageRequestFlag;
        private string _displayTitle;
        private string? _displaySubtitle;
        private ISearchSource _owningSource;
        private GeoElement? _geoElement;
        private Viewpoint? _selectionViewpoint;
        private CalloutDefinition? _calloutDefinition;

        /// <summary>
        /// Gets or sets the title that should be shown whenever the result is displayed.
        /// </summary>
        public string DisplayTitle
        {
            get => _displayTitle;
            set => SetPropertyChanged(value, ref _displayTitle);
        }

        /// <summary>
        /// Gets or sets the subtitle that should be shown whenever the result is displayed.
        /// </summary>
        public string? DisplaySubtitle
        {
            get => _displaySubtitle;
            set => SetPropertyChanged(value, ref _displaySubtitle);
        }

        /// <summary>
        /// Gets the search source that created this result.
        /// </summary>
        public ISearchSource OwningSource
        {
            get => _owningSource;
            private set => SetPropertyChanged(value, ref _owningSource);
        }

        /// <summary>
        /// Gets or sets the optional GeoElement for the result. This could be a graphic for a locator search, a feature for a feature layer search, or null if there is nothing to display on the map.
        /// </summary>
        public GeoElement? GeoElement
        {
            get => _geoElement;
            set
            {
                if (value != _geoElement)
                {
                    _geoElement = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoElement)));

                    // Start loading image
                    if (_geoElement is Graphic graphic && graphic.Symbol is Symbol symbol)
                    {
                        _ = LoadImage(symbol);
                    }
                    else if (_geoElement is Feature feature &&
                             feature.FeatureTable?.Layer is FeatureLayer featureLayer &&
                             featureLayer.Renderer?.GetSymbol(_geoElement) is Symbol featureSymbol)
                    {
                        _ = LoadImage(featureSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the selection viewpoint for the result. Some search sources will return a viewpoint that should be used when a result is selected.
        /// </summary>
        public Viewpoint? SelectionViewpoint
        {
            get => _selectionViewpoint;
            set => SetPropertyChanged(value, ref _selectionViewpoint);
        }

        /// <summary>
        /// Gets or sets the CalloutDefinition used to display the result.
        /// </summary>
        public CalloutDefinition? CalloutDefinition
        {
            get => _calloutDefinition;
            set => SetPropertyChanged(value, ref _calloutDefinition);
        }

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
            _displayTitle = title;
            DisplaySubtitle = subtitle;
            _owningSource = owner;
            GeoElement = geoElement;
            SelectionViewpoint = viewpoint;
        }

        /// <summary>
        /// Gets the image displayed for this result.
        /// </summary>
        public RuntimeImage? MarkerImage
        {
            get => _markerImage;
            private set => SetPropertyChanged(value, ref _markerImage);
        }

        /// <summary>
        /// Gets or sets the image displayed for this result, in a format useful for cross-platform scenarios.
        /// </summary>
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly - catch-22
        public byte[]? MarkerImageData
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
        {
            get => _markerData;
            set => SetPropertyChanged(value, ref _markerData);
        }

        #if WINDOWS_UWP

        /// <summary>
        /// Gets the image displayed for this result, in a format useful for UWP.
        /// </summary>
        public ImageSource? MarkerImageSource
        {
            get => _markerImageSource;
            private set => SetPropertyChanged(value, ref _markerImageSource);
        }
#endif

        private async Task LoadImage(Symbol symbol)
        {
            if (_imageRequestFlag)
            {
                return;
            }

            _imageRequestFlag = true;

            try
            {
            MarkerImage = await symbol.CreateSwatchAsync();
            #if WINDOWS_UWP
            MarkerImageSource = await MarkerImage.ToImageSourceAsync();
            #endif
            var markerDataStream = await MarkerImage.GetEncodedBufferAsync();
            var buffer = new byte[markerDataStream.Length];
            await markerDataStream.ReadAsync(buffer, 0, (int)markerDataStream.Length);
            MarkerImageData = buffer;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MarkerImage)));
            }
            catch (Exception)
            {
                // Ignored
            }
            finally
            {
                _imageRequestFlag = false;
            }
        }

        private void SetPropertyChanged<T>(T value, ref T field, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(value, field))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
