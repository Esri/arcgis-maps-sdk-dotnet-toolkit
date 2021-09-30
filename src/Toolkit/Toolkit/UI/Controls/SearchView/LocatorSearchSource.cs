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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Basic search source implementation for generic locators.
    /// </summary>
    /// <seealso cref="SmartLocatorSearchSource" />
    public class LocatorSearchSource : ISearchSource
    {
        /// <summary>
        /// Gets or sets the name of the locator. Defaults to the locator's name, or "locator" if not set.
        /// </summary>
        public string DisplayName { get; set; } = "Locator";

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
        public Symbol? DefaultSymbol { get; set; }

        /// <summary>
        /// Gets or sets a callback that can be used to customize or filter results before they are returned.
        /// </summary>
        public Func<SearchResult, SearchResult?>? ResultCustomizationCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that can be used to customize or filter suggestions before they are returned.
        /// </summary>
        public Func<SearchSuggestion, SearchSuggestion?>? SuggestionCustomizationCallback { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocatorSearchSource"/> class.
        /// <seealso cref="SmartLocatorSearchSource"/> for a search source with advanced features intended for use with the World Geocoder Service.
        /// </summary>
        /// <param name="locator">Locator to be used.</param>
        public LocatorSearchSource(LocatorTask locator)
        {
            Locator = locator;
            if (!string.IsNullOrWhiteSpace(locator.LocatorInfo?.Name ?? null))
            {
                DisplayName = locator?.LocatorInfo?.Name ?? "Locator";
            }

            _ = EnsureLoaded();
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            var results = await Locator.GeocodeAsync(suggestion.UnderlyingObject as SuggestResult, cancellationToken ?? CancellationToken.None);

            return ResultToSearchResult(results);
        }

        private async Task EnsureLoaded()
        {
            if (Locator.LoadStatus == LoadStatus.Loaded)
            {
                return;
            }

            await Locator.RetryLoadAsync();
            // TODO = decide how to handle locators that throw.

            GeocodeParameters.ResultAttributeNames.Add("*");
        }

        /// <summary>
        /// Creates a basic search result for the given geocode result.
        /// </summary>
        private SearchResult GeocodeResultToSearchResult(GeocodeResult r)
        {
            Mapping.Viewpoint? selectionViewpoint = r.Extent == null ? null : new Mapping.Viewpoint(r.Extent);
            return new SearchResult(r.Label, null, this, new Graphic(r.DisplayLocation, r.Attributes, DefaultSymbol), selectionViewpoint) { CalloutDefinition = DefaultCalloutDefinition };
        }

        /// <summary>
        /// Creates a basic search suggestion for the given suggest result.
        /// </summary>
        private SearchSuggestion SuggestResultToSearchSuggestion(SuggestResult r)
        {
            return new SearchSuggestion(r.Label, this) { IsCollection = r.IsCollection };
        }

        /// <summary>
        /// Converts geocode result list into list of results, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchResult> ResultToSearchResult(IReadOnlyList<GeocodeResult> input)
        {
            var results = input.Select(i => GeocodeResultToSearchResult(i));

            if (ResultCustomizationCallback != null)
            {
                results = results.Select(ResultCustomizationCallback).OfType<SearchResult>();
            }

            return results.Take(MaximumResults).ToList();
        }

        /// <summary>
        /// Converts suggest result list into list of suggestions, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchSuggestion> SuggestionToSearchSuggestion(IReadOnlyList<SuggestResult> input)
        {
            var results = input.Select(i => SuggestResultToSearchSuggestion(i));

            if (SuggestionCustomizationCallback != null)
            {
                results = results.Select(SuggestionCustomizationCallback).OfType<SearchSuggestion>();
            }

            return results.Take(MaximumResults).ToList();
        }

        /// <summary>
        /// No action taken.
        /// </summary>
        public void NotifySelected(SearchResult result)
        {
            // This space intentionally left blank.
        }

        /// <summary>
        /// No action taken.
        /// </summary>
        public void NotifyDeselected(SearchResult? result)
        {
            // This space intentionally left blank.
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, MapPoint? preferredSearchLocation, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            SuggestParameters.PreferredSearchLocation = preferredSearchLocation;
            SuggestParameters.MaxResults = MaximumSuggestions;

            var results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken ?? CancellationToken.None);

            return SuggestionToSearchSuggestion(results);
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchResult>> SearchAsync(string queryString, MapPoint? preferredSearchLocation, Geometry.Geometry? searchArea, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = preferredSearchLocation;
            GeocodeParameters.SearchArea = searchArea;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken ?? CancellationToken.None);
            return ResultToSearchResult(results);
        }
    }
}
