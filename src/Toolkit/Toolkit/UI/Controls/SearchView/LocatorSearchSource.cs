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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// Basic search source implementation for generic locators.
    /// </summary>
    public class LocatorSearchSource : ISearchSource, INotifyPropertyChanged
    {
        internal const string WorldGeocoderUriString = "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer";

        private static LocatorTask? _worldGeocoderTask;

        /// <summary>
        /// Creates a <see cref="LocatorSearchSource"/> configured for use with the Esri World Geocoder service.
        /// </summary>
        /// <param name="cancellationToken">Token used for cancellation.</param>
        /// <remarks>This method will re-use a static LocatorTask instance to improve performance.</remarks>
        public static async Task<LocatorSearchSource> CreateDefaultSourceAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_worldGeocoderTask == null)
            {
                _worldGeocoderTask = new LocatorTask(new Uri(WorldGeocoderUriString));
                await _worldGeocoderTask.LoadAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new WorldGeocoderSearchSource(_worldGeocoderTask, null);
        }


        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly Lazy<Task> _loadTask;
        /// <summary>
        /// Gets the task used to perform initial locator setup.
        /// </summary>
        protected Task LoadTask => _loadTask.Value;

        private string _displayName = string.Empty;
        private bool _displayNameSetExternally = false;

        /// <summary>
        /// Gets or sets the name of the locator. Defaults to the locator's name, or "locator" if not set.
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    _displayNameSetExternally = true;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of results to return for a search. Default is 6.
        /// </summary>
        public int MaximumResults { get; set; } = 6;

        /// <summary>
        /// Gets or sets the maximum number of suggestions to return. Default is 6.
        /// </summary>
        public int MaximumSuggestions { get; set; } = 6;

        /// <summary>
        /// Gets the geocode parameters, which can be used to configure search behavior.
        /// </summary>
        /// <remarks>
        /// <see cref="GeocodeParameters.MaxResults"/>, <see cref="GeocodeParameters.PreferredSearchLocation"/>,
        /// and <see cref="GeocodeParameters.SearchArea"/> are set by <see cref="LocatorSearchSource"/> automatically on search.
        /// <see cref="GeocodeParameters.Categories"/> is set to <c>"*"</c> when the <see cref="Locator"/> is loaded for the first time.
        /// </remarks>
        public GeocodeParameters GeocodeParameters { get; } = new GeocodeParameters();

        /// <summary>
        /// Gets the suggestion parameters, which can be used to configure suggestion behavior.
        /// </summary>
        /// <remarks>
        /// <see cref="SuggestParameters.MaxResults"/> and <see cref="SuggestParameters.PreferredSearchLocation"/> are
        /// set automatically on search.
        /// </remarks>
        public SuggestParameters SuggestParameters { get; } = new SuggestParameters();

        /// <summary>
        /// Gets the underlying locator.
        /// </summary>
        public LocatorTask Locator { get; }

        /// <summary>
        /// Gets or sets the placeholder to show when this search source is selected for use.
        /// </summary>
        public string? Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the default callout definition to use with results.
        /// </summary>
        public CalloutDefinition? DefaultCalloutDefinition { get; set; }

        /// <summary>
        /// Gets or sets the default symbol to use when displaying results.
        /// </summary>
        public Symbol? DefaultSymbol { get; set; } = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 2);

        /// <inheritdoc />
        public double DefaultZoomScale { get; set; } = 100_000;

        /// <inheritdoc />
        public Geometry.Geometry? SearchArea { get; set; }

        /// <inheritdoc />
        public MapPoint? PreferredSearchLocation { get; set; }

        /// <summary>
        /// Gets or sets the attribute key to use as the subtitle when returning results. Key must be included in <see cref="GeocodeParameters.ResultAttributeNames"/>.
        /// </summary>
        public string? SubtitleAttributeKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocatorSearchSource"/> class.
        /// <seealso cref="CreateDefaultSourceAsync(CancellationToken)"/> to create a source configured for use with the Esri World Geocoder.
        /// </summary>
        /// <param name="locator">Locator to be used.</param>
        public LocatorSearchSource(LocatorTask locator)
        {
            Locator = locator;

            RefreshDisplayName();

            _loadTask = new Lazy<Task>(EnsureLoaded);
        }

        private void RefreshDisplayName()
        {
            if (_displayNameSetExternally)
                return;

            if (Locator?.LocatorInfo is LocatorInfo info)
            {
                // Locators from online services have descriptions but not names.
                if (!string.IsNullOrWhiteSpace(info.Name) && info.Name != Locator.Uri?.ToString())
                    _displayName = info.Name;
                else if (!string.IsNullOrWhiteSpace(info.Description))
                    _displayName = info.Description;
                else
                    _displayName = Properties.Resources.GetString("Locator_DefaultName") ?? string.Empty;

                OnPropertyChanged(nameof(DisplayName));
            }

            GeocodeParameters.ResultAttributeNames.Add("*");
        }

        private async Task EnsureLoaded()
        {
            await Locator.LoadAsync();
            RefreshDisplayName();
#if MAUI
            Stream? resourceStream = Assembly.GetAssembly(typeof(LocatorSearchSource))?.GetManifestResourceStream(
                "Esri.ArcGISRuntime.Toolkit.Maui.Assets.pin-red.png");
#else
            Stream? resourceStream = Assembly.GetAssembly(typeof(LocatorSearchSource))?.GetManifestResourceStream(
                "Esri.ArcGISRuntime.Toolkit.EmbeddedResources.pin_red.png");
#endif

            if (resourceStream != null)
            {
                PictureMarkerSymbol pinSymbol = await PictureMarkerSymbol.CreateAsync(resourceStream);
                pinSymbol.Width = 33;
                pinSymbol.Height = 33;
                pinSymbol.LeaderOffsetX = 16.5;
                pinSymbol.OffsetY = 16.5;
                DefaultSymbol = pinSymbol;
            }
        }

        /// <summary>
        /// This search source does not track selection state.
        /// </summary>
        public virtual void NotifySelected(SearchResult result)
        {
            // This space intentionally left blank.
        }

        /// <summary>
        /// This search source does not track selection state.
        /// </summary>
        public virtual void NotifyDeselected(SearchResult? result)
        {
            // This space intentionally left blank.
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await LoadTask;

            cancellationToken.ThrowIfCancellationRequested();

            SuggestParameters.PreferredSearchLocation = PreferredSearchLocation;
            SuggestParameters.MaxResults = MaximumSuggestions;

            var results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return SuggestionToSearchSuggestion(results);
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken cancellationToken = default)
        {
            await LoadTask;

            cancellationToken.ThrowIfCancellationRequested();

            var results = await Locator.GeocodeAsync(suggestion.UnderlyingObject as SuggestResult, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return ResultToSearchResult(results);
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await LoadTask;

            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = PreferredSearchLocation;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return ResultToSearchResult(results);
        }

        /// <inheritdoc />
        public virtual async Task<IList<SearchResult>> RepeatSearchAsync(string queryString, Envelope queryExtent, CancellationToken cancellationToken = default)
        {
            await LoadTask;

            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = PreferredSearchLocation;
            GeocodeParameters.SearchArea = queryExtent;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return ResultToSearchResult(results);
        }

        /// <summary>
        /// Converts geocode result list into list of results, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchResult> ResultToSearchResult(IReadOnlyList<GeocodeResult> input)
        {
            var results = input.Select(i => GeocodeResultToSearchResult(i));

            return results.Take(MaximumResults).ToList();
        }

        /// <summary>
        /// Converts suggest result list into list of suggestions, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchSuggestion> SuggestionToSearchSuggestion(IReadOnlyList<SuggestResult> input)
        {
            var results = input.Select(i => SuggestResultToSearchSuggestion(i));

            return results.Take(MaximumResults).ToList();
        }

        /// <summary>
        /// Creates a basic search result for the given geocode result.
        /// </summary>
        private SearchResult GeocodeResultToSearchResult(GeocodeResult r)
        {
            string? subtitle = null;
            if (SubtitleAttributeKey != null && r.Attributes.ContainsKey(SubtitleAttributeKey))
            {
                subtitle = r.Attributes[SubtitleAttributeKey]?.ToString();
            }

            Mapping.Viewpoint? selectionViewpoint = r.Extent == null ? null : new Mapping.Viewpoint(r.Extent);
            return new SearchResult(r.Label, subtitle, this, new Graphic(r.DisplayLocation, r.Attributes, DefaultSymbol), selectionViewpoint) { CalloutDefinition = DefaultCalloutDefinition };
        }

        /// <summary>
        /// Creates a basic search suggestion for the given suggest result.
        /// </summary>
        private SearchSuggestion SuggestResultToSearchSuggestion(SuggestResult r)
        {
            return new SearchSuggestion(r.Label, this) { IsCollection = r.IsCollection, UnderlyingObject = r };
        }
    }
}
