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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    /// Search source intended for use with the World Geocode Service and similarly-configured geocode services.
    /// </summary>
    internal class WorldGeocoderSearchSource : LocatorSearchSource
    {
        private const string AddressAttributeKey = "Place_addr";

        // Attribute used to identify the type of result coming from the locaotr.
        private const string LocatorIconAttributeKey = "Type";

        /// <summary>
        /// Gets or sets the minimum number of results to attempt to return.
        /// If there are too few results, the search is repeated with loosened parameters until enough results are accumulated.
        /// </summary>
        /// <remarks>
        /// If no search is successful, it is still possible to have a total number of results less than this threshold.
        /// Does not apply to repeated search with area constraint.
        /// Set to zero to disable search repeat behavior. Defaults to 0.
        /// </remarks>
        public int RepeatSearchResultThreshold { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum number of suggestions to attempt to return.
        /// If there are too few suggestions, request is repeated with loosened constraints until enough suggestions are accumulated.
        /// </summary>
        /// <remarks>
        /// If no search is successful, it is still possible to have a total number of results less than this threshold.
        /// Does not apply to repeated search with area constraint.
        /// Set to zero to disable search repeat behavior. Defaults to 0.
        /// </remarks>
        public int RepeatSuggestResultThreshold { get; set; } = 0;

        /// <summary>
        /// Gets or sets the web style used to find symbols for results.
        /// When set, symbols are found for results based on the result's `Type` field, if available.
        /// </summary>
        /// <remarks>
        /// Defaults to the style identified by the name "Esri2DPointSymbolsStyle".
        /// The default Esri 2D point symbol has good results for many of the types returned by the world geocode service.
        /// You can use this property to customize result icons by publishing a web style, taking care to ensure that symbol keys match the `Type` attribute returned by the locator.
        /// </remarks>
        public SymbolStyle? ResultSymbolStyle { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldGeocoderSearchSource"/> class.
        /// </summary>
        /// <param name="locator">Locator to use.</param>
        /// <param name="style">Symbol style to use to find results.</param>
        public WorldGeocoderSearchSource(LocatorTask locator, SymbolStyle? style)
            : base(locator)
        {
            SubtitleAttributeKey = AddressAttributeKey;
            if (style != null)
            {
                ResultSymbolStyle = style;
            }
            InitializeLocatorAttributes();
        }

        private void InitializeLocatorAttributes()
        {
            // Add attributes expected from the World Geocoder Service if present, otherwise default to all attributes.
            if (Locator.Uri?.ToString() == WorldGeocoderUriString &&
                (Locator.LocatorInfo?.ResultAttributes?.Any() == true))
            {
                var desiredAttributes = new[] { AddressAttributeKey, LocatorIconAttributeKey };
                foreach (var attr in desiredAttributes.OfType<string>())
                {
                    if (Locator.LocatorInfo.ResultAttributes.Where(at => at.Name == attr).Any())
                    {
                        GeocodeParameters.ResultAttributeNames.Add(attr);
                    }
                }
            }
            else
            {
                GeocodeParameters.ResultAttributeNames.Add("*");
            }
        }

        /// <summary>
        /// Converts suggest result list into list of suggestions, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchSuggestion> SuggestionToSearchSuggestion(IReadOnlyList<SuggestResult> input)
        {
            var results = input.Select(i => SuggestResultToSearchSuggestion(i));

            return results.Take(MaximumResults).ToList();
        }

        /// <inheritdoc />
        public override async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken cancellationToken = default)
        {
            await LoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            var tempParams = new GeocodeParameters();
            foreach (var attribute in GeocodeParameters.ResultAttributeNames)
            {
                tempParams.ResultAttributeNames.Add(attribute);
            }

            var results = await Locator.GeocodeAsync(suggestion.UnderlyingObject as SuggestResult, tempParams, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return await ResultToSearchResult(results);
        }

        /// <summary>
        /// Creates a basic search suggestion for the given suggest result.
        /// </summary>
        private SearchSuggestion SuggestResultToSearchSuggestion(SuggestResult r)
        {
            return new SearchSuggestion(r.Label, this) { IsCollection = r.IsCollection, UnderlyingObject = r };
        }

        /// <inheritdoc/>
        public override async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await LoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            SuggestParameters.PreferredSearchLocation = PreferredSearchLocation;
            SuggestParameters.MaxResults = MaximumSuggestions;
            SuggestParameters.SearchArea = SearchArea;

            var results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (RepeatSuggestResultThreshold > 0 && results.Count < RepeatSuggestResultThreshold)
            {
                SuggestParameters.SearchArea = null;
                results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (RepeatSuggestResultThreshold > 0 && results.Count < RepeatSuggestResultThreshold)
            {
                SuggestParameters.PreferredSearchLocation = null;
                results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return SuggestionToSearchSuggestion(results);
        }

        /// <inheritdoc/>
        public override async Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await LoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = PreferredSearchLocation;
            GeocodeParameters.SearchArea = SearchArea;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (RepeatSearchResultThreshold > 0 && results.Count < RepeatSearchResultThreshold)
            {
                GeocodeParameters.SearchArea = null;
                results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (RepeatSearchResultThreshold > 0 && results.Count < RepeatSearchResultThreshold)
            {
                GeocodeParameters.PreferredSearchLocation = null;
                results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return await ResultToSearchResult(results);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override async Task<IList<SearchResult>> RepeatSearchAsync(string queryString, Geometry.Envelope queryArea, CancellationToken cancellationToken = default)
        {
            await LoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.SearchArea = queryArea;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return await ResultToSearchResult(results);
        }

        private async Task<SearchResult> GeocodeResultToSearchResult(GeocodeResult r)
        {
            var symbol = await SymbolForResult(r);
            string? subtitle = null;
            if (SubtitleAttributeKey != null && r.Attributes.ContainsKey(SubtitleAttributeKey) && r.Attributes[SubtitleAttributeKey] is string subtitleString)
            {
                subtitle = subtitleString;
            }

            var viewpoint = r.Extent == null ? null : new Mapping.Viewpoint(r.Extent);

            var graphic = new Graphic(r.DisplayLocation, r.Attributes, symbol);

            CalloutDefinition callout = new CalloutDefinition(graphic) { Text = r.Label, DetailText = subtitle };

            return new SearchResult(r.Label, subtitle, this, graphic, viewpoint) { CalloutDefinition = callout };
        }

        private async Task<Symbol?> SymbolForResult(GeocodeResult r)
        {
            if (ResultSymbolStyle != null && r.Attributes.ContainsKey(LocatorIconAttributeKey) && r.Attributes[LocatorIconAttributeKey] is string typeAttrs)
            {
                if (Locator.Uri?.ToString() == WorldGeocoderUriString && ResultSymbolStyle.StyleName == "Esri2DPointSymbolsStyle")
                {
                    var firstResult = await ResultSymbolStyle.GetSymbolAsync(new[] { typeAttrs.ToString().Replace(' ', '-').ToLower() });
                    if (firstResult != null)
                    {
                        return firstResult;
                    }
                }

                var symbParams = new SymbolStyleSearchParameters();
                symbParams.Names.Add(typeAttrs.ToString());
                symbParams.NamesStrictlyMatch = false;
                var symbolResult = await ResultSymbolStyle.SearchSymbolsAsync(symbParams);

                if (symbolResult.Any())
                {
                    return await symbolResult.First().GetSymbolAsync();
                }
            }

            return DefaultSymbol;
        }

        /// <summary>
        /// Converts geocode result list into list of results, applying result limits and calling necessary callbacks.
        /// </summary>
        private async Task<IList<SearchResult>> ResultToSearchResult(IReadOnlyList<GeocodeResult> input)
        {
            IEnumerable<SearchResult> results = await Task.WhenAll(input.Select(i => GeocodeResultToSearchResult(i)));

            return results.Take(MaximumResults).ToList();
        }
    }
}
